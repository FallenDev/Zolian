using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat07 : NetworkFormat
{
    private readonly List<Sprite> _sprites;

    /// <summary>
    /// Add World Object
    /// </summary>
    /// <param name="objectsToAdd"></param>
    public ServerFormat07(IEnumerable<Sprite> objectsToAdd)
    {
        Encrypted = true;
        OpCode = 0x07;
        _sprites = new List<Sprite>(objectsToAdd);
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        if (_sprites.Count <= 0) return;

        writer.Write((ushort)_sprites.Count);

        foreach (var sprite in _sprites)
        {
            if (sprite is not Money money) continue;
            writer.Write((ushort)money.Pos.X);
            writer.Write((ushort)money.Pos.Y);
            writer.Write((uint)money.Serial);
            writer.Write(money.Image);
            writer.Write(byte.MinValue);
            writer.Write(byte.MinValue);
            writer.Write(byte.MinValue);
        }

        foreach (var sprite in _sprites)
        {
            switch (sprite)
            {
                case Item item:
                    writer.Write((ushort)item.Pos.X);
                    writer.Write((ushort)item.Pos.Y);
                    writer.Write((uint)item.Serial);
                    writer.Write(item.DisplayImage);
                    writer.Write(item.Color);
                    writer.Write(byte.MinValue);
                    writer.Write(byte.MinValue);
                    break;
                case Monster monster:
                    writer.Write((ushort)monster.Pos.X);
                    writer.Write((ushort)monster.Pos.Y);
                    writer.Write((uint)monster.Serial);
                    writer.Write(monster.Image);
                    writer.Write((uint)0x0);
                    writer.Write(monster.Direction);
                    writer.Write(byte.MinValue);
                    writer.Write(byte.MinValue); // Monster Type
                    /*
                     * Normal = 0
                     * WalkThrough = 1
                     * Merchant = 2
                     * WhiteSquare = 3
                     * User = 4
                     */
                    break;
                case Mundane mundane:
                    writer.Write((ushort)mundane.Pos.X);
                    writer.Write((ushort)mundane.Pos.Y);
                    writer.Write((uint)mundane.Serial);
                    writer.Write((ushort)mundane.Template.Image);
                    writer.Write(uint.MinValue);
                    writer.Write(mundane.Direction);
                    writer.Write(byte.MinValue);
                    writer.Write((byte)0x02);
                    writer.WriteStringA(mundane.Template.Name);
                    break;
            }
        }
    }
}