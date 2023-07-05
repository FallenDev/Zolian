using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Chaos.Cryptography;
using Chaos.Extensions.Common;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Options;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;
using Darkages.Database;
using Darkages.Meta;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Gender = Darkages.Enums.Gender;

namespace Darkages.Network.Server;

public sealed class LoginServer : ServerBase<ILoginClient>, ILoginServer<ILoginClient>
{
    private readonly IClientFactory<LoginClient> _clientProvider;
    private readonly Notification _notification;
    public ConcurrentDictionary<uint, CreateCharRequestArgs> CreateCharRequests { get; }

    public LoginServer(
        IClientRegistry<ILoginClient> clientRegistry,
        IClientFactory<LoginClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IOptions<Chaos.Networking.Options.ServerOptions> options,
        ILogger<LoginServer> logger
    )
        : base(redirectManager, packetSerializer, clientRegistry, options, logger)
    {
        _clientProvider = clientProvider;
        _notification = Notification.FromFile("Notification.txt");
        CreateCharRequests = new ConcurrentDictionary<uint, CreateCharRequestArgs>();
        IndexHandlers();
    }

    #region OnHandlers

    /// <summary>
    /// 0x57 - Server Table & Redirect
    /// </summary>
    public ValueTask OnClientRedirected(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<ClientRedirectedArgs>(in packet);

        ValueTask InnerOnclientRedirect(ILoginClient localClient, ClientRedirectedArgs localArgs)
        {
            var reservedRedirect = ServerSetup.Instance.Config.ReservedRedirects
                                          .FirstOrDefault(rr => (rr.Id == localArgs.Id) && rr.Name.EqualsI(localArgs.Name));

            if (reservedRedirect != null)
            {
                Logger.WithProperty(localClient)
                      .WithProperty(reservedRedirect)
                      .LogDebug("Received external redirect {@RedirectID}", reservedRedirect.Id);

                localClient.Crypto = new Crypto(localArgs.Seed, localArgs.Key, string.Empty);
                localClient.SendLoginNotice(false, _notification);
            } else if (RedirectManager.TryGetRemove(localArgs.Id, out var redirect))
            {
                Logger.WithProperty(localClient)
                      .WithProperty(redirect)
                      .LogDebug("Received internal redirect {@RedirectId}", redirect.Id);

                localClient.Crypto = new Crypto(redirect.Seed, redirect.Key, redirect.Name);
                localClient.SendLoginNotice(false, _notification);
            } else
            {
                Logger.WithProperty(localClient)
                      .WithProperty(localArgs)
                      .LogWarning("{@ClientIp} tried to redirect with invalid redirect details", localClient.RemoteIp.ToString());
                ServerSetup.Logger($"Attempt to redirect with invalid redirect details, {localClient.RemoteIp}");
                Analytics.TrackEvent($"Attempt to redirect with invalid redirect details, {localClient.RemoteIp}");
                localClient.Disconnect();
            }

            return default;
        }

        return ExecuteHandler(client, args, InnerOnclientRedirect);
    }

    /// <summary>
    /// 0x04 - Create New Player from Template
    /// </summary>
    public ValueTask OnCreateCharFinalize(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharFinalizeArgs>(in packet);

        async ValueTask InnerOnCreateCharFinalize(ILoginClient localClient, CreateCharFinalizeArgs localArgs)
        {
            if (CreateCharRequests.TryGetValue(localClient.Id, out var requestArgs))
            {
                var (hairStyle, gender, hairColor) = localArgs;
                var readyTime = DateTime.UtcNow;
                var maximumHp = Random.Shared.Next(128, 165);
                var maximumMp = Random.Shared.Next(30, 45);
                var user = new Aisling
                {
                    Created = readyTime,
                    Username = requestArgs.Name,
                    Password = requestArgs.Password,
                    LastLogged = readyTime,
                    CurrentHp = maximumHp,
                    BaseHp = maximumHp,
                    CurrentMp = maximumMp,
                    BaseMp = maximumMp,
                    Gender = (Gender)gender,
                    HairColor = (byte)hairColor,
                    HairStyle = hairStyle,
                    SkillBook = new SkillBook(),
                    SpellBook = new SpellBook(),
                    Inventory = new Inventory(),
                    BankManager = new Bank(),
                    EquipmentManager = new EquipmentManager(null)
                };

                await StorageManager.AislingBucket.Create(user).ConfigureAwait(true);

                Logger.WithProperty(localClient).LogDebug("New character created with name {@Name}", user.Username);

                localClient.SendLoginMessage(LoginMessageType.Confirm);
            } else
                localClient.SendLoginMessage(LoginMessageType.ClearNameMessage, "Unable to create character, bad request.");
        }

        return ExecuteHandler(client, args, InnerOnCreateCharFinalize);
    }

