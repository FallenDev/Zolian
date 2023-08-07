using System.Collections.Concurrent;

using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IItem : ISprite
{
    uint ItemId { get; set; }
    ItemTemplate Template { get; set; }
    string Name { get; set; }
    byte Slot { get; set; }
    byte InventorySlot { get; set; }
    byte Color { get; set; }
    bool Cursed { get; set; }
    uint Durability { get; set; }
    uint MaxDurability { get; set; }
    bool Identified { get; init; }
    ushort Stacks { get; set; }
    bool Enchantable { get; set; }
    bool Tarnished { get; set; }

    Sprite[] AuthenticatedAislings { get; set; }
    ConcurrentDictionary<string, ItemScript> Scripts { get; set; }
    ConcurrentDictionary<string, WeaponScript> WeaponScripts { get; set; }

    int Dropping { get; set; }
    bool[] Warnings { get; set; }

    string GetDisplayName();
    string NoColorGetDisplayName();

    Item Create(Sprite owner, string item, Item.Quality quality, Item.Variance variance,
        Item.WeaponVariance wVariance, bool curse = false);

    Item Create(Sprite owner, ItemTemplate itemTemplate, Item.Quality quality, Item.Variance variance,
        Item.WeaponVariance wVariance, bool curse = false);

    Item Create(Sprite owner, string item, bool curse = false);
    Item Create(Sprite owner, ItemTemplate itemTemplate, bool curse = false);
    Item Create(Area map, ItemTemplate itemTemplate);
    Item.Quality QualityRestriction(Item item);
    Item TrapCreate(Sprite owner, ItemTemplate itemTemplate);
    bool CanCarry(Sprite sprite);
    bool GiveTo(Sprite sprite, bool checkWeight = true);
    void Release(Sprite owner, Position position, bool delete = true);
    void DeleteFromAislingDb();
    void ReapplyItemModifiers(WorldClient client);
    void RemoveModifiers(WorldClient client);
    void StatModifiersCalc(WorldClient client, Item equipment);
    void SpellLines(WorldClient client);
    void ItemVarianceCalc(WorldClient client, Item equipment);
    void WeaponVarianceCalc(WorldClient client, Item equipment);
    void QualityVarianceCalc(WorldClient client, Item equipment);
    void UpdateSpell(WorldClient client, Spell spell);
}