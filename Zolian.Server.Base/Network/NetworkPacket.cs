﻿using System.Globalization;

namespace Darkages.Network;

public class NetworkPacket
{
    public NetworkPacket(byte[] array, long count, bool raw = false)
    {
        if (!raw)
        {
            OpCode = array[0];

            if (OpCode == byte.MaxValue)
                return;

            Sequence = array[1];
            Data = count - 2 > 0 ? new byte[count - 0x2] : new byte[count];

            if (Data.Length > 0)
                Buffer.BlockCopy(array, 2, Data, 0, Data.Length);
        }
        else
        {
            Data = new byte[count];
            if (Data.Length > 0)
                Buffer.BlockCopy(array, 0, Data, 0, Data.Length);
        }
    }

    public byte OpCode { get; }
    public byte[] Data { get; }
    public byte Sequence { get; }
    public bool IsEncrypted { get; set; }

    public byte[] ToArray()
    {
        var buffer = new byte[Data.Length + 5];

        buffer[0] = 0xAA;
        buffer[1] = (byte) ((Data.Length + 2) >> 8);
        buffer[2] = (byte) (Data.Length + 2);
        buffer[3] = OpCode;
        buffer[4] = Sequence;

        for (var i = 0; i < Data.Length; i++)
            buffer[i + 5] = Data[i];

        return buffer;
    }

    public Span<byte> ToSpan()
    {
        //the length of the packet after the length portion of the header plus the packet tail (determined by encryption type)
        var resultLength = Data.Length + (IsEncrypted ? 5 : 4) - 3;

        var resultBuffer = new Span<byte>(new byte[resultLength + 3])
        {
            //write packet header
            [0] = 0xAA,
            [1] = (byte)(resultLength / 256),
            [2] = (byte)(resultLength % 256),
            [3] = (byte)OpCode,
            [4] = Sequence
        };

        Data.CopyTo(resultBuffer[^Data.Length..]);

        return resultBuffer;
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.CurrentCulture, "{0:X2} {1:X2} {2}",
            OpCode,
            Sequence,
            BitConverter.ToString(Data).Replace('-', ' '));
    }
}