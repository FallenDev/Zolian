using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using Darkages.GameScripts.Creations;

namespace Darkages.GameScripts.Areas.Undine;

[Script("Undine")]
public class Undine : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];

    private readonly SortedDictionary<Item.Quality, int> _qualityLuckModifiers = new()
    {
        { Item.Quality.Damaged, 0 },
        { Item.Quality.Common, 0 },
        { Item.Quality.Uncommon, 1 },
        { Item.Quality.Rare, 3 },
        { Item.Quality.Epic, 5 },
        { Item.Quality.Legendary, 50 },
        { Item.Quality.Forsaken, 75 },
        { Item.Quality.Mythic, 99 }
    };

    public Undine(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped)
    {
        switch (locationDropped.X)
        {
            case 62 when locationDropped.Y == 47:
            case 62 when locationDropped.Y == 48:
                UndineAltar(client, itemDropped);
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private void UndineAltar(WorldClient client, Item itemDropped)
    {
        var loop = itemDropped.Dropping;
        var luck = 0 + client.Aisling.Luck;

        if (loop == 0) loop = 1;

        if (_qualityLuckModifiers.TryGetValue(itemDropped.ItemQuality, out var qualityLuck))
            luck += qualityLuck;

        if (itemDropped.Template.Group is "Scrolls" or "Health" or "Cures" or "Mana" or "Food" or "Spirits" or "Paper")
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{{=bThe item(s), fumble, and vanished into the altar..");
            return;
        }

        for (var i = 0; i < loop; i++)
        {
            var result = Generator.RandNumGen100();
            result += luck;

            switch (result)
            {
                case >= 95:
                    {
                        var item = CreateItem(client);
                        if (item == null) continue;
                        var receivedItem = GiveItem(client, item);
                        if (receivedItem)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage,
                                "You sense Cail's presence. You feel a sense of calm.");
                            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                                c => c.SendAnimation(83, client.Aisling.Position));
                        }
                    }
                    break;
                case >= 75 and < 95:
                    client.GiveExp(100);
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You feel a warmth placed on your shoulder. (100 Exp)");
                    break;
                case >= 62 and < 75:
                    client.GiveExp(75);
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Thoughts of past achievements fill you with joy. (75 Exp)");
                    break;
                case >= 50 and < 62:
                    client.GiveExp(75);
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "A vision of Spring time and gentle rain overcomes you. (75 Exp)");
                    break;
                case >= 37 and < 50:
                    client.GiveExp(50);
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You briefly hear whispers. What was that? (50 Exp)");
                    break;
                case >= 25 and < 37:
                    client.GiveExp(50);
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "... (50 Exp)");
                    break;
                case >= 12 and < 25:
                    client.GiveExp(25);
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Light fills you. (25 Exp)");
                    break;
                case >= 0 and < 12:
                    {
                        var item = new Item();
                        ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue("Ard Ioc Deum", out var potion);
                        if (potion == null) continue;

                        item = item.Create(client.Aisling, potion);
                        var receivedPotion = GiveItem(client, item);
                        if (receivedPotion)
                        {
                            client.SendServerMessage(ServerMessageType.ActiveMessage,
                                "The feeling of a motherly embrace comes over you.. Glioca?");
                            client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                                c => c.SendAnimation(5, client.Aisling.Position));
                        }
                    }
                    break;
            }
        }

        client.SendAttributes(StatUpdateType.Full);
    }

    private static Item CreateItem(WorldClient client)
    {
        Item item = new();
        var randItem = JoinList(client).RandomIEnum();
        ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(randItem, out var cailItemTemplate);
        return cailItemTemplate == null ? null : item.Create(client.Aisling, cailItemTemplate, NpcShopExtensions.DungeonHighQuality(), ItemQualityVariance.DetermineVariance(), ItemQualityVariance.DetermineWeaponVariance());
    }

    private static List<string> JoinList(WorldClient client)
    {
        var dropList = new List<string>();
        var belt = RewardScript.GenerateDropsCailFountain(client, RewardScript.BeltDrops);
        var shield = RewardScript.GenerateDropsCailFountain(client, RewardScript.ShieldDrops);
        if (belt != null)
            dropList.AddRange(belt);
        if (shield != null)
            dropList.AddRange(shield);
        return dropList;
    }

    private static bool GiveItem(WorldClient client, Item item)
    {
        if (item == null) return false;

        var carry = item.Template.CarryWeight + client.Aisling.CurrentWeight;
        if (carry <= client.Aisling.MaximumWeight)
        {
            ItemQualityVariance.ItemDurability(item, ItemQualityVariance.DetermineQuality());
            var given = item.GiveTo(client.Aisling);
            if (given) return true;
            client.Aisling.BankManager.Items.TryAdd(item.ItemId, item);
            client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue with giving you the item directly, deposited to bank");
            return true;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, "You couldn't hold the item, fumbled, and it vanished into the altar.");
        return false;
    }
}