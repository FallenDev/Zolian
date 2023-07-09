using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Mileth")]
public class Mileth : AreaScript
{
    private readonly SortedDictionary<int, List<string>> _ceannlaidirWeaponDictionary = new()
    {
        { 7, new List<string> { "Loures Saber", "Holy Hermes", "Center Shuriken", "Centered Dagger", "Magus Ares" } },
        { 11, new List<string> { "Claidheamh", "Holy Diana", "Blessed Dagger", "Magus Zeus" } },
        { 22, new List<string> { "Battle Sword", "Blossom Shuriken", "Moon Dagger", "Ether Wand" } },
        { 33, new List<string> { "Masquerade", "Razor Claws", "Wood Axe" } },
        { 44, new List<string> { "Primitive Spear", "Nunchucks", "Scimitar", "Luminous Dagger" } },
        { 55, new List<string> { "Long Sword", "Luminous Shuriken", "Sun Dagger" } },
        { 66, new List<string> { "Spiked Club", "Metal Chain", "Lotus Dagger" } },
        { 77, new List<string> { "Emerald", "Sun Shuriken" } },
        { 88, new List<string> { "Metal Club", "Gladius", "Chain Mace", "Skiv", "Balanced Shuriken" } },
        { 99, new List<string> { "Stone Axe", "Gold Kindjal", "Blood Bane", "Blood Skiv" } },
        { 120, new List<string> { "Scythe", "Golden Dragon Buster Blade", "Desert Skiv" } },
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
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }
    public override void OnMapClick(WorldClient client, int x, int y) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped)
    {
        switch (locationDropped.X)
        {
            case 31 when locationDropped.Y == 52:
            case 31 when locationDropped.Y == 53:
                MilethAltar(client, itemDropped, locationDropped);
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private void MilethAltar(WorldClient client, Item itemDropped, Position locationDropped)
    {
        var loop = itemDropped.Dropping;
        var luck = 0 + client.Aisling.Luck;

        if (loop == 0) loop = 1;

        if (_qualityLuckModifiers.TryGetValue(itemDropped.ItemQuality, out var qualityLuck))
        {
            luck += qualityLuck;
        }

        switch (itemDropped.DisplayName)
        {
            case "Mead":
                client.SendMessage(0x02, "The mead disappears, nothing happens.");
                return;
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

        var weapons = new List<string> { "Stick" };
        foreach (var kvp in _ceannlaidirWeaponDictionary.Where(kvp => client.Aisling.Level >= kvp.Key))
        {
            weapons.AddRange(kvp.Value);
        }
        var weapon = weapons[Random.Shared.Next(weapons.Count)];

        for (var i = 0; i < loop; i++)
        {
            var quality = ItemQualityVariance.DetermineQuality();
            var variance = ItemQualityVariance.DetermineVariance();
            var wVariance = ItemQualityVariance.DetermineWeaponVariance();
            Item item = null;
            var result = Generator.RandNumGen100();
            result += luck;

            switch (result)
            {
                case >= 95:
                    item = new Item();
                    client.SendMessage(0x02, "You hear Ceannlaidir's voice as a weapon manifests before you.");
                    ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(weapon, out var ceanWeapon);
                    if (ceanWeapon != null)
                        item = item.Create(client.Aisling, ceanWeapon, ShopMethods.DungeonHighQuality(), variance, wVariance);
                    Task.Delay(350).ContinueWith(ct => { client.Aisling.Animate(83); });
                    break;
                case >= 75 and < 95:
                    client.SendMessage(0x02, "You feel a warmth placed on your shoulder. (100 Exp)")
                        .GiveExp(100);
                    client.SendStats(StatusFlags.StructC);
                    break;
                case >= 62 and < 75:
                    client.SendMessage(0x02, "Thoughts of past achievements fill you with joy. (75 Exp)")
                        .GiveExp(75);
                    client.SendStats(StatusFlags.StructC);
                    break;
                case >= 50 and < 62:
                    client.SendMessage(0x02, "A vision of Spring time and gentle rain overcomes you. (75 Exp)")
                        .GiveExp(75);
                    client.SendStats(StatusFlags.StructC);
                    break;
                case >= 37 and < 50:
                    client.SendMessage(0x02, "You briefly hear whispers. What was that? (50 Exp)")
                        .GiveExp(50);
                    client.SendStats(StatusFlags.StructC);
                    break;
                case >= 25 and < 37:
                    client.SendMessage(0x02, "... (50 Exp)")
                        .GiveExp(50);
                    client.SendStats(StatusFlags.StructC);
                    break;
                case >= 12 and < 25:
                    client.SendMessage(0x02, "Light fills you. (25 Exp)")
                        .GiveExp(25);
                    client.SendStats(StatusFlags.StructC);
                    break;
                case >= 0 and < 12:
                    item = new Item();
                    client.SendMessage(0x02, "Glioca manifests before you, then quickly tucks a potion in your bag.");
                    ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue("Ard Ioc Deum", out var potion);
                    if (potion != null)
                        item = item.Create(client.Aisling, potion);
                    Task.Delay(350).ContinueWith(ct => { client.Aisling.Animate(5); });
                    break;
            }

            if (item == null) continue;

            var carry = item.Template.CarryWeight + client.Aisling.CurrentWeight;
            if (carry <= client.Aisling.MaximumWeight)
            {
                ItemDura(item, quality, client);
                item.GiveTo(client.Aisling);

                if (item is { OriginalQuality: Item.Quality.Forsaken })
                {
                    var marks = client.Aisling.LegendBook.LegendMarks.ToArray();

                    foreach (var mark in marks)
                    {
                        if (mark.Value.StringContains("Relic Finder"))
                        {
                            client.Aisling.LegendBook.Remove(mark, client);
                        }
                    }

                    var legend = new Legend.LegendItem
                    {
                        Category = "Relic Finder",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.Red,
                        Icon = (byte)LegendIcon.Victory,
                        Value = $"Relic Finder: {client.Aisling.RelicFinder++}"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
            }
            else
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You couldn't hold the item, fumbled, and it vanished into the altar.");

            client.SendStats(StatusFlags.StructA);
        }
    }

    private static void ItemDura(Item item, Item.Quality quality, WorldClient client)
    {
        var temp = item.Template.MaxDurability;
        switch (quality)
        {
            case Item.Quality.Damaged:
                item.MaxDurability = (uint)(temp / 1.4);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Common:
                item.MaxDurability = temp / 1;
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Uncommon:
                item.MaxDurability = (uint)(temp / 0.9);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Rare:
                item.MaxDurability = (uint)(temp / 0.8);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Epic:
                item.MaxDurability = (uint)(temp / 0.7);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Legendary:
                item.MaxDurability = (uint)(temp / 0.6);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Forsaken:
                item.MaxDurability = (uint)(temp / 0.5);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Mythic:
                item.MaxDurability = (uint)(temp / 0.3);
                item.Durability = item.MaxDurability;
                break;
        }
    }
}