using Darkages.Network.Client.Abstractions;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class ItemScript(Item item) : ObjectManager
{
    protected Item Item { get; } = item;
    public abstract void OnUse(Sprite sprite, byte slot);
    public abstract void Equipped(Sprite sprite, byte displaySlot);
    public abstract void UnEquipped(Sprite sprite, byte displaySlot);
    public virtual void OnDropped(Sprite sprite, Position droppedPosition, Area map) { }
    public virtual void OnPickedUp(Sprite sprite, Position pickedPosition, Area map) { }

    protected void CalculateGearPoints(IWorldClient client)
    {
        var totalPoints = 0;
        foreach (var (_, slot) in client.Aisling.EquipmentManager.Equipment)
        {
            if (slot?.Item == null) continue;

            switch (slot.Item.ItemQuality)
            {
                case Item.Quality.Damaged:
                    totalPoints -= 100;
                    break;
                case Item.Quality.Common:
                    break;
                case Item.Quality.Uncommon:
                    totalPoints += 50;
                    break;
                case Item.Quality.Rare:
                    totalPoints += 100;
                    break;
                case Item.Quality.Epic:
                    totalPoints += 200;
                    break;
                case Item.Quality.Legendary:
                    totalPoints += 400;
                    break;
                case Item.Quality.Forsaken:
                    totalPoints += 500;
                    break;
                case Item.Quality.Mythic:
                    totalPoints += 1000;
                    break;
                case Item.Quality.Primordial:
                    totalPoints += 2000;
                    break;
                case Item.Quality.Transcendent:
                    totalPoints += 3000;
                    break;
            }

            switch (slot.Item.ItemMaterial)
            {
                case Item.ItemMaterials.None:
                    break;
                case Item.ItemMaterials.Copper:
                    totalPoints += 50;
                    break;
                case Item.ItemMaterials.Iron:
                    totalPoints += 100;
                    break;
                case Item.ItemMaterials.Steel:
                    totalPoints += 150;
                    break;
                case Item.ItemMaterials.Forged:
                    totalPoints += 200;
                    break;
                case Item.ItemMaterials.Elven:
                    totalPoints += 250;
                    break;
                case Item.ItemMaterials.Dwarven:
                    totalPoints += 350;
                    break;
                case Item.ItemMaterials.Mythril:
                    totalPoints += 450;
                    break;
                case Item.ItemMaterials.Hybrasyl:
                    totalPoints += 600;
                    break;
                case Item.ItemMaterials.MoonStone:
                    totalPoints += 800;
                    break;
                case Item.ItemMaterials.SunStone:
                    totalPoints += 1000;
                    break;
                case Item.ItemMaterials.Ebony:
                    totalPoints += 1500;
                    break;
                case Item.ItemMaterials.Runic:
                    totalPoints += 2500;
                    break;
                case Item.ItemMaterials.Chaos:
                    totalPoints += 4000;
                    break;
            }

            switch (slot.Item.GearEnhancement)
            {
                case Item.GearEnhancements.None:
                    break;
                case Item.GearEnhancements.One:
                    totalPoints += 50;
                    break;
                case Item.GearEnhancements.Two:
                    totalPoints += 100;
                    break;
                case Item.GearEnhancements.Three:
                    totalPoints += 200;
                    break;
                case Item.GearEnhancements.Four:
                    totalPoints += 300;
                    break;
                case Item.GearEnhancements.Five:
                    totalPoints += 500;
                    break;
                case Item.GearEnhancements.Six:
                    totalPoints += 750;
                    break;
                case Item.GearEnhancements.Seven:
                    totalPoints += 1500;
                    break;
                case Item.GearEnhancements.Eight:
                    totalPoints += 2500;
                    break;
                case Item.GearEnhancements.Nine:
                    totalPoints += 4000;
                    break;
            }

            if (slot.Item.ItemVariance != Item.Variance.None)
                totalPoints += 200;

            if (slot.Item.WeapVariance != Item.WeaponVariance.None)
                totalPoints += 500;
        }

        client.Aisling.GamePoints = (uint)totalPoints;
    }
}