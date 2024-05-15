using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Darkages.Enums;
using Darkages.Object;
using Darkages.Sprites;

namespace Darkages.Types;

public class Death
{
    public static void Reap(Aisling player)
    {
        if (player == null) return;
        player.DeathLocation = player.Pos;
        player.DeathMapId = player.CurrentMapId;
        
        ReapInventory(player);
        ReapGold(player);
        ReapEquipment(player);

        player.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.DeathReapingMessage}");
        player.Client.GiveScar();
    }

    private static void ReapInventory(Aisling player)
    {
        var batch = player.Inventory.Items.Select(i => i.Value).Where(i => i != null).Where(i => i is { Template: not null }).ToList();

        foreach (var obj in from obj in batch 
                 where obj.Template.Flags.FlagIsSet(ItemFlags.Dropable) 
                 where !obj.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuest) 
                 where !obj.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuestNoConsume) 
                 where !obj.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuestUnique) 
                 where !obj.Template.Flags.FlagIsSet(ItemFlags.DropScript) select obj)
        {
            if (obj.Durability > 0 && obj.Template.Flags.FlagIsSet(ItemFlags.Equipable))
            {
                var duraLost = obj.Durability * 10 / 100;

                if (obj.Durability > duraLost)
                {
                    obj.Durability -= duraLost;
                }
                else
                {
                    obj.Durability = 0;
                }

                obj.Tarnished = true;
            }

            player.Inventory.Items.TryUpdate(obj.InventorySlot, null, obj);
            player.Client.SendRemoveItemFromPane(obj.InventorySlot);
            ReleaseItem(player, obj);
        }
    }

    private static void ReapEquipment(Aisling player)
    {
        var batch = player.EquipmentManager.Equipment.Select(i => i.Value).Where(i => i != null).Where(i => i is { Item.Template: not null }).ToList();

        foreach (var obj in batch)
        {
            var duraLost = obj.Item.Durability * 10 / 100;

            if (obj.Item.Durability > duraLost)
            {
                obj.Item.Durability -= duraLost;
            }
            else
            {
                obj.Item.Durability = 0;
            }

            if (obj.Item.Template.Flags.FlagIsSet(ItemFlags.PerishIfEquipped) ||
                obj.Item.Template.Flags.FlagIsSet(ItemFlags.Perishable))
            {
                if (obj.Item.ItemQuality == Item.Quality.Damaged)
                {
                    obj.Item.Remove();
                }

                obj.Item.Tarnished = true;
            }

            player.EquipmentManager.RemoveFromExisting(obj.Slot);
        }

        player.ArmorImg = 0;
        player.WeaponImg = 0;
        player.ShieldImg = 0;
        player.HelmetImg = 0;
        player.OverCoatImg = 0;
        player.HeadAccessoryImg = 0;
        player.Accessory1Img = 0;
        player.Accessory2Img = 0;
        player.Accessory3Img = 0;
        player.BootsImg = 0;
    }

    private static void ReapGold(Aisling player)
    {
        var gold = player.GoldPoints;
        if (gold <= 0) return;
        Money.Create(player, (uint)gold, new Position(player.DeathLocation));
        player.GoldPoints = 0;
    }

    private static void ReleaseItem(Aisling player, Item item)
    {
        item.Pos = player.DeathLocation;
        item.CurrentMapId = player.DeathMapId;

        var readyTime = DateTime.UtcNow;
        item.AbandonedDate = readyTime;
        item.Cursed = false;

        item.DeleteFromAislingDb();
        item.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        item.ItemPane = Item.ItemPanes.Ground;
        ObjectManager.AddObject(item);

        foreach (var aisling in item.AislingsNearby())
            item.ShowTo(aisling);
    }
}