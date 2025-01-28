using Chaos.Common.Identity;
using Chaos.Extensions.Common;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Options;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Darkages.Database;
using Darkages.Meta;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Darkages.Managers;
using JetBrains.Annotations;
using Gender = Darkages.Enums.Gender;
using Redirect = Chaos.Networking.Entities.Redirect;
using ServerOptions = Chaos.Networking.Options.ServerOptions;
using ILoginClient = Darkages.Network.Client.Abstractions.ILoginClient;
using StringExtensions = ServiceStack.StringExtensions;
using Chaos.Networking.Abstractions.Definitions;
using Darkages.Common;
using Darkages.Network.Client.Abstractions;

namespace Darkages.Network.Server;

[UsedImplicitly]
public sealed partial class LoginServer : ServerBase<ILoginClient>, ILoginServer<ILoginClient>
{
    private readonly IClientFactory<LoginClient> _clientProvider;
    private readonly Notification _notification;
    private static readonly string[] GameMastersIPs = ServerSetup.Instance.GameMastersIPs;
    private ConcurrentDictionary<uint, CreateCharInitialArgs> CreateCharRequests { get; }

    public LoginServer(
        IClientRegistry<ILoginClient> clientRegistry,
        IClientFactory<LoginClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        ILogger<LoginServer> logger
    )
        : base(
            redirectManager,
            packetSerializer,
            clientRegistry,
            Microsoft.Extensions.Options.Options.Create(new ServerOptions
            {
                Address = ServerSetup.Instance.IpAddress,
                Port = ServerSetup.Instance.Config.LOGIN_PORT
            }),
            logger)
    {
        ServerSetup.Instance.LoginServer = this;
        _clientProvider = clientProvider;
        _notification = Notification.FromFile("Notification.txt");
        CreateCharRequests = [];
        IndexHandlers();
    }

    #region OnHandlers

    public ValueTask OnClientRedirected(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<ClientRedirectedArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnClientRedirect);

