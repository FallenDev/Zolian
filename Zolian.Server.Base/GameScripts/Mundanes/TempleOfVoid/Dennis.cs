using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.TempleOfLight;

[Script("Dennis")]
public class Dennis(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>
        {
            //new(0x01, "Where am I?"),
            //new(0x02, "The VoidSphere"),
            //new(0x03, "Item Shop"),
            //new(0x04, "Can you repair my items?")
        };

        if (client.Aisling.Path is Class.Cleric || client.Aisling.PastClass is Class.Cleric)
            if (client.Aisling.SkillBook.HasSkill("Blink") && !client.Aisling.HasItem("Cleric's Feather"))
                options.Add(new Dialog.OptionsDataItem(0x06, "Lost my Feather, help!"));
            else if (!client.Aisling.LegendBook.Has("Traversing the Divide (Blink)"))
                options.Add(new Dialog.OptionsDataItem(0x05, "Traversing the Divide"));

        client.SendOptionsDialog(Mundane, "Ah, did the guild send you?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x05:
                var skill = Skill.GiveTo(client.Aisling, "Blink", 1);
                if (skill) client.LoadSkillBook();
                client.GiveItem("Cleric's Feather");
                client.SendOptionsDialog(Mundane, "I can see you're worthy, here is something I created personally.");
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDennis hands you a feather");

                var legend = new Legend.LegendItem
                {
                    Key = "LDennis1",
                    Time = DateTime.UtcNow,
                    Color = LegendColor.PinkRedG5,
                    Icon = (byte)LegendIcon.Priest,
                    Text = "Traversing the Divide (Blink)"
                };

                if (!client.Aisling.LegendBook.Has("Traversing the Divide (Blink)"))
                    client.Aisling.LegendBook.AddLegend(legend, client);

                break;
            case 0x06:
                client.GiveItem("Cleric's Feather");
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cDennis hands you a feather");
                client.SendOptionsDialog(Mundane, "Here you go, try not to lose it again.");
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}