using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

using Darkages.Database;
using Darkages.Enums;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ClientFormats;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Sprites;
using Darkages.Types;

using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using RestSharp;

using ServiceStack;

namespace Darkages.Network.Server;

public class LoginServer : NetworkServer<LoginClient>
{
    private readonly RestClient _restClient = new("https://api.abuseipdb.com/api/v2/check");

    public LoginServer()
    {
        MServerTable = MServerTable.FromFile("MServerTable.xml");
        Notification = Notification.FromFile("Notification.txt");
    }

    private static MServerTable MServerTable { get; set; }
    private static Notification Notification { get; set; }

    /// <summary>
    /// Lobby Connection - First client-side checks
    /// </summary>
    protected override async void ClientConnected(LoginClient client)
    {
        if (client == null) return;

        client.ClientIP = client.Socket.RemoteEndPoint as IPEndPoint;
        var checkedIp = CheckIfIpHasAlreadyBeenChecked(client.ClientIP);

        if (!checkedIp)
        {
            var badActor = await ClientOnBlackList(client, client.ClientIP);

            if (badActor)
            {
                ServerSetup.Logger($"{client.ClientIP!.Address} was detected as potentially malicious", LogLevel.Critical);
                ClientDisconnected(client);
                RemoveClient(client);
                return;
            }

            if (!client.Socket.Connected)
            {
                return;
            }
        }

        client.Authorized = true;
        client.Send(new ServerFormat7E());
    }

    /// <summary>
    /// Client versioning check
    /// </summary>
    protected override void Format00Handler(LoginClient client, ClientFormat00 format)
    {
        if (client == null) return;
        if (format.Version != ServerSetup.Instance.Config.ClientVersion)
        {
            ServerSetup.Logger($"An attempted use of an incorrect client was detected. {client.Serial}", LogLevel.Critical);
            client.SendMessageBox(0x08, "You're not using an authorized client. Please visit https://www.TheBuckNetwork.com/Zolian for the latest client.");
            ClientDisconnected(client);
            RemoveClient(client);
            return;
        }

        if (client.Authorized)
        {
            client.Send(new ServerFormat00
            {
                Type = 0x00,
                Hash = MServerTable.Hash,
                Parameters = client.Encryption.Parameters
            });
        }
    }

    /// <summary>
    /// Client IP Check - Blacklist and BOGON list checks
    /// </summary>
    /// <returns>Boolean, whether or not the IP has been listed as valid</returns>
    private async Task<bool> ClientOnBlackList(LoginClient client, IPEndPoint endPoint)
    {
        if (client == null) return true;
        const char delimiter = ':';
        var ipToString = client.Socket.RemoteEndPoint?.ToString();
        var ipSplit = ipToString?.Split(delimiter);
        var ip = ipSplit?[0];
        var tokenSource = new CancellationTokenSource(5000);

        switch (ip)
        {
            case null:
                client.Authorized = false;
                return true;
            case "208.115.199.29": // uptimerrobot ipaddress - Do not allow it to go further than just pinging our IP
                client.Authorized = false;
                try
                {
                    client.Socket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    client.Socket.Close();
                }
                return false;
            case "127.0.0.1":
            case "192.168.50.1": // Local Development Address withing your network
                ServerSetup.Logger("-----------------------------------");
                ServerSetup.Logger("Loopback IP & (Local) Authorized.");
                IpLookupConDict.TryAdd(Random.Shared.Next(), endPoint);
                client.Authorized = true;
                return false;
        }

        var bogonCheck = BogonCheck(client, ip);
        if (bogonCheck)
        {
            client.Authorized = false;
            return true;
        }

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode.IsNullOrEmpty())
            {
                ServerSetup.Logger("Keycode not valid or not set within ServerConfig.json");
                ServerSetup.Logger("Because of this, you're not protected from attackers");
                client.Authorized = true;
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest("");
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", ip);
            request.AddParameter("maxAgeInDays", "180");

            var response = await _restClient.ExecuteGetAsync<Ipdb>(request, tokenSource.Token);
            var json = response.Content;

            if (json.IsNullOrEmpty())
            {
                ServerSetup.Logger("-----------------------------------");
                ServerSetup.Logger("API Issue with IP database.");
                IpLookupConDict.TryAdd(Random.Shared.Next(), endPoint);
                client.Authorized = true;
                return false;
            }

            var ipdb = JsonConvert.DeserializeObject<Ipdb>(json!);
            var ipdbResponse = ipdb?.Data?.AbuseConfidenceScore;

            switch (ipdbResponse)
            {
                case >= 25:
                    Analytics.TrackEvent($"{ip} had a score of {ipdbResponse} and was blocked from accessing the server.");
                    ServerSetup.Logger("-----------------------------------");
                    ServerSetup.Logger($"{ip} was blocked with a score of {ipdbResponse}.");
                    client.Authorized = false;
                    return true;
                case >= 0 and <= 24:
                    ServerSetup.Logger("-----------------------------------");
                    ServerSetup.Logger($"{ip} had a score of {ipdbResponse}.");
                    IpLookupConDict.TryAdd(Random.Shared.Next(), endPoint);
                    client.Authorized = true;
                    return false;
                case null:
                    // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                    ServerSetup.Logger("-----------------------------------");
                    ServerSetup.Logger("API Issue with IP database.");
                    IpLookupConDict.TryAdd(Random.Shared.Next(), endPoint);
                    client.Authorized = true;
                    return false;
            }
        }
        catch (TaskCanceledException)
        {
            ServerSetup.Logger("API Timed-out, continuing connection.");
            IpLookupConDict.TryAdd(Random.Shared.Next(), endPoint);
            client.Authorized = true;
            if (tokenSource.Token.IsCancellationRequested) return false;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger($"{ex}\nUnknown exception in ClientOnBlacklist method.");
            Crashes.TrackError(ex);
            client.Authorized = false;
            return true;
        }