        ValueTask InnerOnClientRedirect(ILoginClient localClient, ClientRedirectedArgs localArgs)
        {
            if (localArgs.Message == "Redirect Successful")
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "Redirected.. Welcome!");
            }

            return default;
        }
    }

    /// <summary>
    /// 0x04 - Create New Player from Template
    /// </summary>
    public ValueTask OnCreateCharFinalize(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharFinalizeArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnCreateCharFinalize);

        async ValueTask InnerOnCreateCharFinalize(ILoginClient localClient, CreateCharFinalizeArgs localArgs)
        {
            if (CreateCharRequests.TryGetValue(localClient.Id, out var requestArgs))
            {
                //var readyTime = DateTime.UtcNow;
                //var maximumHp = Random.Shared.Next(128, 165);
                //var maximumMp = Random.Shared.Next(30, 45);
                //var user = new Aisling
                //{
                //    Created = readyTime,
                //    Username = requestArgs.Name,
                //    Password = requestArgs.Password,
                //    LastLogged = readyTime,
                //    CurrentHp = maximumHp,
                //    BaseHp = maximumHp,
                //    CurrentMp = maximumMp,
                //    BaseMp = maximumMp,
                //    Gender = (Gender)localArgs.Gender,
                //    HairColor = (byte)localArgs.HairColor,
                //    HairStyle = localArgs.HairStyle,
                //    BodyColor = 0,
                //    SkillBook = new SkillBook(),
                //    SpellBook = new SpellBook(),
                //    Inventory = new InventoryManager(),
                //    BankManager = new BankManager(),
                //    EquipmentManager = new EquipmentManager(null)
                //};

                //await StorageManager.AislingBucket.Create(user).ConfigureAwait(true);
                //ServerSetup.Instance.GlobalCreationCount.AddOrUpdate(localClient.RemoteIp, 1, (remoteIp, creations) => creations += 1);
                //localClient.SendLoginMessage(LoginMessageType.Confirm);
            }
        }
    }

    /// <summary>
    /// 0x02 - Character Creation Checks
    /// </summary>
    public ValueTask OnCreateCharInitial(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharInitialArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnCreateCharRequest);

        async ValueTask InnerOnCreateCharRequest(ILoginClient localClient, CreateCharInitialArgs localArgs)
        {
            //ServerSetup.Instance.GlobalCreationCount.TryGetValue(localClient.RemoteIp, out var created);
            //var result = await ValidateUsernameAndPassword(localClient, localArgs.Name, localArgs.Password);

            //switch (result)
            //{
            //    case true when created <= 2:
            //        CreateCharRequests.AddOrUpdate(localClient.Id, localArgs, (_, _) => localArgs);
            //        localClient.SendLoginMessage(LoginMessageType.Confirm, string.Empty);
            //        break;
            //    case true:
            //        localClient.SendLoginMessage(LoginMessageType.Confirm, "Slow down on character creation.");
            //        break;
            //}
        }
    }

    public ValueTask OnHomepageRequest(ILoginClient client, in Packet packet)
    {
        return ExecuteHandler(client, InnerOnHomepageRequest);

        static ValueTask InnerOnHomepageRequest(ILoginClient localClient)
        {
            //localClient.SendLoginControl(LoginControlsType.Homepage, "https://www.darkages.com");
            return default;
        }
    }

    /// <summary>
    /// 0x03 - Player Login and Redirect
    /// </summary>
    public ValueTask OnLogin(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<LoginArgs>(in packet);
        if (ServerSetup.Instance.Running) return ExecuteHandler(client, args, InnerOnLogin);

        client.SendLoginMessage(LoginMessageType.Confirm, "Server is down for maintenance");
        return default;

        async ValueTask InnerOnLogin(ILoginClient localClient, LoginArgs localArgs)
        {
            if (StringExtensions.IsNullOrEmpty(localArgs.Name) || StringExtensions.IsNullOrEmpty(localArgs.Password)) return;

            if (ServerSetup.Instance.GlobalPasswordAttempt.TryGetValue(localClient.RemoteIp, out var attempts))
            {
                if (attempts >= 5)
                {
                    localClient.SendLoginMessage(LoginMessageType.Confirm, "Your IP has been restricted for too many incorrect password attempts.");
                    ServerSetup.EventsLogger($"{localClient.RemoteIp} has attempted {attempts} and has been restricted.");
                    SentrySdk.CaptureException(new Exception($"{localClient.RemoteIp} has been restricted due to password attempts."));
                    return;
                }
            }

            var result = await AislingStorage.CheckPassword(localArgs.Name);

            if (localArgs.Password == ServerSetup.Instance.Unlock)
            {
                var unlockIp = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                if (IPAddress.IsLoopback(localClient.RemoteIp) || localClient.RemoteIp.Equals(unlockIp))
                {
                    result.LastAttemptIP = localClient.RemoteIp.ToString();
                    result.LastIP = localClient.RemoteIp.ToString();
                    result.PasswordAttempts = 0;
                    result.Hacked = false;
                    result.Password = "abc123";
                    await SavePassword(result);
                    localClient.SendLoginMessage(LoginMessageType.Confirm, "Account was unlocked!");
                    return;
                }

                ServerSetup.Instance.GlobalPasswordAttempt.AddOrUpdate(localClient.RemoteIp, 1, (remoteIp, creations) => creations += 1);
                localClient.SendLoginMessage(LoginMessageType.Confirm, "GM Action, denied access and IP logged.");
                ServerSetup.EventsLogger($"{localClient.RemoteIp} has attempted to use the unlock command.");
                SentrySdk.CaptureException(new Exception($"{localClient.RemoteIp} has attempted to use the unlock command"));
                return;
            }

            var passed = await OnSecurityCheck(result, localClient, localArgs.Name, localArgs.Password);
            if (!passed) return;

            switch (localArgs.Name.ToLowerInvariant())
            {
                case "asdf":
                    localClient.SendLoginMessage(LoginMessageType.Confirm, "Locked Account, denied access");
                    return;
                case "death":
                    {
                        var ipLocal = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                        if (IPAddress.IsLoopback(localClient.RemoteIp) || localClient.RemoteIp.Equals(ipLocal))
                        {
                            Login(result, localClient);
                            return;
                        }

                        localClient.SendLoginMessage(LoginMessageType.Confirm, "GM Account, denied access");
                        return;
                    }
                case "scythe":
                    {
                        var ipLocal = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                        if (GameMastersIPs.Any(ip => localClient.RemoteIp.Equals(IPAddress.Parse(ip)))
                            || IPAddress.IsLoopback(localClient.RemoteIp) || localClient.RemoteIp.Equals(ipLocal))
                        {
                            Login(result, localClient);
                            return;
                        }

                        localClient.SendLoginMessage(LoginMessageType.Confirm, "GM Account, denied access");
                        return;
                    }
                default:
                    {
                        if (result.Hacked)
                        {
                            localClient.SendLoginMessage(LoginMessageType.Confirm, "Bruteforce detected, we've locked the account to protect it; If this is your account, please contact the GM.");
                            return;
                        }

                        Login(result, localClient);
                        break;
                    }
            }
        }
    }

    private void Login(Aisling player, ILoginClient loginClient)
    {
        player.LastAttemptIP = loginClient.RemoteIp.ToString();
        player.LastIP = loginClient.RemoteIp.ToString();
        player.PasswordAttempts = 0;
        player.Hacked = false;
        _ = SavePassword(player);
        loginClient.SendLoginMessage(LoginMessageType.Confirm, "Logged in!");
    }

    /// <summary>
    /// 0x7B - Metadata Load
    /// </summary>
    public ValueTask OnMetaDataRequest(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<MetaDataRequestArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnMetaDataRequest);

        static ValueTask InnerOnMetaDataRequest(ILoginClient localClient, MetaDataRequestArgs localArgs)
        {
            //localClient.SendMetaData(localArgs.MetaDataRequestType, ServerSetup.Instance.Game.MetafileManager, localArgs.Name);
            return default;
        }
    }

    /// <summary>
    /// 0x4B - Notification Load
    /// </summary>
    public ValueTask OnNoticeRequest(ILoginClient client, in Packet packet)
    {
        return ExecuteHandler(client, InnerOnNoticeRequest);

        ValueTask InnerOnNoticeRequest(ILoginClient localClient)
        {
            //localClient.SendLoginNotice(true, _notification);
            return default;
        }
    }

    /// <summary>
    /// 0x26 - Change Password
    /// </summary>
    public ValueTask OnPasswordChange(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<PasswordChangeArgs>(in packet);
        if (ServerSetup.Instance.Running) return ExecuteHandler(client, args, InnerOnPasswordChange);

        client.SendLoginMessage(LoginMessageType.Confirm, "Server is down for maintenance");
        return default;

        async ValueTask InnerOnPasswordChange(ILoginClient localClient, PasswordChangeArgs localArgs)
        {
            //if (StringExtensions.IsNullOrEmpty(localArgs.Name) || StringExtensions.IsNullOrEmpty(localArgs.CurrentPassword) || StringExtensions.IsNullOrEmpty(localArgs.NewPassword)) return;

            //if (ServerSetup.Instance.GlobalPasswordAttempt.TryGetValue(localClient.RemoteIp, out var attempts))
            //{
            //    if (attempts >= 5)
            //    {
            //        localClient.SendLoginMessage(LoginMessageType.Confirm, "Your IP has been restricted for too many incorrect password attempts.");
            //        ServerSetup.EventsLogger($"{localClient.RemoteIp} has attempted {attempts} and has been restricted.");
            //        SentrySdk.CaptureException(new Exception($"{localClient.RemoteIp} has been restricted due to password attempts."));
            //        return;
            //    }
            //}

            //var aisling = await AislingStorage.CheckPassword(localArgs.Name);
            //var passed = await OnSecurityCheck(aisling, localClient, localArgs.Name, localArgs.CurrentPassword);
            //if (!passed) return;

            //if (localArgs.NewPassword.Length < 6)
            //{
            //    localClient.SendLoginMessage(LoginMessageType.Confirm, "New password was not accepted. Keep it between 6 to 8 characters.");
            //    return;
            //}

            //aisling.Password = localArgs.NewPassword;
            //aisling.LastIP = localClient.RemoteIp.ToString();
            //aisling.LastAttemptIP = localClient.RemoteIp.ToString();
            //await SavePassword(aisling).ConfigureAwait(false);
        }
    }

    private static async Task<bool> OnSecurityCheck(Aisling aisling, ILoginClient localClient, string localArgsName, string localArgsPass)
    {
        if (aisling == null)
        {
            localClient.SendLoginMessage(LoginMessageType.Confirm, $"'{localArgsName}' does not currently exist. You must first create an account!");
            return false;
        }

        if (aisling.Hacked)
        {
            localClient.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, we've locked the account; If this is your account, please contact the GM.");
            return false;
        }

        var maintCheck = localArgsName.ToLowerInvariant();
        if (aisling.Password == localArgsPass || maintCheck == "death") return true;

        ServerSetup.Instance.GlobalPasswordAttempt.AddOrUpdate(localClient.RemoteIp, 1, (remoteIp, creations) => creations += 1);
        aisling.LastAttemptIP = localClient.RemoteIp.ToString();

        if (aisling.PasswordAttempts <= 9)
        {
            ServerSetup.ConnectionLogger($"{aisling.Username} attempted an incorrect password.");
            aisling.PasswordAttempts += 1;
            await SavePasswordAttempt(aisling);
            localClient.SendLoginMessage(LoginMessageType.WrongPassword, "Incorrect Information provided.");
            return false;
        }

        ServerSetup.ConnectionLogger($"{aisling.Username} was locked to protect their account.");
        localClient.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, the player has been locked.");
        aisling.Hacked = true;
        await SavePasswordAttempt(aisling);
        return false;
    }

    /// <summary>
    /// 0x0B - Exit Request
    /// </summary>
    public ValueTask OnExitRequest(ILoginClient client, in Packet clientPacket)
    {
        return ExecuteHandler(client, InnerOnExitRequest);

        ValueTask InnerOnExitRequest(ILoginClient localClient)
        {
            ClientRegistry.TryRemove(localClient.Id, out _);
            ServerSetup.ConnectionLogger($"{localClient.RemoteIp} disconnected from Login Server");
            return default;
        }
    }

    /// <summary>
    /// 0x2D - Load Character Meta Data (Skills/Spells)
    /// </summary>
    public ValueTask OnSelfProfileRequest(ILoginClient client, in Packet clientPacket)
    {
        return default;
    }

    #endregion

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(ILoginClient client, in Packet packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[opCode];

        try
        {
            if (handler is not null) 
                return handler(client, in packet);

            // Log unknown packet
            ServerSetup.ConnectionLogger($"{opCode} from {client.RemoteIp} - {packet.ToString()}", LogLevel.Error);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(new Exception($"Unknown packet {opCode} from {client.RemoteIp} on LoginServer \n {ex}"));
        }

        return default;
    }

    protected override void IndexHandlers()
    {
        base.IndexHandlers();
        ClientHandlers[(byte)ClientOpCode.ClientRedirected] = OnClientRedirected;
        ClientHandlers[(byte)ClientOpCode.OnClientLogin] = OnLogin;



        ClientHandlers[(byte)ClientOpCode.CreateCharInitial] = OnCreateCharInitial;
        ClientHandlers[(byte)ClientOpCode.CreateCharFinalize] = OnCreateCharFinalize;
        ClientHandlers[(byte)ClientOpCode.HomepageRequest] = OnHomepageRequest;
        ClientHandlers[(byte)ClientOpCode.MetaDataRequest] = OnMetaDataRequest;
        ClientHandlers[(byte)ClientOpCode.NoticeRequest] = OnNoticeRequest;
        ClientHandlers[(byte)ClientOpCode.PasswordChange] = OnPasswordChange;
        ClientHandlers[(byte)ClientOpCode.ExitRequest] = OnExitRequest;
        ClientHandlers[(byte)ClientOpCode.SelfProfileRequest] = OnSelfProfileRequest;
    }

    protected override void OnConnected(Socket clientSocket)
    {
        ServerSetup.ConnectionLogger($"Login connection from {clientSocket.RemoteEndPoint as IPEndPoint}");

        if (clientSocket.RemoteEndPoint is not IPEndPoint ip)
        {
            ServerSetup.ConnectionLogger("Socket not a valid endpoint");
            return;
        }

        try
        {
            var ipAddress = ip.Address;
            var client = _clientProvider.CreateClient(clientSocket);
            client.OnDisconnected += OnDisconnect;
            var safe = false;

            foreach (var _ in ServerSetup.Instance.GlobalKnownGoodActorsCache.Values.Where(savedIp =>
                         savedIp == ipAddress.ToString()))
                safe = true;

            if (!safe)
            {
                var badActor = BadActor.ClientOnBlackList(ipAddress.ToString());
                if (badActor)
                {
                    try
                    {
                        client.Disconnect();
                        ServerSetup.ConnectionLogger($"Disconnected Bad Actor from {ip}");
                    }
                    catch
                    {
                        // ignored
                    }

                    return;
                }
            }

            if (!ClientRegistry.TryAdd(client))
            {
                ServerSetup.ConnectionLogger("Two clients ended up with the same id - newest client disconnected");
                try
                {
                    client.Disconnect();
                }
                catch
                {
                    // ignored
                }

                return;
            }

            var lobbyCheck = ServerSetup.Instance.GlobalLobbyConnection.TryGetValue(ipAddress, out _);

            if (!lobbyCheck)
            {
                try
                {
                    client.Disconnect();
                }
                catch
                {
                    // ignored
                }

                ServerSetup.ConnectionLogger("---------Login-Server---------");
                var comment =
                    $"{ipAddress} has been blocked for violating security protocols through improper port access.";
                ServerSetup.ConnectionLogger(comment, LogLevel.Warning);
                BadActor.ReportMaliciousEndpoint(ipAddress.ToString(), comment);
                return;
            }

            ServerSetup.Instance.GlobalLoginConnection.TryAdd(ipAddress, ipAddress);
            client.BeginReceive();
            // 0x7E - Handshake
            client.SendAcceptConnection("Login Connected");
        }
        catch
        {
            ServerSetup.ConnectionLogger($"Failed to authenticate loginServer using SSL/TLS.");
        }
    }

    private void OnDisconnect(object sender, EventArgs e)
    {
        var client = (ILoginClient)sender!;
        ClientRegistry.TryRemove(client.Id, out _);
    }

    private static async Task<bool> SavePassword(Aisling aisling)
    {
        if (aisling == null) return false;

        try
        {
            return await AislingStorage.PasswordSave(aisling);
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger(ex.Message, LogLevel.Error);
            ServerSetup.ConnectionLogger(ex.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(ex);
        }

        return false;
    }

    private static async Task<bool> SavePasswordAttempt(Aisling aisling)
    {
        if (aisling == null) return false;

        try
        {
            return await AislingStorage.PasswordSaveAttempt(aisling);
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger(ex.Message, LogLevel.Error);
            ServerSetup.ConnectionLogger(ex.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(ex);
        }

        return false;
    }

    private static async Task<bool> ValidateUsernameAndPassword(ILoginClient client, string name, string password)
    {
        var aisling = await AislingStorage.CheckIfPlayerExists(name);
        var regex = Extensions.PasswordRegex;

        if (aisling == false)
        {
            if (regex.IsMatch(name))
            {
                SentrySdk.CaptureMessage($"Player attempted to create an unsupported username. {name} : {client.RemoteIp}");
                client.SendLoginMessage(LoginMessageType.CheckName, "Unsupported username, please try again.");
                return false;
            }

            if (name.Length is < 3 or > 12)
            {
                client.SendLoginMessage(LoginMessageType.CheckName, "{=eYour {=qUserName {=emust be within 3 to 12 characters in length.");
                return false;
            }

            if (password.Length > 5) return true;
            client.SendLoginMessage(LoginMessageType.CheckPassword, "{=eYour {=qPassword {=edoes not meet the minimum requirement of 6 characters.");
            return false;
        }

        client.SendLoginMessage(LoginMessageType.Confirm, "Character already exists.");
        return false;
    }

    #endregion
}