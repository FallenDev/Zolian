using System.Net.Sockets;

using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

namespace Darkages.Network;

public sealed class NetworkSocket
{
    internal readonly Socket Socket;
    private const int HeaderLength = 3;

    private byte[] _header = new byte[HeaderLength];
    private byte[] _packet = new byte[0xFFFF];

    private int _headerOffset;
    private int _packetLength;
    private int _packetOffset;

    public NetworkSocket(Socket socket)
    {
        ConfigureTcpSocket(socket);
        Socket = socket;
    }

    public bool HeaderComplete => _headerOffset == HeaderLength;

    public bool PacketComplete => _packetOffset == _packetLength;

    public void Flush()
    {
        _header = new byte[HeaderLength];
        _packet = new byte[0xFFFF];
        _headerOffset = 0;
        _packetLength = 0;
        _packetOffset = 0;
    }

    public void BeginReceiveHeader(AsyncCallback callback, out SocketError error, object state)
    {
        if (state == null)
        {
            error = SocketError.ConnectionRefused;
            return;
        }

        try
        {
            if (Socket is { Connected: true })
            {
                Socket.BeginReceive(_header, _headerOffset, HeaderLength - _headerOffset, SocketFlags.None, out error, callback, state);
            }
            else
            {
                error = SocketError.ConnectionRefused;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
            error = SocketError.SocketError;
        }
    }

    public void BeginReceivePacket(AsyncCallback callback, out SocketError error, object state)
    {
        if (state == null)
        {
            error = SocketError.ConnectionRefused;
            return;
        }

        try
        {
            Socket.BeginReceive(_packet, _packetOffset, _packetLength - _packetOffset, SocketFlags.None, out error,
                callback, state);
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
            error = SocketError.SocketError;
        }
    }

    public int? EndReceiveHeader(IAsyncResult result, out SocketError error)
    {
        if (!Socket.Connected)
        {
            error = SocketError.Shutdown;
            return null;
        }

        var bytes = Socket.EndReceive(result, out error);

        if (bytes == 0)
            return 0;

        _headerOffset += bytes;

        if (!HeaderComplete)
            return bytes;

        _packetLength = (_header[1] << 8) | _header[2];
        _packetOffset = 0;

        return bytes;
    }

    public int EndReceivePacket(IAsyncResult result, out SocketError error)
    {
        var bytes = Socket.EndReceive(result, out error);

        if (bytes == 0)
            return 0;

        _packetOffset += bytes;

        if (PacketComplete) _headerOffset = 0;

        return bytes;
    }

    public NetworkPacket ToPacket()
    {
        return PacketComplete ? new NetworkPacket(_packet, _packetLength) : null;
    }

    private static void ConfigureTcpSocket(Socket tcpSocket)
    {
        var linger = new LingerOption(true, 30);

        tcpSocket.NoDelay = true;
        tcpSocket.LingerState = linger;
        tcpSocket.ReceiveBufferSize = 0xFFFF;
        tcpSocket.SendBufferSize = 0xFFFF;
    }
}