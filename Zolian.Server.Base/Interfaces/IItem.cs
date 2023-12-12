using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IItem : ISprite
{
    long ItemId { get; set; }
    string GetDisplayName();
    string NoColorGetDisplayName();
    Item Create(Sprite owner, string item, Item.Quality quality, Item.Variance variance, Item.WeaponVariance wVariance, bool curse = false);
    Item Create(Sprite owner, ItemTemplate itemTemplate, Item.Quality quality, Item.Variance variance, Item.WeaponVariance wVariance, bool curse = false);
    Item Create(Sprite owner, string item, bool curse = false);
    Item Create(Sprite owner, ItemTemplate itemTemplate, bool curse = false);
    Item Create(Area map, ItemTemplate itemTemplate);
    Item.Quality QualityRestriction(Item item);
    Trap TrapCreate(Sprite owner, ItemTemplate itemTemplate, int duration, int radius = 1, Action<Sprite, Sprite> cb = null);
    bool CanCarry(Sprite sprite);
    bool GiveTo(Sprite sprite, bool checkWeight = true);
    void Release(Sprite owner, Position position);
    void DeleteFromAislingDb();
    void ReapplyItemModifiers(WorldClient client);
    void RemoveModifiers(WorldClient client);
    void StatModifiersCalc(WorldClient client, Item equipment);
    void SpellLines(WorldClient client);
    void ItemVarianceCalc(WorldClient client, Item equipment);
    void WeaponVarianceCalc(WorldClient client, Item equipment);
    void QualityVarianceCalc(WorldClient client, Item equipment);
    void BuffDebuffCalc(WorldClient client);
    void UpdateSpell(WorldClient client, Spell spell);
}