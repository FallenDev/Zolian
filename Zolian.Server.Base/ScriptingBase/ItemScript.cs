using Darkages.Network.Client.Abstractions;
using Darkages.Object;
using Darkages.Sprites;
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
    
    public void CalculateGearPoints(IWorldClient client)
    {
        var totalPoints = 0;
        foreach (var slot in client.Aisling.EquipmentManager.Equipment.Values)
        {
            var itemPoints = 0;
            if (slot?.Item == null) continue;
            switch (slot.Item.ItemQuality)
            {
                case Item.Quality.Damaged:
                    itemPoints -= 100;
                    break;
                case Item.Quality.Common:
                    break;
                case Item.Quality.Uncommon:
                    itemPoints += 50;
                    break;
                case Item.Quality.Rare:
                    itemPoints += 100;
                    break;
                case Item.Quality.Epic:
                    itemPoints += 200;
                    break;
                case Item.Quality.Legendary:
                    itemPoints += 400;
                    break;
                case Item.Quality.Forsaken:
                    itemPoints += 500;
                    break;
                case Item.Quality.Mythic:
                    itemPoints += 1000;
                    break;
                case Item.Quality.Primordial:
                case Item.Quality.Transcendent:
                    itemPoints += 2000;
                    break;
            }

            switch (slot.Item.ItemMaterial)
            {
                case Item.ItemMaterials.None:
                    break;
                case Item.ItemMaterials.Copper:
                    itemPoints += 50;
                    break;
                case Item.ItemMaterials.Iron:
                    itemPoints += 100;
                    break;
                case Item.ItemMaterials.Steel:
                    itemPoints += 150;
                    break;
                case Item.ItemMaterials.Forged:
                    itemPoints += 200;
                    break;
                case Item.ItemMaterials.Elven:
                    itemPoints += 250;
                    break;
                case Item.ItemMaterials.Dwarven:
                    itemPoints += 350;
                    break;
                case Item.ItemMaterials.Mythril:
                    itemPoints += 450;
                    break;
                case Item.ItemMaterials.Hybrasyl:
                    itemPoints += 600;
                    break;
                case Item.ItemMaterials.MoonStone:
                    itemPoints += 800;
                    break;
                case Item.ItemMaterials.SunStone:
                    itemPoints += 1000;
                    break;
                case Item.ItemMaterials.Ebony:
                    itemPoints += 1500;
                    break;
                case Item.ItemMaterials.Runic:
                    itemPoints += 2500;
                    break;
                case Item.ItemMaterials.Chaos:
                    itemPoints += 4000;
                    break;
            }

            switch (slot.Item.GearEnhancement)
            {
                case Item.GearEnhancements.None:
                    break;
                case Item.GearEnhancements.One:
                    itemPoints += 50;
                    break;
                case Item.GearEnhancements.Two:
                    itemPoints += 100;
                    break;
                case Item.GearEnhancements.Three:
                    itemPoints += 200;
                    break;
                case Item.GearEnhancements.Four:
                    itemPoints += 300;
                    break;
                case Item.GearEnhancements.Five:
                    itemPoints += 400;
                    break;
                case Item.GearEnhancements.Six:
                    itemPoints += 500;
                    break;
                case Item.GearEnhancements.Seven:
                    itemPoints += 600;
                    break;
                case Item.GearEnhancements.Eight:
                    itemPoints += 800;
                    break;
                case Item.GearEnhancements.Nine:
                    itemPoints += 1500;
                    break;
            }

            if (slot.Item.ItemVariance != Item.Variance.None)
                itemPoints += 200;

            if (slot.Item.WeapVariance != Item.WeaponVariance.None)
                itemPoints += 500;

            totalPoints += itemPoints;
        }

        client.Aisling.GamePoints = (uint)totalPoints;
    }
}