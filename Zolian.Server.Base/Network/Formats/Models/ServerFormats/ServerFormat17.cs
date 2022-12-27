using Darkages.Types;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat17 : NetworkFormat
{
    /// <summary>
    /// Add Spell
    /// </summary>
    /// <param name="spell"></param>
    public ServerFormat17(Spell spell) : this() => Spell = spell;

    private ServerFormat17()
    {
        Encrypted = true;
        Command = 0x17;
    }

    private Spell Spell { get; }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(Spell.Slot);
        writer.Write((ushort)Spell.Template.Icon);
        writer.Write((byte)Spell.Template.TargetType);
        writer.WriteStringA(Spell.Name);
        writer.WriteStringA("\0");
        writer.Write((byte)Math.Clamp(Spell.Lines, 0, Spell.Template.MaxLines));
    }
}