    /// <summary>
    /// 0x02 - Character Creation Checks
    /// </summary>
    public ValueTask OnCreateCharRequest(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharRequestArgs>(in packet);

        async ValueTask InnerOnCreateCharRequest(ILoginClient localClient, CreateCharRequestArgs localArgs)
        {
            var result = await ValidateUsernameAndPassword(localClient, localArgs.Name, localArgs.Password);

            if (result)
            {
                CreateCharRequests.AddOrUpdate(localClient.Id, localArgs, (_, _) => localArgs);
                localClient.SendLoginMessage(LoginMessageType.Confirm, string.Empty);
            } else
            {
                ServerSetup.Logger($"Character creation failed - {localArgs.Name} - {localClient.RemoteIp}");
                Analytics.TrackEvent($"Character creation failed - {localArgs.Name} - {localClient.RemoteIp}");
            }
        }

        return ExecuteHandler(client, args, InnerOnCreateCharRequest);
    }

    public ValueTask OnHomepageRequest(ILoginClient client, in ClientPacket packet)
    {
        static ValueTask InnerOnHomepageRequest(ILoginClient localClient)
        {
            localClient.SendLoginControls(LoginControlsType.Homepage, "https://www.ZolianAoC.com");

            return default;
        }

        return ExecuteHandler(client, InnerOnHomepageRequest);
    }

    /// <summary>
    /// 0x03 - Player Login and Redirect
    /// </summary>
    public ValueTask OnLogin(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<LoginArgs>(in packet);

        async ValueTask InnerOnLogin(ILoginClient localClient, LoginArgs localArgs)
        {
            var (name, password) = localArgs;
            var result = await StorageManager.AislingBucket.CheckPassword(name);

            if (result == null)
            {
                Logger.WithProperty(localClient)
                      .WithProperty(password)
                      .LogDebug("Player does not exist {@Name}.", name);

                localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, $"{{=q'{name}' {{=adoes not currently exist on this server. You can make this hero by clicking on 'Create'");

                return;
            }

            Logger.WithProperty(client)
                  .LogDebug("Validated credentials for {@Name}", name);

            var maintCheck = name.ToLowerInvariant();

            var connInfo = new ConnectionInfo
            {
                Address = IPAddress.Parse(ServerSetup.ServerOptions.Value.ServerIp),
                HostName = "Zolian",
                Port = ServerSetup.Instance.Config.SERVER_PORT
            };

            var redirect = new Redirect(
                EphemeralRandomIdGenerator<uint>.Shared.NextId,
                connInfo,
                ServerType.World,
                localClient.Crypto.Key,
                localClient.Crypto.Seed,
                name);

            switch (maintCheck)
            {
                case "asdf":
                    localClient.SendLoginMessage(LoginMessageType.Confirm, "Maintenance Account, denied access");
                    return;
                case "death":
                {
                    var ipLocal = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                    if (localClient.IsLoopback() || localClient.RemoteIp.Equals(ipLocal))
                    {
                        result.LastAttemptIP = ipLocal.ToString();
                        result.LastIP = ipLocal.ToString();
                        if (result.Password == ServerSetup.Instance.Unlock)
                            result.PasswordAttempts = 0;
                        await SavePassword(result);
                        RedirectManager.Add(redirect);
                        localClient.SendLoginMessage(LoginMessageType.Confirm);
                        localClient.SendRedirect(redirect);
                        return;
                    }

                    localClient.SendLoginMessage(LoginMessageType.Confirm, "GM Account, denied access");
                    return;
                }
                default:
                    result.LastAttemptIP = localClient.RemoteIp.ToString();
                    result.LastIP = localClient.RemoteIp.ToString();
                    if (result.Password == ServerSetup.Instance.Unlock)
                        result.PasswordAttempts = 0;
                    await SavePassword(result);
                    break;
            }

            if (result.Hacked)
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, we've locked the account; If this is your account, please contact the GM.");
                return;
            }

            Logger.LogDebug(
                "Redirecting {@ClientIp} to {@ServerIp}",
                localClient.RemoteIp.ToString(),
                connInfo.Address.ToString());

