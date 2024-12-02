using Darkages.Common;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Collections.Concurrent;

namespace Darkages.GameScripts.Areas.Mileth;

[Script("Mileth")]
public class Mileth : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];

    private readonly SortedDictionary<int, List<string>> _ceannlaidirWeaponDictionary = new()
    {
        { 7, ["Loures Saber", "Holy Hermes", "Center Shuriken", "Centered Dagger", "Magus Ares"] },
        { 11, ["Claidheamh", "Holy Diana", "Blessed Dagger", "Magus Zeus"] },
        { 22, ["Battle Sword", "Blossom Shuriken", "Moon Dagger", "Ether Wand"] },
        { 33, ["Masquerade", "Razor Claws", "Wood Axe"] },
        { 44, ["Primitive Spear", "Nunchucks", "Scimitar", "Luminous Dagger"] },
        { 55, ["Long Sword", "Luminous Shuriken", "Sun Dagger"] },
        { 66, ["Spiked Club", "Metal Chain", "Lotus Dagger"] },
        { 77, ["Emerald Short Sword", "Sun Shuriken"] },
        { 88, ["Metal Club", "Gladius", "Chain Mace", "Skiv", "Balanced Shuriken"] },
        { 99, ["Stone Axe", "Gold Kindjal", "Blood Bane", "Blood Skiv"] },
        { 120, ["Scythe", "Golden Dragon Buster Blade", "Desert Skiv"] },
        { 150, ["Splish", "Splash"] }
    };

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

    public Mileth(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);
    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) => _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped)
    {
        switch (locationDropped.X)
        {
            case 31 when locationDropped.Y == 52:
            case 31 when locationDropped.Y == 53:
                MilethAltar(client, itemDropped);
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private void MilethAltar(WorldClient client, Item itemDropped)
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

        // Temple Logic
        switch (itemDropped.DisplayName)
        {
            case "Succubus Hair":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("Temple of Light", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }

                    return;
                }
            case "Succibi Hair":
                {
                    foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
                    {
                        if (npc.Value.Scripts is null) continue;
                        if (npc.Value.Scripts.TryGetValue("Temple of Void", out var scriptObj))
                        {
                            scriptObj.OnClick(client, npc.Value.Serial);
                        }
                    }

                    return;
                }
        }

        for (var i = 0; i < loop; i++)
        {
            var result = Generator.RandNumGen100();
            result += luck;

            switch (result)
            {
                case >= 95:
                    var weapon = CreateItem(client);
                    if (weapon == null) continue;
                    var receivedWeapon = GiveItem(client, weapon);
                    if (receivedWeapon)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "You hear Ceannlaidir's voice if but for a moment.");
                        client.Aisling.SendAnimationNearby(83, client.Aisling.Position);
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
                    var item = new Item();
                    ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue("Ard Ioc Deum", out var potion);
                    if (potion == null) continue;

                    item = item.Create(client.Aisling, potion);
                    var receivedPotion = GiveItem(client, item);
                    if (receivedPotion)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "The feeling of a motherly embrace comes over you.. Glioca?");
                        client.Aisling.SendAnimationNearby(5, client.Aisling.Position);
                    }
                    break;
            }
        }

        client.SendAttributes(StatUpdateType.Full);
    }

    private Item CreateItem(WorldClient client)
    {
        Item item = new();
        var weapons = new List<string> { "Stick" };
        foreach (var kvp in _ceannlaidirWeaponDictionary.Where(kvp => client.Aisling.Level >= kvp.Key))
        {
            weapons.AddRange(kvp.Value);
        }

        var weapon = weapons.RandomIEnum();
        ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(weapon, out var ceanWeapon);
        return ceanWeapon == null ? null : item.Create(client.Aisling, ceanWeapon, NpcShopExtensions.DungeonHighQuality(), ItemQualityVariance.DetermineVariance(), ItemQualityVariance.DetermineWeaponVariance());
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