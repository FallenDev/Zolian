using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Chaos.Cryptography;
using Chaos.Extensions.Common;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Options;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

using Darkages.Database;
using Darkages.Interfaces;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;
using Microsoft.Extensions.Logging;
using RestSharp;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Darkages.Managers;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Gender = Darkages.Enums.Gender;
using Redirect = Chaos.Networking.Entities.Redirect;
using ServerOptions = Chaos.Networking.Options.ServerOptions;
using ILoginClient = Darkages.Network.Client.Abstractions.ILoginClient;
using StringExtensions = ServiceStack.StringExtensions;
using System.Text;
using Darkages.Common;

namespace Darkages.Network.Server;

[UsedImplicitly]
public sealed partial class LoginServer : ServerBase<ILoginClient>, ILoginServer<ILoginClient>
{
    private readonly IClientFactory<LoginClient> _clientProvider;
    private readonly Notification _notification;
    private const string InternalIP = "192.168.50.1"; // Cannot use ServerConfig due to value needing to be constant
    private static readonly string[] GameMastersIPs = ServerSetup.Instance.GameMastersIPs;
    private ConcurrentDictionary<uint, CreateCharInitialArgs> CreateCharRequests { get; }

    public LoginServer(
        IClientRegistry<ILoginClient> clientRegistry,
        IClientFactory<LoginClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        ILogger<LoginServer> logger
    )
        : base(redirectManager, packetSerializer, clientRegistry, Microsoft.Extensions.Options.Options.Create(new ServerOptions
        {
            Address = ServerSetup.Instance.IpAddress,
            Port = ServerSetup.Instance.Config.LOGIN_PORT
        }), logger)
    {
        ServerSetup.Instance.LoginServer = this;
        _clientProvider = clientProvider;
        _notification = Notification.FromFile("Notification.txt");
        CreateCharRequests = [];
        IndexHandlers();
    }

    #region OnHandlers

    /// <summary>
    /// 0x57 - Server Table & Redirect
    /// </summary>
    public ValueTask OnClientRedirected(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<ClientRedirectedArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnClientRedirect);

