using System.Net;
using System.Text;
using Darkages.Interfaces;
using Darkages.Types;

namespace Darkages.Network;

public class NetworkPacketWriter
{
    private int _position;
    private readonly Encoding _encoding = Encoding.GetEncoding(949);
    private readonly byte[] _buffer;

    public NetworkPacketWriter()
    {
        _buffer = new byte[0xFFFF];
    }

    public NetworkPacket ToPacket()
    {
        return _position > 0 ? new NetworkPacket(_buffer, _position) : null;
    }

    public void Write(bool value)
    {
        Write((byte)(value ? 1 : 0));
    }

    public void Write(byte value)
    {
        _buffer[_position++] = value;
    }

    public void Write(byte[] value)
    {
        Array.Copy(value, 0, _buffer, _position, value.Length);
        _position += value.Length;
    }

    public void Write(sbyte value)
    {
        _buffer[_position++] = (byte)value;
    }

    public void Write(short value)
    {
        Write((ushort)value);
    }

    public void Write(ushort value)
    {
        Write((byte)(value >> 8));
        Write((byte)value);
    }

    public void Write(int value)
    {
        Write((uint)value);
    }

    public void Write(uint value)
    {
        Write((ushort)(value >> 16));
        Write((ushort)value);
    }

    public void WritePos16(Position pos)
    {
        Write((ushort)pos.X);
        Write((ushort)pos.Y);
    }

    public void WritePos8(Position pos)
    {
        Write((byte)pos.X);
        Write((byte)pos.Y);
    }

    public void Write<T>(T value)
        where T : IFormattableNetwork
    {
        value.Serialize(this);
    }

    public void Write(IPEndPoint endPoint)
    {
        var ipBytes = endPoint.Address.GetAddressBytes();

        Write(ipBytes[3]);
        Write(ipBytes[2]);
        Write(ipBytes[1]);
        Write(ipBytes[0]);
        Write((ushort)endPoint.Port);
    }

    public void WriteString(string value)
    {
        _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
        _position += _encoding.GetByteCount(value);
    }

    public void WriteAscii(string value)
    {
        Encoding.ASCII.GetBytes(value, 0, value.Length, _buffer, _position);
        _position += Encoding.ASCII.GetByteCount(value);
    }

    public void WriteStringA(string value)
    {
        if (value == null) return;
        var count = _encoding.GetByteCount(value);

        Write((byte)count);

        _encoding.GetBytes(value, 0, value.Length, _buffer, _position);

        _position += count;
    }

    public void WriteStringB(string value)
    {
        var count = _encoding.GetByteCount(value);

        Write((ushort)count);

        _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
        _position += count;
    }
}