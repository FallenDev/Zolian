using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Pete")]
public class Pete(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
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
            ? "What to do.. Oh, what to do?"
            : "Thank you friend, please enjoy some mead on us in the tavern!", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        var exp = Random.Shared.Next(1000, 5000);

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
                    killCount += 1;
                    client.Aisling.QuestManager.PeteKill = killCount;
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Player Coordinates are in the bottom center-left corner");
                    client.SendOptionsDialog(Mundane, $"Please cull {killCount} mice anywhere in the crypt. {{=cCoords{{=a: {{=q89{{=a,{{=q 52");
                    Task.Delay(5000).ContinueWith(ct =>
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cShift + f {{=ato see system and important messages");
                    });
                    Task.Delay(10000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                    Task.Delay(15000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, "Player Coordinates are in the bottom center-left corner"); });
                    Task.Delay(20000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                    Task.Delay(25000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cShift + f {{=ato see system and important messages"); });
                    Task.Delay(30000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
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
                        client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {exp} experience.");
                        client.SendAttributes(StatUpdateType.ExpGold);

                        var legend = new Legend.LegendItem
                        {
                            Key = "LPete1",
                            IsPublic = true,
                            Time = DateTime.UtcNow,
                            Color = LegendColor.PaleSkinToTanSkinG8,
                            Icon = (byte)LegendIcon.Heart,
                            Text = "Saved Mileth's Mead"
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
                            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cShift + f {{=ato see system and important messages");
                        });
                        Task.Delay(10000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                        Task.Delay(15000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, "Player Coordinates are in the bottom center-left corner"); });
                        Task.Delay(20000).ContinueWith(ct => { client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cCoords{{=a: {{=q89{{=a,{{=q 52"); });
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "Thank you friend, please enjoy some mead on us in the tavern!");
                    break;
                }
        }
    }
}