using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Intro")]
public class Intro : AreaScript
{
    private Sprite _aisling;
    private Item _item;
    private bool _givenClothes;

    public Intro(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime) { }

    public override async void OnMapEnter(GameClient client)
    {
        await Task.Delay(250).ContinueWith(ct =>
        {
            _aisling = client.Aisling;
            var item = new Item();
            item = item.Create(client.Aisling,
                client.Aisling.Gender == Gender.Female
                    ? ServerSetup.Instance.GlobalItemTemplateCache["Blouse"]
                    : ServerSetup.Instance.GlobalItemTemplateCache["Peasant Attire"]);
            _item = new Item
            {
                Template = item.Template,
                ItemId = item.ItemId,
                Slot = 2,
                Image = item.Template.Image,
                DisplayImage = item.Template.DisplayImage,
                Durability = item.Durability,
                Owner = item.Serial,
                ItemQuality = Item.Quality.Common,
                OriginalQuality = Item.Quality.Common,
                ItemVariance = Item.Variance.None,
                WeapVariance = Item.WeaponVariance.None,
                Enchantable = item.Template.Enchantable
            };

            _item.GetDisplayName();
            _item.NoColorGetDisplayName();
            if (client.Aisling.EquipmentManager.Equipment[2] == null)
                client.Aisling.EquipmentManager.Add(_item.Template.EquipmentSlot, _item);
            client.LoadEquipment();
            client.SendStats(StatusFlags.StructA);
            client.UpdateDisplay();
            _givenClothes = true;
        });

        await Task.Delay(750).ContinueWith(ct => {
            _aisling.Client.SendMessage(0x08,
                "{=qWelcome!\n\n{=aThis private server that has been made possible due to countless hours of creation and inspiration.\n\n" +
                "This server falls under Fair Use and accepts zero donations of any kind. With that said, art, music, and the client are property of Nexon Inc.\n\n" +
                "{=bZolian{=a: is a server based on Dungeons & Dragons, Final Fantasy, Diablo 3, Zelda, Elder Scrolls, World of Warcraft and many other MMORPGs. Many " +
                "of the grind mechanics are from traditional Hack and Slashers that you may know and love. Here you'll be able to build a character, and play either with friends or run solo. " +
                "The main focus of Zolian is to balance the characters while breathing new life into Nexon's Darkages. That includes new music, new maps, classes, races, and pvp zones " +
                "much like you would see in open world MMOs.");
        }).ConfigureAwait(false);
    }

    public override void OnMapExit(GameClient client) { }
    public override void OnMapClick(GameClient client, int x, int y) { }

    public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation)
    {
        if (_givenClothes) return;
        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Blouse"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Peasant Attire"]);
        _item = new Item
        {
            Template = item.Template,
            ItemId = item.ItemId,
            Slot = 2,
            Image = item.Template.Image,
            DisplayImage = item.Template.DisplayImage,
            Durability = item.Durability,
            Owner = item.Serial,
            ItemQuality = Item.Quality.Common,
            OriginalQuality = Item.Quality.Common,
            ItemVariance = Item.Variance.None,
            WeapVariance = Item.WeaponVariance.None,
            Enchantable = item.Template.Enchantable
        };

        _item.GetDisplayName();
        _item.NoColorGetDisplayName();
        if (client.Aisling.EquipmentManager.Equipment[2] == null)
            client.Aisling.EquipmentManager.Add(_item.Template.EquipmentSlot, _item);
        client.LoadEquipment();
        client.SendStats(StatusFlags.StructA);
        client.UpdateDisplay();
    }
    public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(GameClient client, string message) { }
}