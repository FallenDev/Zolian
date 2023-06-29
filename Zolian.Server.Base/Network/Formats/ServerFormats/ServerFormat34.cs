using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat34 : NetworkFormat
{
    private readonly Aisling _aisling;

    /// <summary>
    /// Player Profile
    /// </summary>
    /// <param name="aisling"></param>
    public ServerFormat34(Aisling aisling)
    {
        Encrypted = true;
        OpCode = 0x34;
        _aisling = aisling;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        if (_aisling.Abyss) return;

        writer.Write((uint)_aisling.Serial);

        BuildEquipment(writer);

        writer.Write((byte)_aisling.ActiveStatus);
        writer.WriteStringA(_aisling.Username);
        writer.Write(_aisling.PlayerNation.NationId);
        writer.WriteStringA(_aisling.GameMaster
            ? "Game Master"
            : $"Vit: {_aisling.BaseHp + _aisling.BaseMp * 2}");
        writer.Write((byte)_aisling.PartyStatus);

        writer.WriteStringA($"Level: {_aisling.ExpLevel}  DR: {_aisling.AbpLevel}");
        writer.WriteStringA(_aisling.Stage != ClassStage.Class ? ClassStrings.StageValue(_aisling.Stage) : ClassStrings.ClassValue(_aisling.Path));
        writer.WriteStringA(_aisling.Clan);

        var legends = _aisling.LegendBook.LegendMarks.DistinctBy(m => m.Value);
        var legendsCount = _aisling.LegendBook.LegendMarks;
        var legendItems = legends.ToList();

        writer.Write((byte)legendItems.Count);

        foreach (var mark in legendItems.Where(m => m != null))
        {
            var markCount = legendsCount.Count(item => item.Value == mark.Value);
            writer.Write(mark.Icon);
            writer.Write((byte)LegendColorConverter.ColorToInt(mark.Color));
            writer.WriteStringA(mark.Category);
            writer.WriteStringA(mark.Value + $" - {mark.Time.ToShortDateString()} ({markCount})");
        }

        if (_aisling.PictureData != null)
        {
            writer.Write((ushort)(_aisling.PictureData.Length + _aisling.ProfileMessage.Length + 4));
            writer.Write((ushort)_aisling.PictureData.Length);
            writer.Write(_aisling.PictureData ?? new byte[1]);
            writer.WriteStringB(_aisling.ProfileMessage ?? string.Empty);
        }
        else
        {
            writer.Write((ushort)4);
            writer.Write((ushort)0);
            writer.Write(new byte[1]);
            writer.WriteStringB(string.Empty);
        }
    }

    private void BuildEquipment(NetworkPacketWriter writer)
    {
        if (_aisling.EquipmentManager.Weapon != null)
        {
            writer.Write(_aisling.EquipmentManager.Weapon.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Weapon.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Armor != null)
        {
            writer.Write(_aisling.EquipmentManager.Armor.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Armor.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Shield != null)
        {
            writer.Write(_aisling.EquipmentManager.Shield.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Shield.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Helmet != null)
        {
            writer.Write(_aisling.EquipmentManager.Helmet.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Helmet.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Earring != null)
        {
            writer.Write(_aisling.EquipmentManager.Earring.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Earring.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Necklace != null)
        {
            writer.Write(_aisling.EquipmentManager.Necklace.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Necklace.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.LHand != null)
        {
            writer.Write(_aisling.EquipmentManager.LHand.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.LHand.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.RHand != null)
        {
            writer.Write(_aisling.EquipmentManager.RHand.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.RHand.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.LArm != null)
        {
            writer.Write(_aisling.EquipmentManager.LArm.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.LArm.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.RArm != null)
        {
            writer.Write(_aisling.EquipmentManager.RArm.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.RArm.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Waist != null)
        {
            writer.Write(_aisling.EquipmentManager.Waist.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Waist.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Leg != null)
        {
            writer.Write(_aisling.EquipmentManager.Leg.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Leg.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.FirstAcc != null)
        {
            writer.Write(_aisling.EquipmentManager.FirstAcc.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.FirstAcc.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Foot != null)
        {
            writer.Write(_aisling.EquipmentManager.Foot.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Foot.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Trousers != null)
        {
            writer.Write(_aisling.EquipmentManager.Trousers.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Trousers.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.Coat != null)
        {
            writer.Write(_aisling.EquipmentManager.Coat.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.Coat.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }

        if (_aisling.EquipmentManager.SecondAcc != null)
        {
            writer.Write(_aisling.EquipmentManager.SecondAcc.Item.DisplayImage);
            writer.Write(_aisling.EquipmentManager.SecondAcc.Item.Color);
        }
        else
        {
            writer.Write(ushort.MinValue);
            writer.Write((byte)0x00);
        }
    }
}