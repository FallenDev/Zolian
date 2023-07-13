using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Pete")]
public class Pete : MundaneScript
{
    public Pete(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (!client.Aisling.QuestManager.PeteComplete && client.Aisling.QuestManager.PeteKill == 0)
        {
            options.Add(new(0x01, "What's wrong?"));
        }

        if (!client.Aisling.QuestManager.PeteComplete && client.Aisling.QuestManager.PeteKill >= 1)
            options.Add(new(0x04, $"{{=cI took care of them{{=a."));

        client.SendOptionsDialog(Mundane, !client.Aisling.QuestManager.PeteComplete
            ? "What to do.. Oh what to do?"
            : "Thank you friend, please enjoy some mead on us in the tavern!", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var exp = (uint)Random.Shared.Next(1000, 5000);

        switch (responseID)
        {
            case 0x01:
            {
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x03, "Sure."),
                    new (0x02, "Not right now.")
                };

                client.SendOptionsDialog(Mundane, "We're having an issue in town with mice. They're getting into our mead, causing it to sour. Would you mind taking care of them for us?", options.ToArray());
                break;
            }
            case 0x02:
            {
                client.CloseDialog();
                break;
            }
            case 0x03:
            {
                var killCount = Generator.RandNumGen10();
                client.Aisling.QuestManager.PeteKill = killCount;
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Player Coordinates are in the bottom center-left corner");
                client.SendOptionsDialog(Mundane, $"Please cull {killCount} mice anywhere in the crypt. {{=cCoords{{=a: {{=q89{{=a,{{=q 52");
                Task.Delay(5000).ContinueWith(ct =>
                {
                    client.CloseDialog();
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cShift + f {{=ato see system and important messages");
                });
                Task.Delay(10000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                Task.Delay(15000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Player Coordinates are in the bottom center-left corner"); });
                Task.Delay(20000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                Task.Delay(25000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cShift + f {{=ato see system and important messages"); });
                Task.Delay(30000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                break;
            }
            case 0x04:
            {
                var options = new List<Dialog.OptionsDataItem>();

                if (client.Aisling.HasKilled("Mouse", client.Aisling.QuestManager.PeteKill))
                {
                    client.Aisling.QuestManager.PeteComplete = true;
                    client.GiveExp(exp);
                    client.Aisling.QuestManager.MilethReputation += 1;
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
                    client.SendStats(StatusFlags.StructC);

                    var legend = new Legend.LegendItem
                    {
                        Category = "Adventure",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.Brass,
                        Icon = (byte)LegendIcon.Heart,
                        Value = "Saved Mileth's Mead"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                else
                {
                    options.Add(new(0x02, "Sorry, I'll head back."));
                    client.SendOptionsDialog(Mundane, "These darn mice... Are you sure you got them?.", options.ToArray());
                    Task.Delay(5000).ContinueWith(ct =>
                    {
                        client.CloseDialog();
                        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cShift + f {{=ato see system and important messages");
                    });
                    Task.Delay(10000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                    Task.Delay(15000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Player Coordinates are in the bottom center-left corner"); });
                    Task.Delay(20000).ContinueWith(ct => { aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                    break;
                }

                client.SendOptionsDialog(Mundane, "Thank you friend, please enjoy some mead on us in the tavern!");
                break;
            }
        }
    }
}