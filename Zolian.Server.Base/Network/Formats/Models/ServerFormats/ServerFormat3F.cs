namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat3F : NetworkFormat
{
    private readonly byte _isSkill;
    private readonly byte _slot;
    private readonly int _time;

    /// <summary>
    /// Cooldown
    /// </summary>
    public ServerFormat3F(byte isSkill, byte slot, int time) : this()
    {
        _isSkill = isSkill;
        _slot = slot;
        _time = time;
    }

    private ServerFormat3F()
    {
        Encrypted = true;
        OpCode = 0x3F;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        writer.Write(_isSkill);
        writer.Write(_slot);
        writer.Write((uint)_time);
    }
}