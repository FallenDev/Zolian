using System.Net;
using System.Net.Sockets;
using Chaos.Collections;
using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Chaos.Cryptography;
using Chaos.Extensions.Common;
using Chaos.Models.World;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities;
using Chaos.Networking.Entities.Client;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;
using Chaos.Security.Abstractions;
using Chaos.Services.Factories.Abstractions;
using Chaos.Services.Servers.Options;
using Chaos.Services.Storage.Abstractions;
using Chaos.Storage.Abstractions;
using Darkages.Network.Client.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chaos.Services.Servers;

public sealed class LoginServer : ServerBase<ILoginClient>, ILoginServer<ILoginClient>
{
    private readonly IAccessManager AccessManager;
    private readonly IAsyncStore<Aisling> AislingStore;
    private readonly ISimpleCacheProvider CacheProvider;
    private readonly IClientProvider ClientProvider;
    private readonly IMetaDataStore MetaDataStore;
    private readonly Notice Notice;
    public ConcurrentDictionary<uint, CreateCharRequestArgs> CreateCharRequests { get; }
    private new LoginOptions Options { get; }

    public LoginServer(
        IAsyncStore<Aisling> aislingStore,
        IClientRegistry<ILoginClient> clientRegistry,
        IClientProvider clientProvider,
        ISimpleCacheProvider cacheProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IOptions<LoginOptions> options,
        ILogger<LoginServer> logger,
        IMetaDataStore metaDataStore,
        IAccessManager accessManager
    )
        : base(
            redirectManager,
            packetSerializer,
            clientRegistry,
            options,
            logger)
    {
        Options = options.Value;
        AislingStore = aislingStore;
        ClientProvider = clientProvider;
        CacheProvider = cacheProvider;
        MetaDataStore = metaDataStore;
        AccessManager = accessManager;
        Notice = new Notice(options.Value.NoticeMessage);
        CreateCharRequests = new ConcurrentDictionary<uint, CreateCharRequestArgs>();

        IndexHandlers();
    }

    #region OnHandlers
    public ValueTask OnClientRedirected(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<ClientRedirectedArgs>(in packet);

        ValueTask InnerOnclientRedirect(ILoginClient localClient, ClientRedirectedArgs localArgs)
        {
            var reservedRedirect = Options.ReservedRedirects
                                          .FirstOrDefault(rr => (rr.Id == localArgs.Id) && rr.Name.EqualsI(localArgs.Name));

            if (reservedRedirect != null)
            {
                Logger.WithProperty(localClient)
                      .WithProperty(reservedRedirect)
                      .LogDebug("Received external redirect {@RedirectID}", reservedRedirect.Id);

                localClient.Crypto = new Crypto(localArgs.Seed, localArgs.Key, string.Empty);
                localClient.SendLoginNotice(false, Notice);
            } else if (RedirectManager.TryGetRemove(localArgs.Id, out var redirect))
            {
                Logger.WithProperty(localClient)
                      .WithProperty(redirect)
                      .LogDebug("Received internal redirect {@RedirectId}", redirect.Id);

                localClient.Crypto = new Crypto(redirect.Seed, redirect.Key, redirect.Name);
                localClient.SendLoginNotice(false, Notice);
            } else
            {
                Logger.WithProperty(localClient)
                      .WithProperty(localArgs)
                      .LogWarning("{@ClientIp} tried to redirect with invalid redirect details", localClient.RemoteIp.ToString());

                localClient.Disconnect();
            }

            return default;
        }

        return ExecuteHandler(client, args, InnerOnclientRedirect);
    }

    public ValueTask OnCreateCharFinalize(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharFinalizeArgs>(in packet);

        async ValueTask InnerOnCreateCharFinalize(ILoginClient localClient, CreateCharFinalizeArgs localArgs)
        {
            if (CreateCharRequests.TryGetValue(localClient.Id, out var requestArgs))
            {
                (var hairStyle, var gender, var hairColor) = localArgs;

                var mapInstanceCache = CacheProvider.GetCache<MapInstance>();
                var startingMap = mapInstanceCache.Get(Options.StartingMapInstanceId);

                var user = new Aisling(
                    requestArgs.Name,
                    gender,
                    hairStyle,
                    hairColor,
                    startingMap,
                    Options.StartingPoint);

                await AislingStore.SaveAsync(user);

                Logger.WithProperty(localClient)
                      .LogDebug("New character created with name {@Name}", user.Name);

                localClient.SendLoginMessage(LoginMessageType.Confirm);
            } else
                localClient.SendLoginMessage(LoginMessageType.ClearNameMessage, "Unable to create character, bad request.");
        }

        return ExecuteHandler(client, args, InnerOnCreateCharFinalize);
    }

