using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;

using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Formats;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

namespace Darkages.Network.Server
{
    public abstract partial class NetworkServer<TClient> : NetworkClient where TClient : NetworkClient, new()
    {
        private readonly MethodInfo[] _handlers;
        private Socket _socket;
        private bool _listening;

        protected NetworkServer()
        {
            var type = typeof(NetworkServer<TClient>);

            Address = ServerSetup.Instance.IpAddress;
            Clients = new ConcurrentDictionary<int, TClient>();
            IpLookupConDict = new ConcurrentDictionary<int, IPEndPoint>();

            _handlers = new MethodInfo[256];

            for (var i = 0; i < _handlers.Length; i++)
                _handlers[i] = type.GetMethod($"Format{i:X2}Handler", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        protected IPAddress Address { get; }
        public ConcurrentDictionary<int, TClient> Clients { get; }
        protected ConcurrentDictionary<int, IPEndPoint> IpLookupConDict { get; }

        public void Abort()
        {
            _listening = false;

            if (_socket != null)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                finally
                {
                    _socket.Close();
                }

                _socket = null;
            }

            lock (Clients)
            {
                foreach (var client in Clients.Values.Where(client => client != null))
                {
                    ClientDisconnected(client);
                    RemoveClient(client);
                }
            }
        }

        private bool AddClient(TClient client)
        {
            if (!Clients.ContainsKey(client.Serial))
                Clients.TryAdd(client.Serial, client);

            return true;
        }

        protected bool CheckIfIpHasAlreadyBeenChecked(IPEndPoint endPoint)
        {
            foreach (var (_, value) in IpLookupConDict)
            {
                if (endPoint.Address.Equals(value.Address)) return true;
            }

            return false;
        }

        protected virtual void ClientConnected(TClient client) { }
        
        private void ClientDataReceived(TClient client, NetworkPacket packet)
        {
            if (client == null) return;
            var format = NetworkFormatManager.GetClientFormat(packet.Command);
            var ip = client.Socket.RemoteEndPoint as IPEndPoint;

            if (format == null) return;
            if (!Clients.ContainsKey(client.Serial)) return;
            if (client.MapOpen && format.Command is not (63 or 69)) return;

            if (client.Serial == 0)
            {
                ServerSetup.Logger($"{ip!.Address} client never established.", LogLevel.Critical);
                ClientDisconnected(client);
                RemoveClient(client);
                return;
            }

            try
            {
                var timeCheck250 = client.LastMessageFromClient.AddMilliseconds(250);
                var timeCheck500 = client.LastMessageFromClient.AddMilliseconds(500);
                var timeCheck600 = client.LastMessageFromClient.AddMilliseconds(600);
                var profileCheck = client.LastMessageFromClient.AddSeconds(1);

                switch (client.LastPacketFromClient)
                {
                    case 0x0F when format.Command == 0x0F && timeCheck500 > client.LastPacket0X0FFromClient:
                        client.LastPacket0X0FFromClient = DateTime.Now;
                        return;
                    case 0x0F when format.Command == 0x0F && timeCheck500 < client.LastPacket0X0FFromClient:
                        client.LastPacket0X0FFromClient = DateTime.Now;
                        break;
                    case 0x13 when format.Command == 0x13 && timeCheck500 > client.LastPacket0X13FromClient:
                        client.LastPacket0X13FromClient = DateTime.Now;
                        return;
                    case 0x13 when format.Command == 0x13 && timeCheck500 < client.LastPacket0X13FromClient:
                        client.LastPacket0X13FromClient = DateTime.Now;
                        break;
                    case 0x1B when format.Command == 0x1B && timeCheck250 > client.LastPacket0X1BFromClient:
                        client.LastPacket0X1BFromClient = DateTime.Now;
                        return;
                    case 0x1B when format.Command == 0x1B && timeCheck250 < client.LastPacket0X1BFromClient:
                        client.LastPacket0X1BFromClient = DateTime.Now;
                        break;
                    case 0x1C when format.Command == 0x1C && timeCheck250 > client.LastPacket0X1CFromClient:
                        client.LastPacket0X1CFromClient = DateTime.Now;
                        return;
                    case 0x1C when format.Command == 0x1C && timeCheck250 < client.LastPacket0X1CFromClient:
                        client.LastPacket0X1CFromClient = DateTime.Now;
                        break;
                    case 0x2D when format.Command == 0x2D && profileCheck > client.LastPacket0X2DFromClient:
                        client.LastPacket0X2DFromClient = DateTime.Now;
                        return;
                    case 0x2D when format.Command == 0x2D && profileCheck < client.LastPacket0X2DFromClient:
                        client.LastPacket0X2DFromClient = DateTime.Now;
                        break;
                    case 0x38 when format.Command == 0x38 && timeCheck600 > client.LastPacket0X38FromClient:
                        client.LastPacket0X38FromClient = DateTime.Now;
                        return;
                    case 0x38 when format.Command == 0x38 && timeCheck600 < client.LastPacket0X38FromClient:
                        client.LastPacket0X38FromClient = DateTime.Now;
                        break;
                }

                if (format.Command != 0x45)
                    client.LastMessageFromClientNot0X45 = DateTime.Now;

                client.LastPacketFromClient = format.Command;
                client.LastMessageFromClient = DateTime.Now;
                client.Read(packet, format);

                if (_handlers[format.Command] == null) return;

                _handlers[format.Command].Invoke(this, new object[]
                {
                    client, format
                });
            }
            catch (Exception ex)
            {
                var formatCommand = format.ToString();
                var pattern = Regex.Split(formatCommand!, @"\D+");
                ServerSetup.Logger($"Client:{ip!.Address}, Invoking: {pattern[1]}");
                ServerSetup.Logger("--------------------------------");
                ServerSetup.Logger(ex.TargetSite?.CallingConvention.ToString(), LogLevel.Critical);
                Crashes.TrackError(ex);
                ClientDisconnected(client);
                RemoveClient(client);
            }
        }

        public virtual void ClientDisconnected(TClient client)
        {
            if (client == null) return;
            if (!client.Socket.Connected) return;

            try
            {
                client.State.Socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                client.State.Socket.Close();
                client.Dispose();
            }
        }

        public void RemoveClient(TClient client)
        {
            if (client == null) return;
            if (Clients != null && Clients.ContainsKey(client.Serial))
                Clients.TryRemove(client.Serial, out _);
        }

        // ToDo: Culprit Port Block
        /*
        $theCulpritPort="2615"
        Get-NetTCPConnection -LocalPort $theCulpritPort -ErrorAction Ignore `
        | Select-Object -Property  @{'Name' = 'ProcessName';'Expression'={(Get-Process -Id $_.OwningProcess).Name}} `
        | Get-Unique `
        | Stop-Process -Name {$_.ProcessName} -Force
        */
        public virtual void Start(int port)
        {
            if (_listening) return;

            _listening = true;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (Exception e)
            {
                ServerSetup.Logger("Winsock error: " + e);
            }
            _socket.Listen(ServerSetup.Instance.Config?.ConnectionCapacity ?? 2048);
            _socket.BeginAccept(EndConnectClient, _socket);
        }

        private void EndConnectClient(IAsyncResult result)
        {
            if (_socket == null || !_listening) return;

            var client = new TClient
            {
                State = new NetworkSocket(_socket.EndAccept(result))
            };

            try
            {
                if (client.Socket.Connected)
                {
                    client.Serial = Generator.GenerateNumber();

                    if (AddClient(client))
                    {
                        ClientConnected(client);

                        client.State.BeginReceiveHeader(EndReceiveHeader, out _, client);
                    }
                    else
                    {
                        ServerSetup.Logger("Client could not be added. - NetworkServer.cs");
                        ClientDisconnected(client);
                        RemoveClient(client);

                        return;
                    }
                }

                if (_listening)
                    _socket.BeginAccept(EndConnectClient, client.State);
            }
            catch (Exception ex)
            {
                ServerSetup.Logger(ex.Message, LogLevel.Warning);
                ServerSetup.Logger(ex.StackTrace, LogLevel.Warning);
                ClientDisconnected(client);
                RemoveClient(client);
            }
        }

        protected static bool BogonCheck(TClient client, string ip)
        {
            if (client.Socket.RemoteEndPoint == null || ip == null) return true;

            var bogonList = new List<string>();
            string[] first = { "0.0.0.0", "0.0.0.1", "0.0.0.2", "0.0.0.3", "0.0.0.4", "0.0.0.5", "0.0.0.6", "0.0.0.7", "0.0.0.8" };
            string[] second = { "10.0.0.0", "10.0.0.1", "10.0.0.2", "10.0.0.3", "10.0.0.4", "10.0.0.5", "10.0.0.6", "10.0.0.7", "10.0.0.8" };
            string[] third =
            {
                "100.64.0.0", "100.64.0.1", "100.64.0.2", "100.64.0.3", "100.64.0.4", "100.64.0.5", "100.64.0.6", "100.64.0.7",
                "100.64.0.8", "100.64.0.9", "100.64.0.10"
            };
            string[] fourth = { "127.0.0.0", "127.0.0.2", "127.0.0.3", "127.0.0.4", "127.0.0.5", "127.0.0.6", "127.0.0.7", "127.0.0.8" };
            string[] fifth =
            {
                "169.254.0.0", "169.254.0.1", "169.254.0.2", "169.254.0.3", "169.254.0.4", "169.254.0.5", "169.254.0.6", "169.254.0.7", "169.254.0.8",
                "169.254.0.9","169.254.0.10","169.254.0.11","169.254.0.12","169.254.0.13","169.254.0.14","169.254.0.15","169.254.0.16"
            };
            string[] sixth =
            {
                "172.16.0.0", "172.16.0.1", "172.16.0.2", "172.16.0.3", "172.16.0.4", "172.16.0.5", "172.16.0.6",
                "172.16.0.7","172.16.0.8","172.16.0.9","172.16.0.10","172.16.0.11","172.16.0.12"
            };
            string[] seventh =
            {
                "192.0.0.0", "192.0.0.1", "192.0.0.2", "192.0.0.3", "192.0.0.4", "192.0.0.5", "192.0.0.6", "192.0.0.7", "192.0.0.8",
                "192.0.0.9","192.0.0.10","192.0.0.11","192.0.0.12","192.0.0.13","192.0.0.14","192.0.0.15","192.0.0.16","192.0.0.17",
                "192.0.0.18", "192.0.0.19","192.0.0.20","192.0.0.21","192.0.0.22","192.0.0.23", "192.0.0.24"
            };
            string[] eight =
            {
                "192.0.2.0","192.0.2.1","192.0.2.2","192.0.2.3","192.0.2.4","192.0.2.5","192.0.2.6","192.0.2.7","192.0.2.8",
                "192.0.2.9","192.0.2.10","192.0.2.11","192.0.2.12","192.0.2.13","192.0.2.14","192.0.2.15","192.0.2.16","192.0.2.17",
                "192.0.2.18","192.0.2.19","192.0.2.20","192.0.2.21","192.0.2.22","192.0.2.23","192.0.2.24"
            };
            string[] ninth =
            {
                "192.168.0.0","192.168.0.1","192.168.0.2","192.168.0.3","192.168.0.4","192.168.0.5","192.168.0.6","192.168.0.7","192.168.0.8",
                "192.168.0.9","192.168.0.10","192.168.0.11","192.168.0.12","192.168.0.13","192.168.0.14","192.168.0.15","192.168.0.16"
            };
            string[] tenth =
            {
                "198.18.0.0","198.18.0.1","198.18.0.2","198.18.0.3","198.18.0.4","198.18.0.5","198.18.0.6","198.18.0.7","198.18.0.8",
                "198.18.0.9","198.18.0.10","198.18.0.11","198.18.0.12","198.18.0.13","198.18.0.14","198.18.0.15"
            };
            string[] eleventh =
            {
                "198.51.100.0", "198.51.100.1","198.51.100.2","198.51.100.3","198.51.100.4","198.51.100.5","198.51.100.6","198.51.100.7","198.51.100.8",
                "198.51.100.9","198.51.100.10","198.51.100.11","198.51.100.12","198.51.100.13","198.51.100.14","198.51.100.15","198.51.100.16","198.51.100.17",
                "198.51.100.18","198.51.100.19","198.51.100.20","198.51.100.21","198.51.100.22","198.51.100.23","198.51.100.24"
            };
            string[] twelfth =
            {
                "203.0.11.0","203.0.11.1","203.0.11.2","203.0.11.3","203.0.11.4","203.0.11.5","203.0.11.6","203.0.11.7","203.0.11.8","203.0.11.9",
                "203.0.11.10","203.0.11.11","203.0.11.12","203.0.11.13","203.0.11.14","203.0.11.15","203.0.11.16","203.0.11.17","203.0.11.18","203.0.11.19",
                "203.0.11.20","203.0.11.21","203.0.11.22","203.0.11.23","203.0.11.24",
            };
            string[] thirteenth = { "224.0.0.0", "224.0.0.1", "224.0.0.2", "224.0.0.3" };
            string[] bannedPlayers = { "" };
            bogonList.AddRange(first);
            bogonList.AddRange(second);
            bogonList.AddRange(third);
            bogonList.AddRange(fourth);
            bogonList.AddRange(fifth);
            bogonList.AddRange(sixth);
            bogonList.AddRange(seventh);
            bogonList.AddRange(eight);
            bogonList.AddRange(ninth);
            bogonList.AddRange(tenth);
            bogonList.AddRange(eleventh);
            bogonList.AddRange(twelfth);
            bogonList.AddRange(thirteenth);
            bogonList.AddRange(bannedPlayers);

            return bogonList.Contains(ip);
        }

        private void EndReceiveHeader(IAsyncResult result)
        {
            if (result.AsyncState is not TClient client) return;

            try
            {
                var bytes = client.State.EndReceiveHeader(result, out var error);
                
                if (bytes == 0 || error != SocketError.Success)
                {
                    ClientDisconnected(client);
                    RemoveClient(client);
                    return;
                }

                const char delimiter = ':';
                var ipToString = client.Socket.RemoteEndPoint?.ToString();
                var ipSplit = ipToString?.Split(delimiter);
                var ip = ipSplit?[0];

                if (client.State.Socket.Connected == false) return;

                if (ip == null)
                {
                    try
                    {
                        client.Socket.Shutdown(SocketShutdown.Both);
                    }
                    finally
                    {
                        client.Socket.Close();
                    }

                    return;
                }

                if (client.State.HeaderComplete)
                {
                    client.State.BeginReceivePacket(EndReceivePacket, out error, client);
                }
                else
                {
                    client.State.BeginReceiveHeader(EndReceiveHeader, out error, client);
                }
            }
            catch (Exception ex)
            {
                ServerSetup.Logger(ex.Message, LogLevel.Warning);
                ServerSetup.Logger(ex.StackTrace, LogLevel.Warning);
                ClientDisconnected(client);
                RemoveClient(client);
            }
        }

        private void EndReceivePacket(IAsyncResult result)
        {
            if (result.AsyncState is not TClient client) return;

            try
            {
                var bytes = client.State.EndReceivePacket(result, out var error);

                if (bytes == 0 || error != SocketError.Success)
                {
                    if (error == SocketError.Success) return;

                    // Fail safe - triggers when socket is not successful
                    Console.Write($"{error}\n");
                    Analytics.TrackEvent($"SocketError:{error} - NetworkServer.cs, EndReceivePacket");
                    ClientDisconnected(client);
                    RemoveClient(client);
                    return;
                }

                if (client.State.PacketComplete)
                {
                    ClientDataReceived(client, client.State.ToPacket());

                    if (client.State == null) return;

                    client.State.BeginReceiveHeader(EndReceiveHeader, out error, client);
                }
                else
                {
                    client.State.BeginReceivePacket(EndReceivePacket, out error, client);
                }

            }
            catch (Exception ex)
            {
                ServerSetup.Logger(ex.Message, LogLevel.Warning);
                ServerSetup.Logger(ex.StackTrace, LogLevel.Warning);
                ClientDisconnected(client);
                RemoveClient(client);
            }
        }
    }
}