        ValueTask InnerOnClientRedirect(ILoginClient localClient, ClientRedirectedArgs localArgs)
        {
            var reservedRedirect = ServerSetup.Instance.Config.ReservedRedirects
                .FirstOrDefault(rr => rr.Id == localArgs.Id && rr.Name.EqualsI(localArgs.Name));

            if (reservedRedirect != null)
            {
                localClient.Crypto = new Crypto(localArgs.Seed, localArgs.Key, string.Empty);
                localClient.SendLoginNotice(false, _notification);
            }
            else if (RedirectManager.TryGetRemove(localArgs.Id, out var redirect))
            {
                localClient.Crypto = new Crypto(redirect.Seed, redirect.Key, redirect.Name);
                localClient.SendLoginNotice(false, _notification);
            }
            else
            {
                ServerSetup.ConnectionLogger($"Attempt to redirect with invalid redirect details, {localClient.RemoteIp}");
                SentrySdk.CaptureMessage($"Attempt to redirect with invalid redirect details, {localClient.RemoteIp}");
                localClient.Disconnect();
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
                    Gender = (Gender)localArgs.Gender,
                    HairColor = (byte)localArgs.HairColor,
                    HairStyle = localArgs.HairStyle,
                    BodyColor = 0,
                    SkillBook = new SkillBook(),
                    SpellBook = new SpellBook(),
                    Inventory = new InventoryManager(),
                    BankManager = new BankManager(),
                    EquipmentManager = new EquipmentManager(null)
                };

                await StorageManager.AislingBucket.Create(user).ConfigureAwait(true);
                ServerSetup.Instance.GlobalCreationCount.AddOrUpdate(localClient.RemoteIp, 1, (remoteIp, creations) => creations += 1);
                localClient.SendLoginMessage(LoginMessageType.Confirm);
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
            ServerSetup.Instance.GlobalCreationCount.TryGetValue(localClient.RemoteIp, out var created);
            var result = await ValidateUsernameAndPassword(localClient, localArgs.Name, localArgs.Password);

            switch (result)
            {
                case true when created <= 2:
                    CreateCharRequests.AddOrUpdate(localClient.Id, localArgs, (_, _) => localArgs);
                    localClient.SendLoginMessage(LoginMessageType.Confirm, string.Empty);
                    break;
                case true:
                    localClient.SendLoginMessage(LoginMessageType.ClearNameMessage, "Slow down on character creation.");
                    break;
            }
        }
    }

    public ValueTask OnHomepageRequest(ILoginClient client, in Packet packet)
    {
        return ExecuteHandler(client, InnerOnHomepageRequest);

        static ValueTask InnerOnHomepageRequest(ILoginClient localClient)
        {
            localClient.SendLoginControl(LoginControlsType.Homepage, "https://www.darkages.com");
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

        client.SendLoginMessage(LoginMessageType.CharacterDoesntExist, "Server is down for maintenance");
        return default;

        async ValueTask InnerOnLogin(ILoginClient localClient, LoginArgs localArgs)
        {
            if (StringExtensions.IsNullOrEmpty(localArgs.Name) || StringExtensions.IsNullOrEmpty(localArgs.Password)) return;
            var result = await StorageManager.AislingBucket.CheckPassword(localArgs.Name);

            if (result == null)
            {
                localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, $"{{=q'{localArgs.Name}' {{=adoes not currently exist on this server. You can make this hero by clicking on 'Create'");
                return;
            }

            if (result.Password != localArgs.Password)
            {
                if (result.PasswordAttempts <= 9)
                {
                    ServerSetup.ConnectionLogger($"{localClient.RemoteIp}: {result.Username} attempted an incorrect password.");
                    result.LastIP = localClient.RemoteIp.ToString();
                    result.LastAttemptIP = localClient.RemoteIp.ToString();
                    result.PasswordAttempts += 1;
                    await SavePassword(result);
                    localClient.SendLoginMessage(LoginMessageType.WrongPassword, "Incorrect Information provided.");
                }
                else
                {
                    ServerSetup.ConnectionLogger($"{result.Username} was locked to protect their account.");
                    client.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, the player has been locked.");
                    result.LastIP = localClient.RemoteIp.ToString();
                    result.LastAttemptIP = localClient.RemoteIp.ToString();
                    result.Hacked = true;
                    await SavePassword(result);
                }
                return;
            }

            var maintCheck = localArgs.Name.ToLowerInvariant();
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
                Encoding.ASCII.GetString(localClient.Crypto.Key),
                localClient.Crypto.Seed,
                localArgs.Name);

            switch (maintCheck)
            {
                case "asdf":
                    localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, "Locked Account, denied access");
                    return;
                case "death":
                    {
                        var ipLocal = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                        if (IPAddress.IsLoopback(localClient.RemoteIp) || localClient.RemoteIp.Equals(ipLocal))
                        {
                            result.LastAttemptIP = localClient.RemoteIp.ToString();
                            result.LastIP = localClient.RemoteIp.ToString();
                            if (result.Password == ServerSetup.Instance.Unlock)
                            {
                                result.PasswordAttempts = 0;
                                result.Hacked = false;
                            }
                            await SavePassword(result);
                            RedirectManager.Add(redirect);
                            localClient.SendLoginMessage(LoginMessageType.Confirm);
                            localClient.SendRedirect(redirect);
                            return;
                        }

                        localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, "GM Account, denied access");
                        return;
                    }
                case "alvino":
                    {
                        var ipLocal = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                        if (GameMastersIPs.Any(ip => localClient.RemoteIp.Equals(IPAddress.Parse(ip))) 
                            || IPAddress.IsLoopback(localClient.RemoteIp) || localClient.RemoteIp.Equals(ipLocal))
                        {
                            result.LastAttemptIP = localClient.RemoteIp.ToString();
                            result.LastIP = localClient.RemoteIp.ToString();
                            if (result.Password == ServerSetup.Instance.Unlock)
                            {
                                result.PasswordAttempts = 0;
                                result.Hacked = false;
                            }
                            await SavePassword(result);
                            RedirectManager.Add(redirect);
                            localClient.SendLoginMessage(LoginMessageType.Confirm);
                            localClient.SendRedirect(redirect);
                            return;
                        }

                        localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, "GM Account, denied access");
                        return;
                    }
                default:
                    result.LastAttemptIP = localClient.RemoteIp.ToString();
                    result.LastIP = localClient.RemoteIp.ToString();
                    if (result.Password == ServerSetup.Instance.Unlock)
                    {
                        result.PasswordAttempts = 0;
                        result.Hacked = false;
                    }
                    await SavePassword(result);
                    break;
            }

            if (result.Hacked)
            {
                localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, "Bruteforce detected, we've locked the account to protect it; If this is your account, please contact the GM.");
                return;
            }

            result.PasswordAttempts = 0;
            await SavePassword(result);
            RedirectManager.Add(redirect);
            localClient.SendLoginMessage(LoginMessageType.Confirm);
            localClient.SendRedirect(redirect);
        }
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
            localClient.SendMetaData(localArgs.MetaDataRequestType, new MetafileManager(), localArgs.Name);
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
            localClient.SendLoginNotice(true, _notification);
            return default;
        }
    }

    /// <summary>
    /// 0x26 - Change Password
    /// </summary>
    public ValueTask OnPasswordChange(ILoginClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<PasswordChangeArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnPasswordChange);

        static async ValueTask InnerOnPasswordChange(ILoginClient localClient, PasswordChangeArgs localArgs)
        {
            if (StringExtensions.IsNullOrEmpty(localArgs.Name) || StringExtensions.IsNullOrEmpty(localArgs.CurrentPassword) || StringExtensions.IsNullOrEmpty(localArgs.NewPassword)) return;
            var aisling = StorageManager.AislingBucket.CheckPassword(localArgs.Name);

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

            if (aisling.Result.Password != localArgs.CurrentPassword)
            {
                if (aisling.Result.PasswordAttempts <= 9)
                {
                    ServerSetup.ConnectionLogger($"{aisling.Result} attempted an incorrect password.");
                    aisling.Result.LastIP = localClient.RemoteIp.ToString();
                    aisling.Result.LastAttemptIP = localClient.RemoteIp.ToString();
                    aisling.Result.PasswordAttempts += 1;
                    await SavePassword(aisling.Result);
                    localClient.SendLoginMessage(LoginMessageType.Confirm, "Incorrect Information provided.");
                    return;
                }

                ServerSetup.ConnectionLogger($"{aisling.Result} was locked to protect their account.");
                localClient.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, the player has been locked.");
                aisling.Result.LastIP = localClient.RemoteIp.ToString();
                aisling.Result.LastAttemptIP = localClient.RemoteIp.ToString();
                aisling.Result.Hacked = true;
                await SavePassword(aisling.Result);
                return;
            }

            if (localArgs.NewPassword.Length < 6)
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "New password was not accepted. Keep it between 6 to 8 characters.");
                return;
            }

            aisling.Result.Password = localArgs.NewPassword;
            aisling.Result.LastIP = localClient.RemoteIp.ToString();
            aisling.Result.LastAttemptIP = localClient.RemoteIp.ToString();
            await SavePassword(aisling.Result).ConfigureAwait(false);
        }
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

    #endregion

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(ILoginClient client, in Packet packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[opCode];

        try
        {
            if (handler is not null) return handler(client, in packet);
            ServerSetup.PacketLogger("//////////////// Handled Login Server Unknown Packet ////////////////", LogLevel.Error);
            ServerSetup.PacketLogger($"{opCode} from {client.RemoteIp}", LogLevel.Error);
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
        ClientHandlers[(byte)ClientOpCode.CreateCharInitial] = OnCreateCharInitial;
        ClientHandlers[(byte)ClientOpCode.CreateCharFinalize] = OnCreateCharFinalize;
        ClientHandlers[(byte)ClientOpCode.ClientRedirected] = OnClientRedirected;
        ClientHandlers[(byte)ClientOpCode.HomepageRequest] = OnHomepageRequest;
        ClientHandlers[(byte)ClientOpCode.Login] = OnLogin;
        ClientHandlers[(byte)ClientOpCode.MetaDataRequest] = OnMetaDataRequest;
        ClientHandlers[(byte)ClientOpCode.NoticeRequest] = OnNoticeRequest;
        ClientHandlers[(byte)ClientOpCode.PasswordChange] = OnPasswordChange;
        ClientHandlers[(byte)ClientOpCode.ExitRequest] = OnExitRequest;
    }

    protected override void OnConnected(Socket clientSocket)
    {
        ServerSetup.ConnectionLogger($"Login connection from {clientSocket.RemoteEndPoint as IPEndPoint}");

        if (clientSocket.RemoteEndPoint is not IPEndPoint ip)
        {
            ServerSetup.ConnectionLogger("Socket not a valid endpoint");
            return;
        }

        var ipAddress = ip.Address;
        var client = _clientProvider.CreateClient(clientSocket);
        client.OnDisconnected += OnDisconnect;
        var safe = false;

        foreach (var _ in ServerSetup.Instance.GlobalKnownGoodActorsCache.Values.Where(savedIp => savedIp == ipAddress.ToString()))
            safe = true;

        if (!safe)
        {
            var badActor = ClientOnBlackList(ipAddress.ToString());
            if (badActor)
            {
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
            var comment = $"{ipAddress} has been blocked for violating security protocols through improper port access.";
            ServerSetup.ConnectionLogger(comment, LogLevel.Warning);
            ReportEndpoint(ipAddress.ToString(), comment);
            return;
        }

        ServerSetup.Instance.GlobalLoginConnection.TryAdd(ipAddress, ipAddress);
        client.BeginReceive();
        // 0x7E - Handshake
        client.SendAcceptConnection("CONNECTED SERVER");
    }

    private void OnDisconnect(object sender, EventArgs e)
    {
        var client = (ILoginClient)sender!;
        ClientRegistry.TryRemove(client.Id, out _);
    }

    private bool ClientOnBlackList(string remoteIp)
    {
        switch (remoteIp)
        {
            case "127.0.0.1":
            case InternalIP:
                return false;
        }

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode is null || keyCode.Length == 0)
            {
                ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest("", Method.Get);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", remoteIp);
            request.AddParameter("maxAgeInDays", "90");
            request.AddParameter("verbose", "");
            var response = ServerSetup.Instance.RestClient.Execute<Ipdb>(request);

            if (response.IsSuccessful)
            {
                var json = response.Content;

                if (json is null || json.Length == 0)
                {
                    ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, response is null or length is 0");
                    return false;
                }

                var ipdb = JsonConvert.DeserializeObject<Ipdb>(json!);
                var abuseConfidenceScore = ipdb?.Data?.AbuseConfidenceScore;
                var tor = ipdb?.Data?.IsTor;
                var usageType = ipdb?.Data?.UsageType;

                if (tor == true)
                {
                    ServerSetup.ConnectionLogger("---------Login-Server---------");
                    ServerSetup.ConnectionLogger($"{remoteIp} is using tor and automatically blocked", LogLevel.Warning);
                    SentrySdk.CaptureMessage($"{remoteIp} has a confidence score of {abuseConfidenceScore}, and was using tor, and IP type: {usageType}");
                    return true;
                }

                if (usageType == "Reserved")
                {
                    ServerSetup.ConnectionLogger("---------Login-Server---------");
                    ServerSetup.ConnectionLogger($"{remoteIp} was blocked due to being a reserved address (bogon)", LogLevel.Warning);
                    SentrySdk.CaptureMessage($"{remoteIp} has a confidence score of {abuseConfidenceScore}, and was using a Reserved Address");
                    return true;
                }

                if (usageType == "Data Center/Web Hosting/Transit")
                {
                    ServerSetup.ConnectionLogger("---------Login-Server---------");
                    ServerSetup.ConnectionLogger($"{remoteIp} was blocked due to being a data center, web hosting, or transit address", LogLevel.Warning);
                    SentrySdk.CaptureMessage($"{remoteIp} has a confidence score of {abuseConfidenceScore}, and is a data center, web host, or transit service.");
                    return true;
                }

                switch (abuseConfidenceScore)
                {
                    case >= 5:
                        ServerSetup.ConnectionLogger("---------Login-Server---------");
                        var comment = $"{remoteIp} has been blocked due to a high risk assessment score of {abuseConfidenceScore}, indicating a recognized malicious entity.";
                        ServerSetup.ConnectionLogger(comment, LogLevel.Warning);
                        SentrySdk.CaptureMessage($"{remoteIp} has a confidence score of {abuseConfidenceScore}, is using tor: {tor}, and IP type: {usageType}");
                        ReportEndpoint(remoteIp, comment);
                        return true;
                    case >= 0:
                        return false;
                    case null:
                        // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                        ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, confidence score was null");
                        return false;
                }
            }
            else
            {
                // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, response was not successful");
                return false;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger("Unknown issue with IPDB, connections refused", LogLevel.Warning);
            ServerSetup.ConnectionLogger($"{ex}");
            SentrySdk.CaptureException(ex);
            return false;
        }

        return true;
    }

    private void ReportEndpoint(string remoteIp, string comment)
    {
        var keyCode = ServerSetup.Instance.KeyCode;
        if (keyCode is null || keyCode.Length == 0)
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", "14, 15, 16, 21");
            request.AddParameter("comment", comment);
            var response = ServerSetup.Instance.RestReport.Execute(request);

            if (response.IsSuccessful) return;
            ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            SentrySdk.CaptureMessage($"Error reporting {remoteIp} : {comment}");
        }
        catch
        {
            // ignore
        }
    }

    private static async Task<bool> SavePassword(Aisling aisling)
    {
        if (aisling == null) return false;

        try
        {
            await StorageManager.AislingBucket.PasswordSave(aisling);
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger(ex.Message, LogLevel.Error);
            ServerSetup.ConnectionLogger(ex.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(ex);
        }

        return true;
    }

    private static async Task<bool> ValidateUsernameAndPassword(ILoginClient client, string name, string password)
    {
        var aisling = await StorageManager.AislingBucket.CheckIfPlayerExists(name);
        var regex = Extensions.PasswordRegex;

        if (aisling == false)
        {
            if (regex.IsMatch(name))
            {
                SentrySdk.CaptureMessage($"Player attempted to create an unsupported username. {name} : {client.RemoteIp}");
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

        client.SendLoginMessage(LoginMessageType.ClearNameMessage, "Character already exists.");
        return false;
    }

    #endregion
}