using Darkages.Common;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.CthonicRemains;

[Script("Guild XP Boost")]
public class Carn(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.QuestManager.AdventuresGuildReputation >= 1)
            options.Add(new Dialog.OptionsDataItem(0x00, "x2 Experience Boost"));

        if (client.Aisling.QuestManager.AdventuresGuildReputation >= 3)
            options.Add(new Dialog.OptionsDataItem(0x01, "Dia Hastega"));

        if (client.Aisling.QuestManager.AdventuresGuildReputation >= 5)
            options.Add(new Dialog.OptionsDataItem(0x02, "x3 Experience Boost"));

        client.SendOptionsDialog(Mundane, "*yawns* Hey! Welcome friend, I'm tasked with boosting the properties monsters give towards adventurer's progression.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                {
                    var options = new List<Dialog.OptionsDataItem> { new(0x10, "Continue") };
                    client.SendOptionsDialog(Mundane, "It will cost 100,000,000 gold coins in guild resources, continue?\n" +
                                                      "This spell will be cast on you and those grouped with you.\n" +
                                                      "Please make sure they're nearby.", options.ToArray());
                }
                break;
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem> { new(0x20, "Continue") };
                    client.SendOptionsDialog(Mundane, "It will cost 150,000,000 gold coins in guild resources, continue?\n" +
                                                      "This spell will be cast on you and those grouped with you.\n" +
                                                      "Please make sure they're nearby.", options.ToArray());
                }
                break;
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem> { new(0x30, "Continue") };
                    client.SendOptionsDialog(Mundane, "It will cost 250,000,000 gold coins in guild resources, continue?\n" +
                                                      "This spell will be cast on you and those grouped with you.\n" +
                                                      "Please make sure they're nearby.", options.ToArray());
                }
                break;
            case 0x10:
                {
                    if (client.Aisling.GoldPoints >= 100000000)
                    {
                        client.Aisling.GoldPoints -= 100000000;
                        client.SendAttributes(StatUpdateType.ExpGold);

                        if (client.Aisling.GroupId != 0 && client.Aisling.GroupParty != null)
                        {
                            foreach (var player in client.Aisling.GroupParty.PartyMembers.Values.Where(player => player.Map.ID == 5031))
                            {
                                var buff = new BuffDoubleExperience();
                                buff.OnApplied(player, buff);
                            }
                        }
                        else
                        {
                            var buff = new BuffDoubleExperience();
                            buff.OnApplied(client.Aisling, buff);
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you may be a little short on gold.");
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "Of course, *holds his hand up*; There you go! For the Guild!");
                }
                break;
            case 0x20:
                {
                    if (client.Aisling.GoldPoints >= 150000000)
                    {
                        client.Aisling.GoldPoints -= 150000000;
                        client.SendAttributes(StatUpdateType.ExpGold);

                        if (client.Aisling.GroupId != 0 && client.Aisling.GroupParty != null)
                        {
                            foreach (var player in client.Aisling.GroupParty.PartyMembers.Values.Where(player => player.Map.ID == 5031))
                            {
                                var buff = new buff_Dia_Haste();
                                buff.OnApplied(player, buff);
                            }
                        }
                        else
                        {
                            var buff = new buff_Dia_Haste();
                            buff.OnApplied(client.Aisling, buff);
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you may be a little short on gold.");
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "Of course, *holds his hand up*; There you go! For the Guild!");
                }
                break;
            case 0x30:
                {
                    if (client.Aisling.GoldPoints >= 250000000)
                    {
                        client.Aisling.GoldPoints -= 250000000;
                        client.SendAttributes(StatUpdateType.ExpGold);

                        if (client.Aisling.GroupId != 0 && client.Aisling.GroupParty != null)
                        {
                            foreach (var player in client.Aisling.GroupParty.PartyMembers.Values.Where(player => player.Map.ID == 5031))
                            {
                                var buff = new BuffTripleExperience();
                                buff.OnApplied(player, buff);
                            }
                        }
                        else
                        {
                            var buff = new BuffTripleExperience();
                            buff.OnApplied(client.Aisling, buff);
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you may be a little short on gold.");
                        return;
                    }

                    client.SendOptionsDialog(Mundane, "Of course, *holds his hand up*; There you go! For the Guild!");
                }
                break;

        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}