using Darkages.Types;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat15 : NetworkFormat
{
    /// <summary>
    /// Map Information
    /// </summary>
    /// <param name="area"></param>
    public ServerFormat15(Area area) : this() => Area = area;

    private ServerFormat15()
    {
        Encrypted = true;
        OpCode = 0x15;
    }

    private Area Area { get; }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        if (Area == null) return;
        writer.Write((ushort)Area.ID);
        writer.Write((byte)Area.Cols);
        writer.Write((byte)Area.Rows);
        writer.Write((byte)Area.Flags);
        writer.Write(ushort.MinValue);
        writer.Write((byte)(Area.Hash / 256));
        writer.Write((byte)(Area.Hash % 256));
        writer.WriteStringA(Area.Name);
    }
}