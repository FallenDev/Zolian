using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas
{
    [Script("Mileth")]
    public class Mileth : AreaScript
    {
        public Mileth(Area area) : base(area) => Area = area;
        public override void Update(TimeSpan elapsedTime) { }
        public override void OnMapEnter(GameClient client) { }
        public override void OnMapExit(GameClient client) { }
        public override void OnMapClick(GameClient client, int x, int y) { }
        public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation) { }

        public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped)
        {
            switch (locationDropped.X)
            {
                case 31 when locationDropped.Y == 52:
                case 31 when locationDropped.Y == 53:
                    MilethAltar(client, itemDropped, locationDropped);
                    break;
            }
        }

        public override void OnGossip(GameClient client, string message) { }

        private void MilethAltar(GameClient client, Item itemDropped, Position locationDropped)
        {
            var loop = itemDropped.Dropping;
            var luck = 0 + client.Aisling.Luck;

            if (loop == 0) loop = 1;

            switch (itemDropped.ItemQuality)
            {
                case Item.Quality.Damaged:
                    client.SendMessage(0x02, "The altar consumes the damaged item, nothing happens.");
                    itemDropped.Remove();
                    return;
                case Item.Quality.Common:
                    luck += 0;
                    break;
                case Item.Quality.Uncommon:
                    luck += 1;
                    break;
                case Item.Quality.Rare:
                    luck += 3;
                    break;
                case Item.Quality.Epic:
                    luck += 5;
                    break;
                case Item.Quality.Legendary:
                    luck += 50;
                    break;
                case Item.Quality.Forsaken:
                    luck += 75;
                    break;
                case Item.Quality.Mythic:
                    luck += 99;
                    break;
                default:
                    luck += 0;
                    break;
            }

            switch (itemDropped.DisplayName)
            {
                case "Mead":
                    client.SendMessage(0x02, "The mead disappears, nothing happens.");
                    return;
                case "Succubus Hair":
                    {
                        const string script = "Hallowed Voice";
                        var scriptObj = ServerSetup.Instance.GlobalMundaneScriptCache.FirstOrDefault(i => i.Key == script);
                        scriptObj.Value?.OnClick(client.Aisling.Client.Server, client.Aisling.Client);
                        return;
                    }
            }

            var weapon = client.Aisling.Level switch
            {
                >= 7 and <= 10 => "Loures Saber",
                >= 11 and <= 21 => "Broad Sword",
                >= 22 and <= 32 => "Templar",
                >= 33 and <= 43 => "Bramble",
                >= 44 and <= 54 => "Scimitar",
                >= 55 and <= 65 => "Stilla",
                >= 66 and <= 76 => "Talgonite Axe",
                >= 77 and <= 87 => "Chain Mace",
                >= 88 and <= 98 => "Kindjal",
                >= 99 => "Stone Axe",
                _ => "Stick"
            };

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
                        item = item.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache[weapon], ShopMethods.DungeonHighQuality(), variance, wVariance);
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
                        item = item.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Ard Ioc Deum"]);
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
                            Time = DateTime.Now,
                            Color = LegendColor.Red,
                            Icon = (byte)LegendIcon.Victory,
                            Value = $"Relic Finder: {client.Aisling.RelicFinder++}"
                        };

                        client.Aisling.LegendBook.AddLegend(legend, client);
                    }
                }
                else
                    client.SendMessage(0x03, "You couldn't hold the item, fumbled, and it vanished into the altar.");

                client.SendStats(StatusFlags.StructA);
            }
        }

        private static void ItemDura(Item item, Item.Quality quality, IGameClient client)
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
}