    public ValueTask OnCreateCharRequest(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharRequestArgs>(in packet);

        async ValueTask InnerOnCreateCharRequest(ILoginClient localClient, CreateCharRequestArgs localArgs)
        {
            var result = await AccessManager.SaveNewCredentialsAsync(localClient.RemoteIp, localArgs.Name, localArgs.Password);

            if (result.Success)
            {
                CreateCharRequests.AddOrUpdate(localClient.Id, localArgs, (_, _) => localArgs);
                localClient.SendLoginMessage(LoginMessageType.Confirm, string.Empty);
            } else
            {
                Logger.WithProperty(localClient)
                      .LogDebug(
                          "Failed to create character with name {@Name} for reason {@Reason}",
                          localArgs.Name,
                          result.FailureMessage);

                localClient.SendLoginMessage(GetLoginMessageType(result.Code), result.FailureMessage);
            }
        }

        return ExecuteHandler(client, args, InnerOnCreateCharRequest);
    }

    public ValueTask OnHomepageRequest(ILoginClient client, in ClientPacket packet)
    {
        static ValueTask InnerOnHomepageRequest(ILoginClient localClient)
        {
            localClient.SendLoginControls(LoginControlsType.Homepage, "https://www.darkages.com");

            return default;
        }

        return ExecuteHandler(client, InnerOnHomepageRequest);
    }

    public ValueTask OnLogin(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<LoginArgs>(in packet);

        async ValueTask InnerOnLogin(ILoginClient localClient, LoginArgs localArgs)
        {
            (var name, var password) = localArgs;

            var result = await AccessManager.ValidateCredentialsAsync(localClient.RemoteIp, name, password);

            if (!result.Success)
            {
                Logger.WithProperty(localClient)
                      .WithProperty(password)
                      .LogDebug("Failed to validate credentials for {@Name} for reason {@Reason}", name, result.FailureMessage);

                localClient.SendLoginMessage(LoginMessageType.WrongPassword, result.FailureMessage);

                return;
            }

            Logger.WithProperty(client)
                  .LogDebug("Validated credentials for {@Name}", name);

            var redirect = new Redirect(
                EphemeralRandomIdGenerator<uint>.Shared.NextId,
                Options.WorldRedirect,
                ServerType.World,
                localClient.Crypto.Key,
                localClient.Crypto.Seed,
                name);

            Logger.LogDebug(
                "Redirecting {@ClientIp} to {@ServerIp}",
                localClient.RemoteIp.ToString(),
                Options.WorldRedirect.Address.ToString());

            RedirectManager.Add(redirect);
            localClient.SendLoginMessage(LoginMessageType.Confirm);
            localClient.SendRedirect(redirect);
        }

        return ExecuteHandler(client, args, InnerOnLogin);
    }

    public ValueTask OnMetaDataRequest(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<MetaDataRequestArgs>(in packet);

        ValueTask InnerOnMetaDataRequest(ILoginClient localClient, MetaDataRequestArgs localArgs)
        {
            (var metadataRequestType, var name) = localArgs;

            localClient.SendMetaData(metadataRequestType, MetaDataStore, name);

            return default;
        }

        return ExecuteHandler(client, args, InnerOnMetaDataRequest);
    }

    public ValueTask OnNoticeRequest(ILoginClient client, in ClientPacket packet)
    {
        ValueTask InnerOnNoticeRequest(ILoginClient localClient)
        {
            localClient.SendLoginNotice(true, Notice);

            return default;
        }

        return ExecuteHandler(client, InnerOnNoticeRequest);
    }

    public ValueTask OnPasswordChange(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<PasswordChangeArgs>(in packet);

        async ValueTask InnerOnPasswordChange(ILoginClient localClient, PasswordChangeArgs localArgs)
        {
            (var name, var currentPassword, var newPassword) = localArgs;

            var result = await AccessManager.ChangePasswordAsync(
                localClient.RemoteIp,
                name,
                currentPassword,
                newPassword);

            if (!result.Success)
            {
                Logger.WithProperty(client)
                      .LogInformation(
                          "Failed to change password for aisling {@AislingName} for reason {@Reason}",
                          name,
                          result.FailureMessage);

                localClient.SendLoginMessage(GetLoginMessageType(result.Code), result.FailureMessage);

                return;
            }

            Logger.WithProperty(client)
                  .LogInformation("Changed password for aisling {@AislingName}", name);
        }

        return ExecuteHandler(client, args, InnerOnPasswordChange);
    }
    #endregion