            RedirectManager.Add(redirect);
            localClient.SendLoginMessage(LoginMessageType.Confirm);
            localClient.SendRedirect(redirect);
        }

        return ExecuteHandler(client, args, InnerOnLogin);
    }

    /// <summary>
    /// 0x7B - Metadata Load
    /// </summary>
    public ValueTask OnMetaDataRequest(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<MetaDataRequestArgs>(in packet);

        ValueTask InnerOnMetaDataRequest(ILoginClient localClient, MetaDataRequestArgs localArgs)
        {
            var (metadataRequestType, name) = localArgs;

            localClient.SendMetaData(metadataRequestType, new MetafileManager(), name);

            return default;
        }

        return ExecuteHandler(client, args, InnerOnMetaDataRequest);
    }

    /// <summary>
    /// 0x4B - Notification Load
    /// </summary>
    public ValueTask OnNoticeRequest(ILoginClient client, in ClientPacket packet)
    {
        ValueTask InnerOnNoticeRequest(ILoginClient localClient)
        {
            localClient.SendLoginNotice(true, _notification);

            return default;
        }

        return ExecuteHandler(client, InnerOnNoticeRequest);
    }

    /// <summary>
    /// 0x26 - Change Password
    /// </summary>
    public ValueTask OnPasswordChange(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<PasswordChangeArgs>(in packet);

        async ValueTask InnerOnPasswordChange(ILoginClient localClient, PasswordChangeArgs localArgs)
        {
            var (name, currentPassword, newPassword) = localArgs;
            var aisling = StorageManager.AislingBucket.CheckPassword(name);

            if (aisling.Result == null)
            {
                localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, "Player does not exist");
                return;
            }

            if (aisling.Result.Hacked)
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, we've locked the account; If this is your account, please contact the GM.");
                return;
            }

            if (aisling.Result.Password != currentPassword)
            {
                if (aisling.Result.PasswordAttempts <= 9)
                {
                    ServerSetup.Logger($"{aisling.Result} attempted an incorrect password.");
                    aisling.Result.LastIP = localClient.RemoteIp.ToString();
                    aisling.Result.LastAttemptIP = localClient.RemoteIp.ToString();
                    aisling.Result.PasswordAttempts += 1;
                    await SavePassword(aisling.Result);
                    localClient.SendLoginMessage(LoginMessageType.Confirm, "Incorrect Information provided.");
                    return;
                }

                ServerSetup.Logger($"{aisling.Result} was locked to protect their account.");
                localClient.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, the player has been locked.");
                aisling.Result.LastIP = localClient.RemoteIp.ToString();
                aisling.Result.LastAttemptIP = localClient.RemoteIp.ToString();
                aisling.Result.Hacked = true;
                await SavePassword(aisling.Result);
                return;
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "New password was not accepted. Keep it between 6 to 8 characters.");
                return;
            }

            aisling.Result.Password = newPassword;
            aisling.Result.LastIP = localClient.RemoteIp.ToString();
            aisling.Result.LastAttemptIP = localClient.RemoteIp.ToString();
            await SavePassword(aisling.Result);

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
        if (ClientHandlers == null!) return;

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

        //ToDo: Access restriction - Add-in checks

        var client = _clientProvider.CreateClient(clientSocket);

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

    private async Task<bool> SavePassword(Aisling aisling)
    {
        if (aisling == null) return false;

        try
        {
            await StorageManager.AislingBucket.PasswordSave(aisling);
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }

        return true;
    }

    private async Task<bool> ValidateUsernameAndPassword(ILoginClient client, string name, string password)
    {
        var aisling = await StorageManager.AislingBucket.CheckIfPlayerExists(name);
        var regex = new Regex("(?:[^a-z]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        if (aisling == false)
        {
            if (regex.IsMatch(name))
            {
                Analytics.TrackEvent($"Player attempted to create an unsupported username. {name} \n {client.Id}");
                client.SendLoginMessage(LoginMessageType.ClearNameMessage, "Unsupported username, please try again.");
                return false;
            }

            if (name.Length is < 3 or > 12)
            {
                client.SendLoginMessage(LoginMessageType.ClearNameMessage, "{=eYour {=qUserName {=emust be within 3 to 12 characters in length.");
                return false;
            }

            if (password.Length > 5) return true;
            client.SendLoginMessage(LoginMessageType.ClearPswdMessage, "{=eYour {=qPassword {=edoes not meet the minimum requirement of 6 characters.");
            return false;
        }

        client.SendLoginMessage(LoginMessageType.Confirm, "Character already exists.");
        return false;
    }
    #endregion
}