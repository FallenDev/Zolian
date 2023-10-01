using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

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
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;

using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using RestSharp;

using Gender = Darkages.Enums.Gender;
using Redirect = Chaos.Networking.Entities.Redirect;
using ServerOptions = Chaos.Networking.Options.ServerOptions;

namespace Darkages.Network.Server;

public sealed partial class LoginServer : ServerBase<ILoginClient>, ILoginServer<ILoginClient>
{
    private readonly IClientFactory<LoginClient> _clientProvider;
    private readonly Notification _notification;
    private readonly RestClient _restClient = new("https://api.abuseipdb.com/api/v2/check");
    private const string InternalIP = "192.168.50.1"; // Cannot use ServerConfig due to value needing to be constant
    private const string GameMasterIpA = "75.226.159.140";
    private const string GameMasterIpB = "24.137.144.53";
    private ConcurrentDictionary<uint, CreateCharRequestArgs> CreateCharRequests { get; }

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
                ServerSetup.Logger($"Attempt to redirect with invalid redirect details, {localClient.RemoteIp}");
                Analytics.TrackEvent($"Attempt to redirect with invalid redirect details, {localClient.RemoteIp}");
                localClient.Disconnect();
            }

            return default;
        }
    }

    /// <summary>
    /// 0x04 - Create New Player from Template
    /// </summary>
    public ValueTask OnCreateCharFinalize(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharFinalizeArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnCreateCharFinalize);

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
                    BodyColor = 0,
                    SkillBook = new SkillBook(),
                    SpellBook = new SpellBook(),
                    Inventory = new Inventory(),
                    BankManager = new Bank(),
                    EquipmentManager = new EquipmentManager(null)
                };

                await StorageManager.AislingBucket.Create(user).ConfigureAwait(true);
                localClient.SendLoginMessage(LoginMessageType.Confirm);
            }
            else
                localClient.SendLoginMessage(LoginMessageType.ClearNameMessage, "Unable to create character, bad request.");
        }
    }

    /// <summary>
    /// 0x02 - Character Creation Checks
    /// </summary>
    public ValueTask OnCreateCharRequest(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<CreateCharRequestArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnCreateCharRequest);

        async ValueTask InnerOnCreateCharRequest(ILoginClient localClient, CreateCharRequestArgs localArgs)
        {
            var result = await ValidateUsernameAndPassword(localClient, localArgs.Name, localArgs.Password);

            if (result)
            {
                CreateCharRequests.AddOrUpdate(localClient.Id, localArgs, (_, _) => localArgs);
                localClient.SendLoginMessage(LoginMessageType.Confirm, string.Empty);
            }
            else
            {
                ServerSetup.Logger($"Character creation failed - {localArgs.Name} - {localClient.RemoteIp}");
                Analytics.TrackEvent($"Character creation failed - {localArgs.Name} - {localClient.RemoteIp}");
            }
        }
    }

    public ValueTask OnHomepageRequest(ILoginClient client, in ClientPacket packet)
    {
        return ExecuteHandler(client, InnerOnHomepageRequest);

        static ValueTask InnerOnHomepageRequest(ILoginClient localClient)
        {
            localClient.SendLoginControls(LoginControlsType.Homepage, "https://classicrpgcharacter.nexon.com/service/ConfirmGameUser.aspx?id=%s&pw=%s&mainCode=2&subCode=0");
            return default;
        }
    }

    /// <summary>
    /// 0x03 - Player Login and Redirect
    /// </summary>
    public ValueTask OnLogin(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<LoginArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnLogin);

        async ValueTask InnerOnLogin(ILoginClient localClient, LoginArgs localArgs)
        {
            var (name, password) = localArgs;
            if (name.IsNullOrEmpty() || password.IsNullOrEmpty()) return;
            var result = await StorageManager.AislingBucket.CheckPassword(name);

            if (result == null)
            {
                localClient.SendLoginMessage(LoginMessageType.CharacterDoesntExist, $"{{=q'{name}' {{=adoes not currently exist on this server. You can make this hero by clicking on 'Create'");
                return;
            }

            if (result.Password != password)
            {
                if (result.PasswordAttempts <= 9)
                {
                    ServerSetup.Logger($"{localClient.RemoteIp}: {result.Username} attempted an incorrect password.");
                    result.LastIP = localClient.RemoteIp.ToString();
                    result.LastAttemptIP = localClient.RemoteIp.ToString();
                    result.PasswordAttempts += 1;
                    await SavePassword(result);
                    localClient.SendLoginMessage(LoginMessageType.WrongPassword, "Incorrect Information provided.");
                }
                else
                {
                    ServerSetup.Logger($"{result.Username} was locked to protect their account.");
                    client.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, the player has been locked.");
                    result.LastIP = localClient.RemoteIp.ToString();
                    result.LastAttemptIP = localClient.RemoteIp.ToString();
                    result.Hacked = true;
                    await SavePassword(result);
                }
                return;
            }

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
                            result.LastAttemptIP = localClient.RemoteIp.ToString();
                            result.LastIP = localClient.RemoteIp.ToString();
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
                case "scythe":
                    {
                        var gmA = IPAddress.Parse(GameMasterIpA);
                        var gmB = IPAddress.Parse(GameMasterIpB);
                        var ipLocal = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                        if (localClient.RemoteIp.Equals(gmA) || localClient.RemoteIp.Equals(gmB) || localClient.IsLoopback() || localClient.RemoteIp.Equals(ipLocal))
                        {
                            result.LastAttemptIP = localClient.RemoteIp.ToString();
                            result.LastIP = localClient.RemoteIp.ToString();
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
                    {
                        result.Hacked = false;
                    }
                    await SavePassword(result);
                    break;
            }

            if (result.Hacked)
            {
                localClient.SendLoginMessage(LoginMessageType.Confirm, "Hacking detected, we've locked the account; If this is your account, please contact the GM.");
                return;
            }

            RedirectManager.Add(redirect);
            localClient.SendLoginMessage(LoginMessageType.Confirm);
            localClient.SendRedirect(redirect);
        }
    }

    /// <summary>
    /// 0x7B - Metadata Load
    /// </summary>
    public ValueTask OnMetaDataRequest(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<MetaDataRequestArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnMetaDataRequest);

        static ValueTask InnerOnMetaDataRequest(ILoginClient localClient, MetaDataRequestArgs localArgs)
        {
            var (metadataRequestType, name) = localArgs;
            localClient.SendMetaData(metadataRequestType, new MetafileManager(), name);
            return default;
        }
    }

    /// <summary>
    /// 0x4B - Notification Load
    /// </summary>
    public ValueTask OnNoticeRequest(ILoginClient client, in ClientPacket packet)
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
    public ValueTask OnPasswordChange(ILoginClient client, in ClientPacket packet)
    {
        var args = PacketSerializer.Deserialize<PasswordChangeArgs>(in packet);
        return ExecuteHandler(client, args, InnerOnPasswordChange);

        static async ValueTask InnerOnPasswordChange(ILoginClient localClient, PasswordChangeArgs localArgs)
        {
            var (name, currentPassword, newPassword) = localArgs;
            if (name.IsNullOrEmpty() || currentPassword.IsNullOrEmpty() || newPassword.IsNullOrEmpty()) return;
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
            await SavePassword(aisling.Result).ConfigureAwait(false);
        }
    }

    #endregion

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(ILoginClient client, in ClientPacket packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[(byte)packet.OpCode];
        if (handler != null) return handler(client, in packet);
        ServerSetup.Logger($"Unknown message to login server with code {opCode} from {client.RemoteIp}");
        Analytics.TrackEvent($"Unknown message to login server with code {opCode} from {client.RemoteIp}");
        return default;
    }

    protected override void IndexHandlers()
    {
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
        ServerSetup.Logger($"Login connection from {ip}");

        try
        {
            await FinalizeConnectionAsync(clientSocket);
        }
        catch (Exception e)
        {
            Analytics.TrackEvent($"Failed to finalize connection {ip}");
            Crashes.TrackError(e);
        }
    }

    private async Task FinalizeConnectionAsync(Socket clientSocket)
    {
        var client = _clientProvider.CreateClient(clientSocket);
        var badActor = ClientOnBlackList(client);

        if (badActor)
        {
            client.Disconnect();
            return;
        }

        if (!ClientRegistry.TryAdd(client))
        {
            client.Disconnect();
            return;
        }

        var lobbyCheck = ServerSetup.Instance.GlobalLobbyConnection.TryGetValue(client.RemoteIp, out _);

        if (!lobbyCheck)
        {
            client.Disconnect();
            ServerSetup.Logger($"{client.RemoteIp} was blocked due to attempting bypass", LogLevel.Warning);
            return;
        }

        ServerSetup.Instance.GlobalLoginConnection.TryAdd(client.RemoteIp, client.RemoteIp);
        client.OnDisconnected += OnDisconnect;
        client.BeginReceive();
        client.SendAcceptConnection();
    }

    private void OnDisconnect(object sender, EventArgs e)
    {
        var client = (ILoginClient)sender!;
        ClientRegistry.TryRemove(client.Id, out _);
    }

    private bool ClientOnBlackList(ISocketClient client)
    {
        if (client == null) return true;

        switch (client.RemoteIp.ToString())
        {
            case "208.115.199.29": // uptimerrobot address - Do not allow it to go further than just pinging our IP
                return true;
            case "127.0.0.1":
            case InternalIP:
                return false;
        }

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode is null || keyCode.Length == 0)
            {
                ServerSetup.Logger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest("", Method.Get);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", client.RemoteIp.ToString());
            request.AddParameter("maxAgeInDays", "90");
            request.AddParameter("verbose", "");
            var response = _restClient.Execute<Ipdb>(request);

            if (response.IsSuccessful)
            {
                var json = response.Content;

                if (json is null || json.Length == 0)
                {
                    ServerSetup.Logger($"{client.RemoteIp} - API Issue, response is null or length is 0");
                    return false;
                }

                var ipdb = JsonConvert.DeserializeObject<Ipdb>(json!);
                var abuseConfidenceScore = ipdb?.Data?.AbuseConfidenceScore;
                var tor = ipdb?.Data?.IsTor;
                var usageType = ipdb?.Data?.UsageType;

                Analytics.TrackEvent($"{client.RemoteIp} has a confidence score of {abuseConfidenceScore}, is using tor: {tor}, and IP type: {usageType}");

                if (tor == true)
                {
                    ServerSetup.Logger("---------Login-Server---------");
                    ServerSetup.Logger($"{client.RemoteIp} is using tor and automatically blocked", LogLevel.Warning);
                    return true;
                }

                if (usageType == "Reserved")
                {
                    ServerSetup.Logger("---------Login-Server---------");
                    ServerSetup.Logger($"{client.RemoteIp} was blocked due to being a reserved address (bogon)", LogLevel.Warning);
                    return true;
                }

                switch (abuseConfidenceScore)
                {
                    case >= 5:
                        ServerSetup.Logger("---------Login-Server---------");
                        var comment = $"{client.RemoteIp} was blocked with a score of {abuseConfidenceScore}";
                        ServerSetup.Logger(comment, LogLevel.Warning);
                        ReportEndpoint(client, comment);
                        return true;
                    case >= 0:
                        return false;
                    case null:
                        // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                        ServerSetup.Logger($"{client.RemoteIp} - API Issue, confidence score was null");
                        return false;
                }
            }
            else
            {
                // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                ServerSetup.Logger($"{client.RemoteIp} - API Issue, response was not successful");
                return false;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.Logger("Unknown issue with IPDB, connections refused", LogLevel.Warning);
            ServerSetup.Logger($"{ex}");
            Crashes.TrackError(ex);
            return true;
        }

        return true;
    }

    private void ReportEndpoint(ISocketClient client, string comment)
    {
        var keyCode = ServerSetup.Instance.KeyCode;
        if (keyCode is null || keyCode.Length == 0)
        {
            ServerSetup.Logger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        var request = new RestRequest("", Method.Post);
        request.AddHeader("Key", keyCode);
        request.AddHeader("Accept", "application/json");
        request.AddParameter("ip", client.RemoteIp.ToString());
        request.AddParameter("categories", "14, 15, 16, 21");
        request.AddParameter("comment", comment);
        _restClient.Execute(request);
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
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }

        return true;
    }

    private static async Task<bool> ValidateUsernameAndPassword(ILoginClient client, string name, string password)
    {
        var aisling = await StorageManager.AislingBucket.CheckIfPlayerExists(name);
        var regex = PasswordRegex();

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

    [GeneratedRegex("(?:[^a-z]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex PasswordRegex();
    #endregion
}