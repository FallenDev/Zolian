using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;

using Gender = Darkages.Enums.Gender;

namespace Darkages.GameScripts.Areas.Tutorial;

[Script("Intro")]
public class Intro : AreaScript
{
    private readonly ConcurrentDictionary<long, Aisling> _playersOnMap = [];

    public Intro(Area area) : base(area) => Area = area;

    public override void Update(TimeSpan elapsedTime) { }

    public override async void OnMapEnter(WorldClient client)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        await Task.Delay(250).ContinueWith(ct =>
        {
            var item = new Item();
            item = item.Create(client.Aisling,
                client.Aisling.Gender == Gender.Female
                    ? ServerSetup.Instance.GlobalItemTemplateCache["Blouse"]
                    : ServerSetup.Instance.GlobalItemTemplateCache["Peasant Attire"]);
            item.GetDisplayName();
            item.NoColorGetDisplayName();
            if (client.Aisling.EquipmentManager.Equipment[2] == null)
                client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);
        });

        await Task.Delay(350).ContinueWith(ct =>
        {
            client.SendServerMessage(ServerMessageType.ScrollWindow,
                "{=qWelcome!\n\n{=aThis private server that has been made possible due to countless hours of creation and inspiration.\n\n" +
                "This server falls under Fair Use and accepts zero donations of any kind. With that said, art, music, and the client are property of Nexon Inc.\n\n" +
                "{=bZolian{=a: is a server based on Dungeons & Dragons, Final Fantasy, Diablo 3, Zelda, Elder Scrolls, World of Warcraft and many other MMORPGs. Many " +
                "of the grind mechanics are from traditional Hack and Slashers that you may know and love. Here you'll be able to build a character, and play either with friends or run solo. " +
                "The main focus of Zolian is to balance the characters while breathing new life into Nexon's Darkages. That includes new music, new maps, classes, races, and pvp zones " +
                "much like you would see in open world MMOs.");
        });

        await Task.Delay(6000).ContinueWith(ct =>
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aYou cannot die in the tutorial, use {{=qSpacebar {{=ato attack.");
        });

        await Task.Delay(6000).ContinueWith(ct =>
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aYou can see these messages by combo pressing {{=qshift+f{{=a.");
        });

        await Task.Delay(6000).ContinueWith(ct =>
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qF1{{=a shows advanced stats, while {{=qF4{{=a shows settings.");
        });

        await Task.Delay(6000).ContinueWith(ct =>
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aOpen your inventory by pressing {{=qa{{=a; Check out the Guide!");
        }).ConfigureAwait(false);
    }

    public override void OnMapExit(WorldClient client) => _playersOnMap.TryRemove(client.Aisling.Serial, out _);

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        _playersOnMap.TryAdd(client.Aisling.Serial, client.Aisling);

        var item = new Item();
        item = item.Create(client.Aisling,
            client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Blouse"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Peasant Attire"]);
        item.GetDisplayName();
        item.NoColorGetDisplayName();
        if (client.Aisling.EquipmentManager.Equipment[2] == null)
            client.Aisling.EquipmentManager.Add(item.Template.EquipmentSlot, item);
    }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }
}