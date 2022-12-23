using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public interface IDialogData : IFormattableNetwork
    {
        byte Type { get; }
    }

    public class ServerFormat2F : NetworkFormat
    {
        /// <summary>
        /// NPC Reply
        /// </summary>
        public ServerFormat2F(Mundane mundane, string text, IDialogData data) : this()
        {
            Mundane = mundane;
            Text = text;
            Data = data;
        }

        private ServerFormat2F()
        {
            Command = 0x2F;
            Encrypted = true;
        }

        private IDialogData Data { get; }
        private Mundane Mundane { get; }
        private string Text { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Data.Type); // Type
            writer.Write((byte)0x01); // Obj Type
            writer.Write((uint)Mundane.Serial);
            
            writer.Write((byte)0x01); // 0x01 Unknown
            writer.Write((ushort)Mundane.Template.Image); // 0x4000 + Sprite # = Template.Image
            writer.Write(byte.MinValue); // Color

            writer.Write((byte)0x01); // 0x01 Unknown
            writer.Write((ushort)Mundane.Template.Image); // Replay Sprite Image
            writer.Write(byte.MinValue); // Color

            writer.WriteStringB(Mundane.Template.Name);
            writer.WriteStringB(Text); // Mundane Text
            writer.Write(Data); // Mundane Options
        }
    }

    public class BankingData : IDialogData
    {
        public BankingData(ushort step, IEnumerable<byte> items)
        {
            Step = step;
            ItemsPosition = items;
        }

        private IEnumerable<byte> ItemsPosition { get; }
        private ushort Step { get; }

        public byte Type => 0x05; // User Inventory Items

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.Write((byte)Step);
            writer.Write((short)ItemsPosition.Count());

            foreach (var item in ItemsPosition)
                writer.Write(item);
        }
    }

    public class WithdrawBankData : IDialogData
    {
        public WithdrawBankData(ushort step, Bank data)
        {
            Step = step;
            Data = data;
        }

        private Bank Data { get; }
        private ushort Step { get; }
        public byte Type => 0x04; // Merchant Shop Items

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Step);
            writer.Write((ushort)Data.Items.Count);

            foreach (var item in Data.Items.Values)
            {
                writer.Write(item.DisplayImage);
                writer.Write(item.Color);
                writer.Write((uint)Data.Items[item.ItemId].Stacks);
                writer.WriteStringA(item.NoColorDisplayName);
                writer.WriteStringA(ItemEnumConverters.QualityToString(item.OriginalQuality));
            }
        }
    }

    public class ItemSellData : IDialogData
    {
        public ItemSellData(ushort step, IEnumerable<byte> items)
        {
            Step = step;
            Items = items;
        }

        private IEnumerable<byte> Items { get; }
        private ushort Step { get; }

        public byte Type => 0x05;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Type);
            writer.Write((short)Items.Count());

            foreach (var item in Items)
                writer.Write(item);
        }
    }

    public class ItemShopData : IDialogData
    {
        public ItemShopData(ushort step, IEnumerable<ItemTemplate> items)
        {
            Step = step;
            Items = items;
        }

        private IEnumerable<ItemTemplate> Items { get; }
        private ushort Step { get; }

        public byte Type => 0x04;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Step);
            writer.Write((ushort)Items.Count());

            foreach (var item in Items)
            {
                writer.Write(item.DisplayImage);
                writer.Write((byte)item.Color);
                writer.Write(item.Value);
                writer.WriteStringA(item.Name);
                writer.WriteStringA(item.Class.ToString());
            }
        }
    }

    public class OptionsData : List<OptionsDataItem>, IDialogData
    {
        public OptionsData(IEnumerable<OptionsDataItem> collection) : base(collection) { }

        public byte Type => 0x00;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.Write((byte)Count);

            foreach (var option in this.Where(option => option.Text != null))
            {
                writer.WriteStringA(option.Text);
                writer.Write(option.Step);
            }
        }
    }

    public class OptionsDataItem
    {
        public OptionsDataItem(short step, string text)
        {
            Step = step;
            Text = text;
        }

        public short Step { get; }
        public string Text { get; }
    }

    public class OptionsPlusArgsData : List<OptionsDataItem>, IDialogData
    {
        public OptionsPlusArgsData(IEnumerable<OptionsDataItem> collection, string args) : base(collection) => Args = args;

        private string Args { get; }

        public byte Type => 0x01;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.WriteStringA(Args);
            writer.Write((byte)Count);

            foreach (var option in this)
            {
                writer.WriteStringA(option.Text);
                writer.Write(option.Step);
            }
        }
    }

    public class PopupFormat : NetworkFormat
    {
        public PopupFormat(Mundane popup, string text, IDialogData data) : this()
        {
            Mundane = popup;
            Text = text;
            Data = data;
        }

        private PopupFormat()
        {
            Command = 0x2F;
            Encrypted = true;
        }

        private IDialogData Data { get; }
        private Mundane Mundane { get; }
        private string Text { get; }

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(byte.MinValue); // Options = 0
            writer.Write((byte)0x01); // Obj Type Mundane
            writer.Write((uint)Mundane.Serial);

            writer.Write((byte)0x01); // 0x01 Unknown
            writer.Write((ushort)Mundane.Template.Image); // 0x4000 + Sprite # = Template.Image
            writer.Write(byte.MinValue); // Color

            writer.Write((byte)0x01); // 0x01 Unknown
            writer.Write((ushort)Mundane.Template.Image); // 0x4000 + Sprite # = Template.Image
            writer.Write(byte.MinValue); // Color

            writer.WriteStringB(Mundane.Template.Name);
            writer.WriteStringB(Text); // Mundane Text
            writer.Write(Data); // Mundane Data
        }
    }

    public class SkillAcquireData : IDialogData
    {
        public SkillAcquireData(ushort step, IEnumerable<SkillTemplate> skills)
        {
            Step = step;
            Skills = skills;
        }

        private IEnumerable<SkillTemplate> Skills { get; }
        private ushort Step { get; }
        public byte Type => 0x07;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Step);
            writer.Write((ushort)Skills.Count());

            foreach (var skill in Skills)
            {
                writer.Write((byte)0x03);
                writer.Write((ushort)skill.Icon);
                writer.Write((byte)0x00);
                writer.WriteStringA(skill.Name);
            }
        }
    }

    public class SkillForfeitData : IDialogData
    {
        public SkillForfeitData(ushort step) => Step = step;

        private ushort Step { get; }
        public byte Type => 0x09;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer) => writer.Write(Step);
    }

    public class SpellAcquireData : IDialogData
    {
        public SpellAcquireData(ushort step, IEnumerable<SpellTemplate> spells)
        {
            Step = step;
            Spells = spells;
        }

        private IEnumerable<SpellTemplate> Spells { get; }
        private ushort Step { get; }
        public byte Type => 0x06;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer)
        {
            writer.Write(Step);
            writer.Write((ushort)Spells.Count());

            foreach (var spell in Spells)
            {
                writer.Write((byte)0x02);
                writer.Write((ushort)spell.Icon);
                writer.Write((byte)0x00);
                writer.WriteStringA(spell.Name);
            }
        }
    }

    public class SpellForfeitData : IDialogData
    {
        public SpellForfeitData(ushort step) => Step = step;

        private ushort Step { get; }
        public byte Type => 0x08;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer) => writer.Write(Step);
    }

    public class TextInputData : IDialogData
    {
        private ushort Step { get; set; }

        public byte Type => 0x02;

        public void Serialize(NetworkPacketReader reader) { }

        public void Serialize(NetworkPacketWriter writer) => writer.Write(Step);
    }
}