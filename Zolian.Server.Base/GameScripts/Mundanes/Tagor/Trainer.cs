﻿using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

using System.Numerics;
using Darkages.Common;
using Darkages.Enums;

namespace Darkages.GameScripts.Mundanes.Tagor;

[Script("Training Center")]
public class Trainer : MundaneScript
{
    private List<Skill> _skillList;
    private List<Spell> _spellList;
    private readonly List<Vector2> _dojoSpots;

    public Trainer(GameServer server, Mundane mundane) : base(server, mundane)
    {
        _dojoSpots = new List<Vector2>
        {
            new(12, 22),
            new(13, 21),
            new(14, 22),
            new(13, 23),
            new(12, 13),
            new(13, 12),
            new(14, 13),
            new(13, 14),
            new(22, 14),
            new(21, 13),
            new(23, 13),
            new(22, 13),
            new(22, 21),
            new(23, 22),
            new(22, 23),
            new(21, 22)
        };
    }

    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        _skillList = ObtainSkillList(client);
        _spellList = ObtainSpellList(client);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var options = new List<OptionsDataItem>();

        if (_skillList.Count > 0)
        {
            options.Add(new(0x01, "Advance Skills"));
        }

        if (_spellList.Count > 0)
        {
            //options.Add(new(0x0010, "Advance Spells"));
        }

        options.Add(new(0x02, "Forget Skill"));
        options.Add(new(0x0011, "Forget Spell"));

        client.SendOptionsDialog(Mundane, "Here to train? Let's get started.", options.ToArray());
    }

    public override async void OnResponse(GameClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            #region Skills

            case 0x00:
                client.SendMessage(0x02, "Come back sometime!");
                client.CloseDialog();
                break;
            case 0x0001:
                {
                    var options = new List<OptionsDataItem>();

                    if (_skillList.Count > 0)
                    {
                        options.Add(new(0x03, "Let's get started"));
                        options.Add(new OptionsDataItem(0x00, "{=bNo, thank you"));
                        client.SendOptionsDialog(Mundane, "Ready? It'll cost you 100,000 gold for unlimited time.", options.ToArray());
                    }
                    else
                    {
                        client.CloseDialog();
                        client.SendMessage(0x02, "Ah, come back anytime.");
                    }

                    break;
                }
            case 0x0002:
                {
                    client.SendSkillForgetDialog(Mundane,
                        "Muscle memory is a hard thing to unlearn. \nYou may come back to relearn what the mind has lost but the muscle still remembers.", 0x9000);
                    break;
                }
            case 0x9000:
                {
                    int.TryParse(args, out var idx);

                    if (idx is < 0 or > byte.MaxValue)
                    {
                        client.SendMessage(0x02, "You don't quite have that skill.");
                        client.CloseDialog();
                    }

                    client.Aisling.SkillBook.Remove((byte)idx, true);
                    client.Send(new ServerFormat2D((byte)idx));
                    client.LoadSkillBook();

                    client.SendSkillForgetDialog(Mundane, "Your body is still, breathing in, relaxed. \nAny other skills you wish to forget?", 0x9000);
                    break;
                }
            case 0x0003:
                {
                    if (client.Aisling.GoldPoints >= 100000)
                    {
                        client.Aisling.GoldPoints -= 100000;
                        client.TransitionToMap(5257, new Position(17, 17));
                        await Task.Delay(100).ContinueWith(ct =>
                        {
                            client.CloseDialog();
                            var spot = _dojoSpots.RandomIEnum();
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


                        });

                        var monster = client.Aisling.MonstersNearby().FirstOrDefault(i => i.WithinRangeOf(client.Aisling, 2));
                        client.Aisling.Target = monster;

                        while (client.Aisling.NextTo(client.Aisling.Target!.X, client.Aisling.Target!.Y))
                        {
                            await Task.Delay(500).ContinueWith(ct =>
                            {
                                foreach (var skill in client.Aisling.SkillBook.Skills.Values)
                                {
                                    if (skill is null) continue;
                                    if (!skill.CanUse()) continue;
                                    if (skill.Scripts is null || skill.Scripts.IsEmpty) continue;

                                    skill.InUse = true;

                                    var script = skill.Scripts.Values.First();
                                    script?.OnUse(client.Aisling);

                                    skill.InUse = false;
                                    skill.CurrentCooldown = skill.Template.Cooldown;
                                }

                                GameServer.Assail(client);
                            });
                        }
                    }
                    else
                    {
                        client.SendOptionsDialog(Mundane, "Looks like you don't have enough, come back when you do. (100,000 gold)");
                    }
                    break;
                }
            case 0x0004:
                {
                    // add logic to warp to a training dummy, then start improving the skill they've picked

                    break;
                }

            #endregion

            #region Spells

            case 0x0011:
                {
                    client.SendSpellForgetDialog(Mundane, "The mind is a complex place, sometimes we need to declutter. \nBe warned, This cannot be undone.", 0x0800);
                    break;
                }
            case 0x0800:
                {
                    int.TryParse(args, out var idx);

                    if (idx is < 0 or > byte.MaxValue)
                    {
                        client.SendMessage(0x02, "I do not sense this spell within you any longer.");
                        client.CloseDialog();
                    }

                    client.Aisling.SpellBook.Remove((byte)idx, true);
                    client.Send(new ServerFormat18((byte)idx));
                    client.LoadSpellBook();

                    client.SendSpellForgetDialog(Mundane, "It is gone, Shall we cleanse more?\nRemember, This cannot be undone.", 0x0800);
                    break;
                }

                #endregion
        }
    }

    private static List<Skill> ObtainSkillList(IGameClient client)
    {
        return client.Aisling.SkillBook.GetSkills(s => s != null && s.Slot != 0).ToList();
    }

    private static List<Spell> ObtainSpellList(IGameClient client)
    {
        return client.Aisling.SpellBook.GetSpells(s => s != null && s.Slot != 0).ToList();
    }
}