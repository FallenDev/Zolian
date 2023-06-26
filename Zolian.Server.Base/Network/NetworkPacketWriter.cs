using System.Net;
using System.Text;
using Darkages.Interfaces;

namespace Darkages.Network;

public class NetworkPacketWriter
{
    private readonly Encoding _encoding = Encoding.GetEncoding(0x3B5);
    private readonly MemoryStream _buffer;
    
    public long Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public NetworkPacketWriter() => _buffer = new MemoryStream(ushort.MaxValue);

    public NetworkPacket ToPacket() => _buffer.Position > 0 ? new NetworkPacket(_buffer.ToArray(), (int)_buffer.Position) : null;

    public void Write(bool value) => Write((byte)(value ? 1 : 0));

    public void Write(byte value) => _buffer.WriteByte(value);

    public void Write(byte[] value)
    {
        _buffer.Write(value, 0, value.Length);
    }

    public void Write(sbyte value) => _buffer.WriteByte((byte)value);

    public void Write(short value) => Write((ushort)value);

    public void Write(ushort value)
    {
        Write((byte)(value >> 8));
        Write((byte)value);
    }

    public void Write(int value) => Write((uint)value);

    public void Write(uint value)
    {
        Write((ushort)(value >> 16));
        Write((ushort)value);
    }

    public void Write<T>(T value) where T : IFormattableNetwork => value.Serialize(this);

    public void Write(IPEndPoint endPoint)
    {
        var ipBytes = endPoint.Address.GetAddressBytes();

        Write(ipBytes[3]);
        Write(ipBytes[2]);
        Write(ipBytes[1]);
        Write(ipBytes[0]);
        Write((ushort)endPoint.Port);
    }

    public void WriteAscii(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        _buffer.Write(bytes, 0, bytes.Length);
    }

    public void WriteString(string value)
    {
        var bytes = _encoding.GetBytes(value);
        _buffer.Write(bytes, 0, bytes.Length);
    }

    public void WriteStringA(string value)
    {
        if (value == null) 
            return;
        var bytes = _encoding.GetBytes(value);
        Write((byte)bytes.Length);
        _buffer.Write(bytes, 0, bytes.Length);
    }

    public void WriteStringB(string value)
    {
        var bytes = _encoding.GetBytes(value);
        Write((ushort)bytes.Length);
        _buffer.Write(bytes, 0, bytes.Length);
    }
}
