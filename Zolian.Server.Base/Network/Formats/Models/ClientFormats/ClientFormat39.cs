using System.Text;

namespace Darkages.Network.Formats.Models.ClientFormats;

public class ClientFormat39 : NetworkFormat
{
    /// <summary>
    /// Request Pursuit
    /// </summary>
    public ClientFormat39()
    {
        Encrypted = true;
        OpCode = 0x39;
    }

    public string Args { get; private set; }
    public int Serial { get; private set; }
    public ushort Step { get; private set; }
    private byte Type { get; set; }

    public override void Serialize(NetworkPacketReader reader)
    {
        Type = reader.ReadByte();
        Serial = reader.ReadInt32();
        Step = reader.ReadUInt16();

        if (!reader.GetCanRead()) return;
        var length = reader.ReadByte();

        // ToDo: Step correlates directly to the mundane script and needs to correlate to remove skills/spells
        if (Step is 0x0500 or 0x0800 or 0x9000)
        {
            Args = Convert.ToString(length);
        }
        else
        {
            var data = reader.ReadBytes(length);
            Args = Encoding.ASCII.GetString(data);
        }
    }

    public override void Serialize(NetworkPacketWriter writer) { }
}