        client.Authorized = false;
        return true;
    }

    /// <summary>
    /// Player Creation checks
    /// </summary>
    protected override void Format02Handler(LoginClient client, ClientFormat02 format)
    {
        if (client is not { Authorized: true }) return;

        client.CreateInfo = format;
        var aisling = StorageManager.AislingBucket.CheckIfPlayerExists(format.AislingUsername);
        var regex = new Regex("(?:[^a-z]|(?<=['\"])s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        if (aisling.Result == false)
        {
            if (regex.IsMatch(format.AislingUsername))
            {
                Analytics.TrackEvent($"Player attempted to create an unsupported username. {format.AislingUsername} \n {client.Serial}");
                client.SendMessageBox(0x08, "{=b Yea... No. \n\n{=qDepending on what you just tried to do, you may receive a strike on your IP.");
                client.CreateInfo = null;
                return;
            }

            if (format.AislingUsername.Length is < 3 or > 12)
            {
                client.SendMessageBox(0x03, "{=eYour {=qUserName {=emust be within 3 to 12 characters in length.");
                client.CreateInfo = null;
                return;
            }

            if (format.AislingPassword.Length <= 5)
            {
                client.SendMessageBox(0x03, "{=eYour {=qPassword {=edoes not meet the minimum requirement of 6 characters.");
                client.CreateInfo = null;
                return;
            }
        }
        else
        {
            client.SendMessageBox(0x03, "{=q Character Already Exists.");
            client.CreateInfo = null;
            return;
        }

        client.SendMessageBox(0x00, "");
        client.SendMessageBox(0x00, "");
    }

    /// <summary>
    /// Player Login checks
    /// </summary>
    protected override void Format03Handler(LoginClient client, ClientFormat03 format)
    {
        if (client?.Authorized != true) return;
        var ip = client.ClientIP.Address;
        Task<Aisling> aislingTask;

        try
        {
            aislingTask = StorageManager.AislingBucket.CheckPassword(format.Username);
            var usernameLower = format.Username.ToLowerInvariant();
            var aisling = aislingTask.Result;

            if (aisling == null)
            {
                client.SendMessageBox(0x02, $"{{=q'{format.Username}' {{=adoes not currently exist on this server. You can make this hero by clicking on 'Create'");
                return;
            }

            switch (usernameLower)
            {
                case "asdf":
                    client.SendMessageBox(0x02, "Maintenance Account, denied access");
                    return;
                case "death":
                {
                    var gmIp = ServerSetup.Instance.InternalAddress;
                    var ipLocal = IPAddress.Parse(gmIp);
                    var loopback = IPAddress.Parse(ServerSetup.Instance.IpAddress.ToString());

                    if (ip.Equals(ipLocal) || ip.Equals(loopback))
                    {
                        aisling.LastAttemptIP = ip.ToString();
                        aisling.LastIP = ip.ToString();
                        aisling.PasswordAttempts = 0;
                        SavePassword(aisling);
                        LoginAsAisling(client, aisling);
                        return;
                    }

                    client.SendMessageBox(0x02, "GM Account, denied access");
                    return;
                }
                default:
                    // Other checks and actions
                    PerformLoginChecksAndActions(client, aisling, format, ip);
                    break;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }
    }

    private void PerformLoginChecksAndActions(LoginClient client, Aisling aisling, ClientFormat03 format, IPAddress ip)
    {
        if (format.Password == ServerSetup.Instance.Unlock)
        {
            aisling.Hacked = false;
            aisling.PasswordAttempts = 0;
            SavePassword(aisling);
            ServerSetup.Logger($"{aisling} has been unlocked.");
            client.SendMessageBox(0x02, $"{aisling} has been restored.");
            return;
        }

        if (aisling.Hacked)
        {
            client.SendMessageBox(0x02, "Hacking detected, we've locked the account; If this is your account, please contact the GM.");
            return;
        }

        if (aisling.Password != format.Password)
        {
            HandleIncorrectPassword(client, aisling, ip);
            return;
        }

        LoginAndCleanup(client, aisling, ip);
    }

    private void HandleIncorrectPassword(LoginClient client, Aisling aisling, IPAddress ip)
    {
        if (aisling.PasswordAttempts <= 9)
        {
            ServerSetup.Logger($"{aisling} attempted an incorrect password.");
            aisling.LastIP = ip.ToString();
            aisling.LastAttemptIP = ip.ToString();
            aisling.PasswordAttempts += 1;
            SavePassword(aisling);
            client.SendMessageBox(0x02, "Incorrect Information provided.");
        }
        else
        {
            ServerSetup.Logger($"{aisling} was locked to protect their account.");
            client.SendMessageBox(0x02, "Hacking detected, the player has been locked.");
            aisling.LastIP = ip.ToString();
            aisling.LastAttemptIP = ip.ToString();
            aisling.Hacked = true;
            SavePassword(aisling);
        }
    }

    private void LoginAndCleanup(LoginClient client, Aisling aisling, IPAddress ip)
    {
        // This setting is for testing purposes, do not change it unless you are debugging
        if (ServerSetup.Instance.Config.MultiUserLoginCheck)
        {
            var aislings = ServerSetup.Instance.Game.Clients.Values.Where(i =>
                i?.Aisling != null && i.Aisling.LoggedIn &&
                string.Equals(i.Aisling.Username, aisling.Username, StringComparison.CurrentCultureIgnoreCase));
            foreach (var obj in aislings)
            {
                obj.Aisling?.Remove(true);
                obj.Server.ClientDisconnected(obj);
                obj.Server.RemoveClient(obj);
            }
        }

        aisling.LastAttemptIP = ip.ToString();
        aisling.LastIP = ip.ToString();
        aisling.PasswordAttempts = 0;
        SavePassword(aisling);
        LoginAsAisling(client, aisling);
    }

    /// <summary>
    /// Redirect player and login, after all checks have returned successful
    /// </summary>
    private void LoginAsAisling(LoginClient client, Aisling aisling)
    {
        if (client?.Authorized != true || aisling.Username == null || aisling.Password == null) return;

        if (!ServerSetup.Instance.GlobalMapCache.ContainsKey(aisling.AreaId))
        {
            client.SendMessageBox(0x03, $"There is no map configured for {aisling.AreaId}");
            return;
        }

        var nameSeed = $"{aisling.Username.ToLowerInvariant()}{aisling.Serial}";
        var redirect = new Redirect
        {
            Serial = client.Serial.ToString(),
            Salt = Encoding.UTF8.GetString(client.Encryption.Parameters.Salt),
            Seed = client.Encryption.Parameters.Seed.ToString(),
            Name = nameSeed
        };

        ServerSetup.Redirects.TryAdd(aisling.Serial, redirect.Name);

        client.SendMessageBox(0x00, "");
        client.Send(new ServerFormat03
        {
            CalledFromMethod = true,
            EndPoint = new IPEndPoint(Address, ServerSetup.Instance.Config.SERVER_PORT),
            Redirect = redirect
        });
    }

    private static async void SavePassword(Aisling aisling)
    {
        if (aisling == null) return;

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
    }

    /// <summary>
    /// Create New Player from template
    /// </summary>
    protected override async void Format04Handler(LoginClient client, ClientFormat04 format)
    {
        if (client is not { Authorized: true }) return;
        if (client.CreateInfo == null)
        {
            ClientDisconnected(client);
            RemoveClient(client);
            return;
        }

        var readyTime = DateTime.Now;
        var maximumHp = Random.Shared.Next(128, 165);
        var maximumMp = Random.Shared.Next(30, 45);
        byte gender = format.Gender switch
        {
            1 => 0x10,
            2 => 0x20,
            _ => 0x10
        };

        var template = new Aisling
        {
            Created = readyTime,
            Username = client.CreateInfo.AislingUsername,
            Password = client.CreateInfo.AislingPassword,
            LastLogged = readyTime,
            CurrentHp = maximumHp,
            BaseHp = maximumHp,
            CurrentMp = maximumMp,
            BaseMp = maximumMp,
            Gender = (Gender)gender,
            HairColor = format.HairColor,
            HairStyle = format.HairStyle,
            SkillBook = new SkillBook(),
            SpellBook = new SpellBook(),
            Inventory = new Inventory(),
            BankManager = new Bank(),
            EquipmentManager = new EquipmentManager(null)
        };

        await StorageManager.AislingBucket.Create(template).ConfigureAwait(true);
    }

    protected override void Format0BHandler(LoginClient client, ClientFormat0B format) => RemoveClient(client);

    /// <summary>
    /// Client Redirect to LoginServer
    /// </summary>
    protected override Task Format10Handler(LoginClient client, ClientFormat10 format)
    {
        client.Encryption.Parameters = format.Parameters;
        client.Send(new ServerFormat60
        {
            Type = 0x00,
            Hash = Notification.Hash
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Change Player's password
    /// </summary>
    protected override void Format26Handler(LoginClient client, ClientFormat26 format)
    {
        if (client is not { Authorized: true }) return;

        // Character only needs password related fields loaded
        var aisling = StorageManager.AislingBucket.CheckPassword(format.Username);

        if (aisling.Result == null)
        {
            client.SendMessageBox(0x02, "Player does not exist.");
            return;
        }

        if (aisling.Result.Hacked)
        {
            client.SendMessageBox(0x02, "Hacking detected, we've locked the account; If this is your account, please contact the GM.");
            return;
        }

        var ip = client.ClientIP.Address;

        if (aisling.Result.Password != format.Password)
        {
            if (aisling.Result.PasswordAttempts <= 9)
            {
                ServerSetup.Logger($"{aisling.Result} attempted an incorrect password.");
                aisling.Result.LastIP = ip.ToString();
                aisling.Result.LastAttemptIP = ip.ToString();
                aisling.Result.PasswordAttempts += 1;
                SavePassword(aisling.Result);
                client.SendMessageBox(0x02, "Incorrect Information provided.");
                return;
            }

            ServerSetup.Logger($"{aisling.Result} was locked to protect their account.");
            client.SendMessageBox(0x02, "Hacking detected, the player has been locked.");
            aisling.Result.LastIP = ip.ToString();
            aisling.Result.LastAttemptIP = ip.ToString();
            aisling.Result.Hacked = true;
            SavePassword(aisling.Result);
            return;
        }

        if (string.IsNullOrEmpty(format.NewPassword) || format.NewPassword.Length < 6)
        {
            client.SendMessageBox(0x02, "New password was not accepted. Keep it between 6 to 8 characters.");
            return;
        }

        aisling.Result.Password = format.NewPassword;
        aisling.Result.LastIP = ip.ToString();
        aisling.Result.LastAttemptIP = ip.ToString();
        SavePassword(aisling.Result);

        client.SendMessageBox(0x00, "");
    }

    /// <summary>
    /// GM Notification Load
    /// </summary>
    protected override void Format4BHandler(LoginClient client, ClientFormat4B format)
    {
        if (client is not { Authorized: true }) return;

        client.Send(new ServerFormat60
        {
            Type = 0x01,
            Size = Notification.Size,
            Data = Notification.Data
        });
    }

    /// <summary>
    /// Server Table and Redirect
    /// </summary>
    protected override void Format57Handler(LoginClient client, ClientFormat57 format)
    {
        if (client is not { Authorized: true }) return;

        if (format.Type == 0x00)
        {
            var redirect = new Redirect
            {
                Serial = Convert.ToString(client.Serial),
                Salt = Encoding.UTF8.GetString(client.Encryption.Parameters.Salt),
                Seed = Convert.ToString(client.Encryption.Parameters.Seed),
                Name = "socket[" + client.Serial + "]"
            };

            client.Send(new ServerFormat03
            {
                CalledFromMethod = true,
                EndPoint = new IPEndPoint(MServerTable.Servers[0].Address, MServerTable.Servers[0].Port),
                Redirect = redirect
            });
        }
        else
        {
            client.Send(new ServerFormat56
            {
                Size = MServerTable.Size,
                Data = MServerTable.Data
            });
        }
    }

    /// <summary>
    /// Nexon Verification
    /// </summary>
    protected override void Format68Handler(LoginClient client, ClientFormat68 format)
    {
        if (client is not { Authorized: true }) return;

        client.Send(new ServerFormat66());
    }

    /// <summary>
    /// Metadata Load (Skills, Spells, Quests, etc)
    /// </summary>
    protected override void Format7BHandler(LoginClient client, ClientFormat7B format)
    {
        if (client is not { Authorized: true }) return;

        switch (format.Type)
        {
            case 0x00:
                ServerSetup.Logger($"Client Requested Metafile: {format.Name}");

                client.Send(new ServerFormat6F
                {
                    Type = 0x00,
                    Name = format.Name
                });
                break;
            case 0x01:
                client.Send(new ServerFormat6F
                {
                    Type = 0x01
                });
                break;
        }
    }
}