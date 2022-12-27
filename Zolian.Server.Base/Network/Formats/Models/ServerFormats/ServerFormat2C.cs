namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat2C : NetworkFormat
{
    private readonly short _icon;
    private readonly byte _slot;
    private readonly string _text;
        
    /// <summary>
    /// Add Skill
    /// </summary>
    /// <param name="slot"></param>
    /// <param name="icon"></param>
    /// <param name="text"></param>
    public ServerFormat2C(byte slot, short icon, string text) : this()
    {
        _slot = slot;
        _icon = icon;
        _text = text;
    }

    private ServerFormat2C()
    {
        Command = 0x2C;
        Encrypted = true;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(_slot);
        writer.Write(_icon);
        writer.WriteStringA(_text);
    }
}