using System.Collections.Concurrent;

using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IItem : ISprite
{
    Sprite[] AuthenticatedAislings { get; set; }
    byte Color { get; set; }
    bool Cursed { get; set; }
    ushort DisplayImage { get; init; }
    string Name { get; set; }
    uint Durability { get; set; }
    uint MaxDurability { get; set; }
    bool Equipped { get; set; }
    bool Identified { get; init; }
    ushort Image { get; init; }
    int ItemId { get; set; }
    int Owner { get; set; }
    ConcurrentDictionary<string, ItemScript> Scripts { get; set; }
    ConcurrentDictionary<string, WeaponScript> WeaponScripts { get; set; }
    byte InventorySlot { get; set; }
    byte Slot { get; set; }
    ushort Stacks { get; set; }
    int Dropping { get; set; }
    ItemTemplate Template { get; set; }
    bool Enchantable { get; set; }
    bool[] Warnings { get; init; }

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
    void ReapplyItemModifiers(GameClient client);
    void RemoveModifiers(GameClient client);
    void StatModifiersCalc(GameClient client, Item equipment);
    void SpellLines(GameClient client);
    void ItemVarianceCalc(GameClient client, Item equipment);
    void WeaponVarianceCalc(GameClient client, Item equipment);
    void QualityVarianceCalc(GameClient client, Item equipment);
    void UpdateSpellSlot(GameClient client, byte slot);
}