    #region Connection / Handler
    public override ValueTask HandlePacketAsync(ILoginClient client, in ClientPacket packet)
    {
        var handler = ClientHandlers[(byte)packet.OpCode];

        return handler?.Invoke(client, in packet) ?? default;
    }

    protected override void IndexHandlers()
    {
        if (ClientHandlers == null!)
            return;

        base.IndexHandlers();

        ClientHandlers[(byte)ClientOpCode.CreateCharRequest] = OnCreateCharRequest;
        ClientHandlers[(byte)ClientOpCode.CreateCharFinalize] = OnCreateCharFinalize;
        ClientHandlers[(byte)ClientOpCode.ClientRedirected] = OnClientRedirected;
        ClientHandlers[(byte)ClientOpCode.HomepageRequest] = OnHomepageRequest;
        ClientHandlers[(byte)ClientOpCode.Login] = OnLogin;
        ClientHandlers[(byte)ClientOpCode.MetaDataRequest] = OnMetaDataRequest;
        ClientHandlers[(byte)ClientOpCode.NoticeRequest] = OnNoticeRequest;
        ClientHandlers[(byte)ClientOpCode.PasswordChange] = OnPasswordChange;
    }

    protected override async void OnConnection(IAsyncResult ar)
    {
        var serverSocket = (Socket)ar.AsyncState!;
        var clientSocket = serverSocket.EndAccept(ar);

        serverSocket.BeginAccept(OnConnection, serverSocket);

        var ip = clientSocket.RemoteEndPoint as IPEndPoint;
        Logger.LogDebug("Incoming connection from {@Ip}", ip!.ToString());

        try
        {
            await FinalizeConnectionAsync(clientSocket);
        } catch (Exception e)
        {
            Logger.LogError(e, "Failed to finalize connection");
        }
    }

    private async Task FinalizeConnectionAsync(Socket clientSocket)
    {
        var ipAddress = ((IPEndPoint)clientSocket.RemoteEndPoint!).Address;

        if (!await AccessManager.ShouldAllowAsync(ipAddress))
        {
            Logger.LogDebug("Rejected connection from {@Ip}", ipAddress.ToString());

            await clientSocket.DisconnectAsync(false);

            return;
        }

        var client = ClientProvider.CreateClient<ILoginClient>(clientSocket);

        Logger.LogDebug("Connection established with {@ClientIp}", client.RemoteIp.ToString());

        if (!ClientRegistry.TryAdd(client))
        {
            Logger.WithProperty(client)
                  .LogError("Somehow two clients got the same id");

            client.Disconnect();

            return;
        }

        client.OnDisconnected += OnDisconnect;

        client.BeginReceive();
        client.SendAcceptConnection();
    }

    private void OnDisconnect(object? sender, EventArgs e)
    {
        var client = (ILoginClient)sender!;
        ClientRegistry.TryRemove(client.Id, out _);
    }

    private LoginMessageType GetLoginMessageType(CredentialValidationResult.FailureCode code) => code switch
    {
        CredentialValidationResult.FailureCode.InvalidUsername    => LoginMessageType.ClearNameMessage,
        CredentialValidationResult.FailureCode.InvalidPassword    => LoginMessageType.ClearPswdMessage,
        CredentialValidationResult.FailureCode.PasswordTooLong    => LoginMessageType.ClearPswdMessage,
        CredentialValidationResult.FailureCode.PasswordTooShort   => LoginMessageType.ClearPswdMessage,
        CredentialValidationResult.FailureCode.UsernameTooLong    => LoginMessageType.ClearNameMessage,
        CredentialValidationResult.FailureCode.UsernameTooShort   => LoginMessageType.ClearNameMessage,
        CredentialValidationResult.FailureCode.UsernameNotAllowed => LoginMessageType.ClearNameMessage,
        CredentialValidationResult.FailureCode.TooManyAttempts    => LoginMessageType.ClearPswdMessage,
        _                                                         => throw new ArgumentOutOfRangeException()
    };
    #endregion
}