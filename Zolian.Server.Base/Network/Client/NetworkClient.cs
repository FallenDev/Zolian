using System.Net;
using System.Net.Sockets;
using Darkages.Network.Formats;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Security;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Network.Client;

public abstract class NetworkClient : IDisposable
{
    protected NetworkClient()
    {
        ReceiveLock = new SemaphoreSlim(1, 1);
        SendLock = new SemaphoreSlim(1, 1);
        Reader = new NetworkPacketReader();
        Writer = new NetworkPacketWriter();
        Encryption = new SecurityProvider();
    }

    public int Serial { get; set; }
    private SemaphoreSlim ReceiveLock { get; }
    private SemaphoreSlim SendLock { get; }
    public event EventHandler OnDisconnected;
    public SecurityProvider Encryption { get; }
    private byte Ordinal { get; set; }
    public bool MapOpen { get; set; }
    private NetworkPacketReader Reader { get; set; }
    private NetworkPacketWriter Writer { get; set; }
    public Socket Socket => State.Socket;
    internal NetworkSocket State { get; init; }
    public DateTime LastMessageFromClient { get; set; }
    public DateTime LastMessageFromClientNot0X45 { get; set; }
    public DateTime LastPacket0X0FFromClient { get; set; } // Spell Limiter
    public DateTime LastPacket0X13FromClient { get; set; } // User Spacebar Limiter
    public DateTime LastPacket0X1BFromClient { get; set; } // User Options Limiter
    public DateTime LastPacket0X1CFromClient { get; set; } // Item Limiter
    public DateTime LastPacket0X2DFromClient { get; set; } // Request Profile Limiter
    public DateTime LastPacket0X38FromClient { get; set; } // User F5 Limiter
    public byte LastPacketFromClient { get; set; }

    public void SetLastPacketTime(int command, DateTime time)
    {
        switch (command)
        {
            case 0x0F:
                LastPacket0X0FFromClient = time;
                break;
            case 0x13:
                LastPacket0X13FromClient = time;
                break;
            case 0x1B:
                LastPacket0X1BFromClient = time;
                break;
            case 0x1C:
                LastPacket0X1CFromClient = time;
                break;
            case 0x2D:
                LastPacket0X2DFromClient = time;
                break;
            case 0x38:
                LastPacket0X38FromClient = time;
                break;
        }
    }

    /// <summary>
    /// Send method for single packets
    /// </summary>
    public async void Send(NetworkFormat format)
    {
        if (!Socket.Connected) return;
        if (MapOpen && format.Command is not 59) return;

        await SendLock.WaitAsync().ConfigureAwait(false);

        try
        {
            Writer = new NetworkPacketWriter();
            Writer.Write(format.Command);

            if (format.Encrypted)
                Writer.Write(Ordinal++);

            format.Serialize(Writer);

            var packet = Writer.ToPacket();

            if (packet == null) return;
            var packetOpCodeToString = $"{packet.Command:X2}";

            // ToDo: Server to Client Logger
            if (ServerSetup.Instance.Config.LogClientPackets)
                ServerSetup.Logger($"Server: 0x{packetOpCodeToString} = {packet}");

            if (format.Encrypted)
                Encryption.Transform(packet);

            var buffer = packet.ToArray();
            if (buffer.Length <= 0x0) return;

            if (!Socket.Connected) return;
            var ar = Socket.BeginSend(buffer, 0x0, buffer.Length, SocketFlags.None, SendCompleted, Socket);
            ar?.AsyncWaitHandle.WaitOne();
        }
        catch
        {
            Dispose();
        }
        finally
        {
            if (Socket.Connected)
                SendLock.Release();
        }
    }

