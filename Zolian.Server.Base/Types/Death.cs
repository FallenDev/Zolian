using System.Numerics;
using Dapper;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Types;

public class Death
{
    private Vector2 Location { get; set; }
    private int MapId { get; set; }
    public Aisling Owner { get; set; }

    public void Reap(Aisling player)
    {
        Owner = player;
        if (Owner == null) return;

        Location = Owner.Pos;
        MapId = Owner.CurrentMapId;

        ReapEquipment();
        ReapInventory();
        ReapGold();

        Owner.Client.SendMessage(0x02, $"{ServerSetup.Instance.Config.DeathReapingMessage}");
        Owner.Client.SendStats(StatusFlags.All);
        Owner.Client.UpdateDisplay();
    }

    private void ReapInventory()
    {
        var batch = Owner.Inventory.Items.Select(i => i.Value).Where(i => i != null).Where(i => i is { Template: { } }).ToList();

        foreach (var obj in batch)
        {
            if (!obj.Template.Flags.FlagIsSet(ItemFlags.Dropable)) continue;
            if (obj.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuest)) continue;
            if (obj.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuestNoConsume)) continue;
            if (obj.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuestUnique)) continue;
            if (obj.Template.Flags.FlagIsSet(ItemFlags.DropScript)) continue;

            if (obj.Durability > 0 && obj.Template.Flags.FlagIsSet(ItemFlags.Equipable))
            {
                var duraLost = obj.Durability * 10 / 100;

                if (obj.Durability >= duraLost)
                {
                    obj.Durability -= duraLost;
                }
                else
                {
                    obj.Durability = 0;
                }

                if (obj.Template.Flags.FlagIsSet(ItemFlags.Perishable))
                {
                    if (obj.ItemQuality != Item.Quality.Damaged)
                    {
                        obj.ItemQuality -= 1;
                    }
                }
            }

            Owner.EquipmentManager.RemoveFromInventory(obj, true);
            ReleaseInventory(obj);
        }

        foreach (var inventory in Owner.Inventory.Items)
        {
            if (inventory.Value == null) continue;
            if (inventory.Value.Stacks > 1)
            {
                for (var i = 0; i < inventory.Value.Stacks; i++)
                {
                    Owner.CurrentWeight += inventory.Value.Template.CarryWeight;
                }
            }
            else
            {
                Owner.CurrentWeight += inventory.Value.Template.CarryWeight;
            }
        }
    }

    private void ReapEquipment()
    {
        var batch = Owner.EquipmentManager.Equipment.Select(i => i.Value).Where(i => i != null).Where(i => i is { Item.Template: { } }).ToList();

        foreach (var obj in batch)
        {
            if (!obj.Item.Template.Flags.FlagIsSet(ItemFlags.Dropable)) continue;
            if (obj.Item.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuest)) continue;
            if (obj.Item.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuestNoConsume)) continue;
            if (obj.Item.Template.Flags.FlagIsSet(ItemFlags.NonDropableQuestUnique)) continue;
            if (obj.Item.Template.Flags.FlagIsSet(ItemFlags.DropScript)) continue;

            var duraLost = obj.Item.Durability * 10 / 100;

            if (obj.Item.Durability >= duraLost)
            {
                obj.Item.Durability -= duraLost;
            }
            else
            {
                obj.Item.Durability = 0;
            }

            if (obj.Item.Template.Flags.FlagIsSet(ItemFlags.PerishIFEquipped) ||
                obj.Item.Template.Flags.FlagIsSet(ItemFlags.Perishable))
            {
                if (obj.Item.ItemQuality != Item.Quality.Damaged)
                {
                    obj.Item.ItemQuality -= 1;
                }
            }

            Owner.EquipmentManager.RemoveFromExisting(obj.Slot, false);
            ReleaseEquipment(obj.Item);
        }
    }

    private void ReapGold()
    {
        var gold = Owner.GoldPoints;
        {
            Money.Create(Owner, gold, Owner.Position);
            Owner.GoldPoints = 0;
        }
    }

    private void ReleaseInventory(Item item)
    {
        item.Pos = Location;
        item.CurrentMapId = MapId;

        var readyTime = DateTime.Now;
        item.AbandonedDate = readyTime;
        item.Cursed = false;

        item.DeleteFromAislingDb();
        item.Serial = Generator.GenerateNumber();
        item.ItemId = Generator.GenerateNumber();

        item.AddObject(item);

        foreach (var player in item.AislingsNearby())
        {
            item.ShowTo(player);
        }
    }

    private void ReleaseEquipment(Item item)
    {
        item.Pos = Location;
        item.CurrentMapId = MapId;

        var readyTime = DateTime.Now;
        item.AbandonedDate = readyTime;
        item.Cursed = false;

        DeleteFromAislingDb(item);
        item.Serial = Generator.GenerateNumber();
        item.ItemId = Generator.GenerateNumber();

        item.AddObject(item);

        foreach (var player in item.AislingsNearby())
        {
            item.ShowTo(player);
        }
    }

    private static async void DeleteFromAislingDb(Item item)
    {
        var sConn = new SqlConnection(AislingStorage.ConnectionString);
        if (item.ItemId == 0) return;

        try
        {
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersEquipped WHERE ItemId = @ItemId";
            await sConn.ExecuteAsync(cmd, new { item.ItemId });
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }
}