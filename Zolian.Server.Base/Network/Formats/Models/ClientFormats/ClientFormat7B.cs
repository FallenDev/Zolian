namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat7B : NetworkFormat
{
    /// <summary>
    /// Client Metafile Request
    /// </summary>
    public ClientFormat7B()
    {
        Encrypted = true;
        Command = 0x7B;
    }

    public string Name { get; private set; }
    public byte Type { get; private set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        Type = reader.ReadByte();

        #region Type 0

        if (Type == 0x00)
            Name = reader.ReadStringA();

        #endregion Type 0

        #region Type 1

        if (Type == 0x01 && reader.Packet.Data.Length > 2) Name = reader.ReadStringB();

        #endregion Type 1
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}