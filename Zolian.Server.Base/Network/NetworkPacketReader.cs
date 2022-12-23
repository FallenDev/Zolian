using System.Text;

using Darkages.Interfaces;
using Darkages.Types;

namespace Darkages.Network
{
    public class NetworkPacketReader
    {
        public NetworkPacket Packet;
        public int Position;
        private readonly Encoding _encoding = Encoding.GetEncoding(949);

        public byte ReadByte()
        {
            byte b = 0;

            if (Position == -1)
            {
                b = Packet.Ordinal;
            }
            else
            {
                if (Position < Packet.Data.Length)
                    b = Packet.Data[Position];
            }

            Position++;

            return b;
        }

        public byte[] ReadBytes(int count)
        {
            var array = new byte[count];

            for (var i = 0; i < count; i++)
                array[i] = ReadByte();

            return array;
        }

        public T ReadObject<T>()
            where T : IFormattableNetwork, new()
        {
            var result = new T();

            result.Serialize(this);

            return result;
        }

        public Position ReadPosition()
        {
            return new Position(ReadUInt16(), ReadUInt16());
        }

        public string ReadStringA()
        {
            var length = ReadByte();
            var result = _encoding.GetString(Packet.Data, Position, length);

            Position += length;

            return result;
        }

        public string ReadStringB()
        {
            var length = ReadUInt16();
            var result = _encoding.GetString(Packet.Data, Position, length);

            Position += length;

            return result;
        }

        public bool GetCanRead() => Position + 1 < Packet.Data.Length;
        public bool ReadBool() => ReadByte() != 0;
        public short ReadInt16() => (short)ReadUInt16();
        public ushort ReadUInt16() => (ushort)((ReadByte() << 0x08) | ReadByte());
        public int ReadInt32() => (int)ReadUInt32();
        public uint ReadUInt32() => (uint)((ReadUInt16() << 0x10) + ReadUInt16());
    }
}