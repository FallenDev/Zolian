using System.Numerics;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat29 : NetworkFormat
{
    public ushort CasterEffect;
    public uint CasterSerial;
    public ushort Speed;
    public ushort TargetEffect;
    public uint TargetSerial;
    private readonly ushort _x;
    private readonly ushort _y;
        
    /// <summary>
    /// Spell Animation
    /// </summary>
    /// <param name="animation"></param>
    /// <param name="pos"></param>
    public ServerFormat29(ushort animation, Vector2 pos) : this()
    {
        CasterSerial = 0;
        CasterEffect = animation;
        Speed = 0x64;
        _x = (ushort)pos.X;
        _y = (ushort)pos.Y;
    }

    public ServerFormat29(ushort animation, Vector2 pos, short speed) : this()
    {
        CasterSerial = 0;
        CasterEffect = animation;
        Speed = (ushort)speed;
        _x = (ushort)pos.X;
        _y = (ushort)pos.Y;
    }

    public ServerFormat29()
    {
        Encrypted = true;
        OpCode = 0x29;
    }

    public ServerFormat29(uint casterSerial, uint targetSerial, ushort casterEffect, ushort targetEffect, short speed) : this()
    {
        CasterSerial = casterSerial;
        TargetSerial = targetSerial;
        CasterEffect = casterEffect;
        TargetEffect = targetEffect;
        Speed = (ushort)speed;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        if (CasterSerial == 0)
        {
            writer.Write(uint.MinValue);
            writer.Write(CasterEffect);
            writer.Write((byte)0x00);
            writer.Write((byte)Speed);
            writer.Write(_x);
            writer.Write(_y);
            return;
        }

        writer.Write(TargetSerial);
        writer.Write(CasterSerial);
        writer.Write(CasterEffect);
        writer.Write(TargetEffect);
        writer.Write(Speed);
    }
}