    /// <summary>
    /// Send method for cluster packets
    /// </summary>
    public async void Send(params NetworkFormat[] formats)
    {
        if (!Socket.Connected) return;
        if (formats.Any(format => MapOpen && format.Command is not 59)) return;

        await SendLock.WaitAsync().ConfigureAwait(false);

        try
        {
            foreach (var format in formats)
            {
                Writer = new NetworkPacketWriter();
                Writer.Write(format.Command);

                if (format.Encrypted)
                    Writer.Write(Ordinal++);

                format.Serialize(Writer);

                var packet = Writer.ToPacket();

                if (packet == null) return;
                var packetOpCodeToString = $"{packet.Command:X2}";

                // ToDo: Server to Client Logger
                if (ServerSetup.Instance.Config.LogClientPackets)
                    ServerSetup.Logger($"Server: 0x{packetOpCodeToString} = {packet}");

                if (format.Encrypted)
                    Encryption.Transform(packet);

                var buffer = packet.ToArray();
                if (buffer.Length <= 0x0) return;

                if (!Socket.Connected) return;
                var ar = Socket.BeginSend(buffer, 0x0, buffer.Length, SocketFlags.None, SendCompleted, Socket);
                ar?.AsyncWaitHandle.WaitOne();
            }
        }
        catch
        {
            Disconnect();
        }
        finally
        {
            if (Socket.Connected)
                SendLock.Release();
        }
    }

    private static void SendCompleted(IAsyncResult ar)
    {
        var signal = ar.AsyncState as ManualResetEvent;

        if (ar.IsCompleted && ar.CompletedSynchronously)
        {
            signal?.Set();
        }
    }

    /// <summary>
    /// Client-To-Client Trading
    /// </summary>
    public void Send(NetworkPacketWriter data)
    {
        if (!Socket.Connected) return;
        if (Socket.RemoteEndPoint is not IPEndPoint) return;

        lock (ServerSetup.SyncLock)
        {
            var packet = data.ToPacket();
            if (packet == null) return;

            Encryption.Transform(packet);

            var buffer = packet.ToArray();

            try
            {
                if (!Socket.Connected) return;
                Socket.SendAsync(buffer, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode is SocketError.WouldBlock or SocketError.IOPending or SocketError.NoBufferSpaceAvailable or SocketError.ConnectionAborted)
                {
                    // Ignore
                }
                else
                {
                    Crashes.TrackError(ex);
                }
            }
        }
    }

    public void SendMessageBox(byte code, string text) => Send(new ServerFormat02(code, text));

    /// <summary>
    /// Read method for packets
    /// </summary>
    public async void Read(NetworkPacket packet, NetworkFormat format)
    {
        if (packet == null) return;

        await ReceiveLock.WaitAsync().ConfigureAwait(false);

        try
        {
            Reader = new NetworkPacketReader();

            switch (format.Encrypted)
            {
                case true:
                {
                    Encryption.Transform(packet);

                    // ToDo: Client to Server Logger
                    if (ServerSetup.Instance.Config.LogServerPackets)
                        ServerSetup.Logger($"Client: 0x{packet.Command:X2} = {packet}");

                    if (format.Command is 0x39 or 0x3A)
                    {
                        TransFormDialog(packet);
                        Reader.Position = 0x6;
                    }
                    else
                    {
                        Reader.Position = 0x0;
                    }

                    break;
                }
                default:
                    Reader.Position = -0x1;
                    break;
            }

            Reader.Packet = packet;
            format.Serialize(Reader);
        }
        catch
        {
            Disconnect();
        }
        finally
        {
            if (Socket.Connected)
                ReceiveLock.Release();
        }
    }
        
    private static void TransFormDialog(NetworkPacket value)
    {
        if (value.Data.Length > 0x2) value.Data[0x2] ^= (byte)(P(value) + 0x73);
        if (value.Data.Length > 0x3) value.Data[0x3] ^= (byte)(P(value) + 0x73);
        if (value.Data.Length > 0x4) value.Data[0x4] ^= (byte)(P(value) + 0x28);
        if (value.Data.Length > 0x5) value.Data[0x5] ^= (byte)(P(value) + 0x29);

        for (var i = value.Data.Length - 0x6 - 0x1; i >= 0x0; i--)
        {
            var index = i + 0x6;

            if (index >= 0x0 && value.Data.Length > index)
                value.Data[index] ^= (byte)(((byte)(P(value) + 0x28) + i + 0x2) % 0x100);
        }
    }

    private static byte P(NetworkPacket value) => (byte)(value.Data[0x1] ^ (byte)(value.Data[0x0] - 0x2D));

    public void FlushAfterCleanup() => State?.Flush();

    private void Disconnect()
    {
        if (!Socket.Connected) return;

        try
        {
            Socket.Disconnect(false);
        }
        catch
        {
            // ignored
        }

        try
        {
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // ignored
        }

        Dispose();
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        GC.SuppressFinalize(this);
        Socket.Dispose();
        ReceiveLock.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}