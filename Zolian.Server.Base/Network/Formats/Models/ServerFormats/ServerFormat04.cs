using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat04 : NetworkFormat
{
    /// <summary>
    /// Location
    /// </summary>
    /// <param name="sprite"></param>
    public ServerFormat04(Sprite sprite) : this()
    {
        X = (ushort)sprite.Pos.X;
        Y = (ushort)sprite.Pos.Y;
    }

    private ServerFormat04()
    {
        Encrypted = true;
        OpCode = 0x04;
    }

    private ushort X { get; }
    private ushort Y { get; }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
    }
}