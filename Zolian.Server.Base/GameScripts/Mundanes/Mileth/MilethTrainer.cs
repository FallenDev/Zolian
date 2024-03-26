using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Numerics;
using Darkages.Templates;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Mileth Training")]
public class MilethTrainer(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    private List<Skill> _skillList;
    private List<Spell> _spellList;
    private string _tempSpellName;
    private readonly List<Vector2> _dojoMeleeSpots =
    [
        new(12, 22),
        new(13, 21),
        new(14, 22),
        new(13, 23),

        // Far Top
        new(12, 13),
        new(13, 12),
        new(14, 13),
        new(13, 14),

        // Far Right
        new(22, 14),
        new(21, 13),
        new(23, 13),
        new(22, 12),

        // Far Bottom
        new(22, 21),
        new(23, 22),
        new(22, 23),
        new(21, 22),

        // Top
        new(15, 16),
        new(16, 15),
        new(17, 16),
        new(16, 17),

        // Bottom
        new(18, 19),
        new(19, 18),
        new(20, 19),
        new(19, 20),

        // Left
        new(15, 19),
        new(16, 18),
        new(17, 19),
        new(16, 20),

        // Right
        new(18, 16),
        new(19, 15),
        new(20, 16),
        new(19, 17)

    ];
    private readonly List<Vector2> _dojoCasterSpots =
    [
        // Top
        new(14, 10),
        new(15, 10),
        new(16, 10),
        new(17, 10),
        new(18, 10),
        new(19, 10),
        new(20, 10),
        new(21, 10),

        // Bottom
        new(14, 25),
        new(15, 25),
        new(16, 25),
        new(17, 25),
        new(18, 25),
        new(19, 25),
        new(20, 25),
        new(21, 25),

        // Left
        new(10, 21),
        new(10, 20),
        new(10, 19),
        new(10, 17),
        new(10, 16),
        new(10, 15),
        new(10, 14),

        // Right
        new(25, 14),
        new(25, 15),
        new(25, 16),
        new(25, 17),
        new(25, 18),
        new(25, 19),
        new(25, 20),
        new(25, 21)
    ];

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        _skillList = ObtainSkillList(client);
        _spellList = ObtainSpellList(client);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        _tempSpellName = "";
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (_skillList.Count > 0)
        {
            options.Add(new(0x01, "Train Skills"));
        }

        if (_spellList.Count > 0)
        {
            options.Add(new(0x02, "Train Spells"));
        }

        client.SendOptionsDialog(Mundane, "Here to train? Let's get started.", options.ToArray());
    }

    public override async void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x00:
                _tempSpellName = "";
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Come back sometime!");
                client.CloseDialog();
                break;
            // Skills
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>();

                    if (_skillList.Count > 0)
                    {
                        options.Add(new(0x03, "Yes"));
                        options.Add(new Dialog.OptionsDataItem(0x00, "{=bNo, thank you"));
                        client.SendOptionsDialog(Mundane, "Ready? It'll cost you 100,000 gold for unlimited time.", options.ToArray());
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "Ah, come back anytime.");
                    }

                    break;
                }
            case 0x03:
                {
                    if (client.Aisling.GoldPoints >= 100000)
                    {
                        client.Aisling.GoldPoints -= 100000;
                        client.TransitionToMap(5269, new Position(17, 17));
                        await Task.Delay(100).ContinueWith(ct =>
                        {
                            client.CloseDialog();
                            var spot = _dojoMeleeSpots.RandomIEnum();
                            client.Aisling.Pos = spot;
                            client.ClientRefreshed();
                            var monsters = client.Aisling.MonstersNearby()
                                .Where(i => i.WithinRangeOf(client.Aisling, 2));

                            foreach (var monster in monsters)
                            {
                                client.Aisling.Facing(monster.X, monster.Y, out var direction);
                                if (!client.Aisling.Position.IsNextTo(monster.Position)) return;
                                client.Aisling.Direction = (byte)direction;
                                client.Aisling.Turn();
                            }

                            client.Aisling.AutoRoutine();
                        });
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you don't have enough, come back when you do. (100,000 gold)");
                    }
                    break;
                }
            // Spells
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem>();

                    if (_spellList.Count > 0)
                    {
                        options.Add(new(0x04, "Yes"));
                        options.Add(new Dialog.OptionsDataItem(0x00, "{=bNo, thank you"));
                        client.SendOptionsDialog(Mundane, "Ready? It'll cost you 200,000 gold for unlimited time.", options.ToArray());
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "Ah, come back anytime.");
                    }
                    break;
                }
            case 0x04:
                client.SendSpellLearnDialog(Mundane, "Which spell would you like to train? If you're capable, you can train up to three at a time.", 0x20, ObtainSpellTemplateList(client));
                break;
            case 0x05:
                {
                    if (client.Aisling.GoldPoints >= 200000)
                    {
                        client.Aisling.GoldPoints -= 200000;
                        client.TransitionToMap(5269, new Position(17, 17));
                        await Task.Delay(100).ContinueWith(ct =>
                        {
                            client.CloseDialog();
                            var spot = _dojoCasterSpots.RandomIEnum();
                            client.Aisling.Pos = spot;
                            client.ClientRefreshed();
                            client.Aisling.AutoCastRoutine();
                        });
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you don't have enough, come back when you do. (200,000 gold)");
                    }
                    break;
                }
            case 0x20:
                {
                    var foundTemplate = ServerSetup.Instance.GlobalSpellTemplateCache.TryGetValue(args, out var spellTemplate);

                    if (foundTemplate)
                    {
                        var options = new List<Dialog.OptionsDataItem>
                        {
                            new(0x22, $"{client.Aisling.SpellTrainOne ?? "Spell 1"}"),
                            new(0x23, $"{client.Aisling.SpellTrainTwo ?? "Spell 2"}"),
                            new(0x24, $"{client.Aisling.SpellTrainThree ?? "Spell 3"}"),
                            new(0x05, "Let's Train")
                        };

                        _tempSpellName = spellTemplate.Name;

                        client.SendOptionsDialog(Mundane, "The order they're set in, is the order they'll execute.", options.ToArray());
                        break;
                    }

                    client.SendOptionsDialog(Mundane, "Hmm, let's try that again.");
                    break;
                }
            case 0x22:
                {
                    client.Aisling.SpellTrainOne = _tempSpellName;
                    client.SendSpellLearnDialog(Mundane, "Would you like to train another? If you're capable, you can train up to three at a time.", 0x20, ObtainSpellTemplateList(client));
                    break;
                }
            case 0x23:
                {
                    client.Aisling.SpellTrainTwo = _tempSpellName;
                    client.SendSpellLearnDialog(Mundane, "Would you like to train another? If you're capable, you can train up to three at a time.", 0x20, ObtainSpellTemplateList(client));
                    break;
                }
            case 0x24:
                {
                    client.Aisling.SpellTrainThree = _tempSpellName;
                    client.SendSpellLearnDialog(Mundane, "Would you like to train another? If you're capable, you can train up to three at a time.", 0x20, ObtainSpellTemplateList(client));
                    break;
                }
        }
    }

    private static List<Skill> ObtainSkillList(WorldClient client)
    {
        return client.Aisling.SkillBook.GetSkills(s => s != null && s.Slot != 0).ToList();
    }

    private static List<Spell> ObtainSpellList(WorldClient client)
    {
        return client.Aisling.SpellBook.TryGetSpells(s => s != null && s.Slot != 0).ToList();
    }

    private static List<SpellTemplate> ObtainSpellTemplateList(WorldClient client)
    {
        return client.Aisling.SpellBook.TryGetSpells(s => s != null && s.Slot != 0).Select(i => i.Template).ToList();
    }
}