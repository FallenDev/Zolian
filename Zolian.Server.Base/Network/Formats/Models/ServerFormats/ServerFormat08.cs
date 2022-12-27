using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat08 : NetworkFormat
{
    /// <summary>
    /// Attributes
    /// </summary>
    /// <param name="aisling"></param>
    /// <param name="flags"></param>
    public ServerFormat08(Aisling aisling, StatusFlags flags) : this()
    {
        Aisling = aisling;
        Flags = (byte)flags;
    }

    private ServerFormat08()
    {
        Encrypted = true;
        Command = 0x08;
    }

    private Aisling Aisling { get; }
    private byte Flags { get; set; }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        if (Aisling.GameMaster)
            Flags |= 0x40;
        else
            Flags |= 0x40 | 0x80;

        writer.Write(Flags);

        var hp = Aisling.MaximumHp is >= int.MaxValue or <= 0 ? 1 : Aisling.MaximumHp;
        var mp = Aisling.MaximumMp is >= int.MaxValue or <= 0 ? 1 : Aisling.MaximumMp;

        var chp = Aisling.CurrentHp is >= int.MaxValue or <= 0 ? 1 : Aisling.CurrentHp;
        var cmp = Aisling.CurrentMp is >= int.MaxValue or <= 0 ? 1 : Aisling.CurrentMp;

        // Struct A
        if ((Flags & 0x20) != 0)
        {
            writer.Write((byte)1);
            writer.Write((byte)0);
            writer.Write((byte)0);

            writer.Write((byte)Aisling.ExpLevel);
            writer.Write((byte)Aisling.AbpLevel);

            writer.Write((uint)hp);
            writer.Write((uint)mp);

            writer.Write((byte)Math.Clamp(Aisling.Str, 0, ServerSetup.Instance.Config.StatCap));
            writer.Write((byte)Math.Clamp(Aisling.Int, 0, ServerSetup.Instance.Config.StatCap));
            writer.Write((byte)Math.Clamp(Aisling.Wis, 0, ServerSetup.Instance.Config.StatCap));
            writer.Write((byte)Math.Clamp(Aisling.Con, 0, ServerSetup.Instance.Config.StatCap));
            writer.Write((byte)Math.Clamp(Aisling.Dex, 0, ServerSetup.Instance.Config.StatCap));

            if (Aisling.StatPoints > 0)
            {
                writer.Write((byte)1);
                writer.Write((byte)Aisling.StatPoints);
            }
            else
            {
                writer.Write((byte)0);
                writer.Write((byte)0);
            }

            writer.Write((ushort)Math.Clamp(Aisling.MaximumWeight, 50, 600));
            writer.Write((ushort)Aisling.CurrentWeight);
            writer.Write(uint.MinValue);
        }

        // Struct B
        if ((Flags & 0x10) != 0)
        {
            writer.Write((uint)chp);
            writer.Write((uint)cmp);
        }

        // Struct C
        if ((Flags & 0x08) != 0)
        {
            writer.Write(Aisling.ExpTotal);
            writer.Write(Aisling.ExpLevel >= ServerSetup.Instance.Config.PlayerLevelCap
                ? 0
                : Aisling.ExpNext);
            writer.Write(Aisling.AbpTotal);
            writer.Write(Aisling.AbpNext);
            writer.Write(Aisling.GamePoints);
            writer.Write(Aisling.GoldPoints);
        }

        // Struct D
        if ((Flags & 0x04) == 0) return;

        writer.Write(byte.MinValue);
        writer.Write(Aisling.Blind);
        writer.Write(byte.MinValue);
        writer.Write(byte.MinValue);
        writer.Write(byte.MinValue);
        writer.Write((byte)Aisling.MailFlags);
        writer.Write((byte)Aisling.OffenseElement);
        writer.Write((byte)Aisling.DefenseElement);
        writer.Write((byte)(Aisling.Regen / 10));
        writer.Write(byte.MinValue);
        writer.Write((sbyte)Math.Clamp(Aisling.Ac, sbyte.MinValue, sbyte.MaxValue));
        writer.Write((sbyte)Math.Clamp(Aisling.Dmg, sbyte.MinValue, sbyte.MaxValue));
        writer.Write((sbyte)Math.Clamp(Aisling.Hit, sbyte.MinValue, sbyte.MaxValue));
    }
}