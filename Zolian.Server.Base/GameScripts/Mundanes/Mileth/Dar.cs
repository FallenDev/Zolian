using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;
using ServiceStack;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Dar")]
public class Dar : MundaneScript
{
    private string _retrieve;
    private string _retrieveAdv;
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;
    private bool _0X0A;
    private bool _0X0B;
    private bool _0X0C;
    private bool _0X0D;
    private bool _0X0E;
    private bool _0X0F;

    public Dar(GameServer server, Mundane mundane) : base(server, mundane)
    {
        _skillList = ObtainSkillList();
        _spellList = ObtainSpellList();
    }
    
    public override void OnClick(GameClient client, int serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(IGameClient client)
    {
        base.TopMenu(client);

        var options = new List<OptionsDataItem>();

        if (!client.Aisling.QuestManager.DarItem.IsNullOrEmpty())
        {
            switch (client.Aisling.QuestManager.Dar)
            {
                case <= 7:
                    _0X0B = true;
                    options.Add(new(0x0B, $"{{=cIs this what you're looking for{{=a?"));
                    break;
                case 8 when client.Aisling.Level >= 23 || client.Aisling.GameMaster:
                    _0X0D = true;
                    options.Add(new(0x0D, $"{{=cIs this what you're looking for{{=a?"));
                    break;
                case 9 when client.Aisling.Level >= 34 || client.Aisling.GameMaster:
                    _0X0F = true;
                    options.Add(new(0x0F, $"{{=cIs this what you're looking for{{=a?"));
                    break;
            }
        }

        if (_skillList.Count > 0)
        {
            options.Add(new(0x01, "Show Available Skills"));
        }

        if (_spellList.Count > 0)
        {
            options.Add(new(0x0010, "Show Available Spells"));
        }

        options.Add(new(0x02, "Forget Skill"));
        options.Add(new(0x0011, "Forget Spell"));

        if (client.Aisling.QuestManager.DarItem.IsNullOrEmpty())
        {
            switch (client.Aisling.QuestManager.Dar)
            {
                case <= 7 when client.Aisling.Level <= 22 || client.Aisling.GameMaster:
                    _0X0A = true;
                    options.Add(new(0x0A, "Dark Things"));
                    break;
                case 8 when client.Aisling.Level is <= 33 and >= 23 || client.Aisling.GameMaster:
                    _0X0C = true;
                    options.Add(new(0x0C, "Darker Things"));
                    break;
                case 9 when client.Aisling.Level is <= 50 and >= 34 || client.Aisling.GameMaster:
                    _0X0E = true;
                    options.Add(new(0x0E, "Things that bump in the Twilight"));
                    break;
            }
        }

        client.SendOptionsDialog(Mundane, "Looking into the darker things is what I like to do, how may I help you?", options.ToArray());
    }

    public override void OnResponse(GameClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        var darkThings = Random.Shared.Next(1, 12);
        var advExp = (uint)Random.Shared.Next(150000, 300000);
        var advExp2 = (uint)Random.Shared.Next(500000, 1000000);

        _retrieve = darkThings switch
        {
            1 => "Spider Leg",
            2 => "Spider Eye",
            3 => "Centipede Gland",
            4 => "Mold",
            5 => "Spider Silk",
            6 => "Raw Wax",
            7 => "Spoiled Cherries",
            8 => "Rotten Veggies",
            9 => "Spoiled Grapes",
            10 => "Royal Wax",
            11 => "Mead",
            _ => _retrieve
        };

        _retrieveAdv = darkThings switch
        {
            1 => "Wolf Fur",
            2 => "Wolf Skin",
            3 => "Bee Sting",
            4 => "Great Bat Wing",
            5 => "Scorpion Sting",
            6 => "Scorpion Venom",
            7 => "Wolf Lock",
            8 => "Mantis Eye",
            9 => "Goblin Skull",
            10 => "Mead",
            _ => _retrieve
        };

        switch (responseId)
        {
            #region Skills

            case 0x0001:
            {
                var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                var newSkills = _skillList.Except(learnedSkills).ToList();

                newSkills = newSkills.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                if (newSkills.Count > 0)
                {
                    client.SendSkillLearnDialog(Mundane, "What move do you wish to learn? \nThese skills have been taught for generations now and are available to you.", 0x0003,
                        newSkills.Where(i => i.Prerequisites.ClassRequired == client.Aisling.Path
                                             || i.Prerequisites.SecondaryClassRequired == client.Aisling.PastClass
                                             || i.Prerequisites.ClassRequired == Class.Peasant));
                }
                else
                {
                    client.CloseDialog();
                    client.SendMessage(0x02, "I have nothing left to teach you, for now.");
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
                client.SendOptionsDialog(Mundane, "Are you sure you want to learn the method of " + args + "? \nLet me test if you're ready.", args,
                    new OptionsDataItem(0x0006, $"What does {args} do?"),
                    new OptionsDataItem(0x0004, "Learn"),
                    new OptionsDataItem(0x0001, "No, thank you."));
                break;
            }
            case 0x0004:
            {
                var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                if (subject == null) return;

                var conditions = subject.Prerequisites.IsMet(client.Aisling, (msg, result) =>
                {
                    if (!result)
                    {
                        client.SendOptionsDialog(Mundane, msg, subject.Name);
                    }
                });

                if (conditions)
                {
                    client.SendOptionsDialog(Mundane, "Have you brought what is required?",
                        subject.Name,
                        new OptionsDataItem(0x0005, "Yes."),
                        new OptionsDataItem(0x0001, "I'll come back later."));
                }

                break;
            }
            case 0x0006:
            {
                var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                if (subject == null) return;

                client.SendOptionsDialog(Mundane,
                    $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
                    subject.Name,
                    new OptionsDataItem(0x0004, "Yes"),
                    new OptionsDataItem(0x0001, "No"));

                break;
            }
            case 0x0005:
            {
                var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
                if (subject == null) return;

                client.SendAnimation(109, client.Aisling, Mundane);
                client.LearnSkill(Mundane, subject, "Always refine your skills as much as you sharpen your knife.");

                break;
            }

            #endregion

            #region Spells

            case 0x0010:
            {
                var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
                var newSpells = _spellList.Except(learnedSpells).ToList();

                newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

                if (newSpells.Count > 0)
                {
                    client.SendSpellLearnDialog(Mundane, "Do you dare unravel the power of your mind? \nThese are the secrets available to you.", 0x0012,
                        newSpells.Where(i => i.Prerequisites.ClassRequired == client.Aisling.Path
                                             || i.Prerequisites.SecondaryClassRequired == client.Aisling.PastClass
                                             || i.Prerequisites.ClassRequired == Class.Peasant));
                }
                else
                {
                    client.CloseDialog();
                    client.SendMessage(0x02, "I have nothing left to teach you, for now.");
                }

                break;
            }
            case 0x0011:
            {
                client.SendSpellForgetDialog(Mundane, "The mind is a complex place, sometimes we need to declutter. \nBe warned, This cannot be undone.", 0x0800);
                break;
            }
            case 0x0012:
            {
                client.SendOptionsDialog(Mundane, "Are you sure you want to learn the secret of " + args + "? \nLet me test if you're ready.", args,
                    new OptionsDataItem(0x0015, $"What does {args} do?"),
                    new OptionsDataItem(0x0013, "Learn"),
                    new OptionsDataItem(0x0010, "No, thank you."));
                break;
            }
            case 0x0013:
            {
                var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                if (subject == null) return;

                var conditions = subject.Prerequisites.IsMet(client.Aisling, (msg, result) =>
                {
                    if (!result)
                    {
                        client.SendOptionsDialog(Mundane, msg, subject.Name);
                    }
                });

                if (conditions)
                {
                    client.SendOptionsDialog(Mundane, "Have you brought what is required?",
                        subject.Name,
                        new OptionsDataItem(0x0014, "Yes."),
                        new OptionsDataItem(0x0010, "I'll come back later."));
                }

                break;
            }
            case 0x0014:
            {
                var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                if (subject == null) return;

                client.SendAnimation(109, client.Aisling, Mundane);
                client.LearnSpell(Mundane, subject, "Always expand your knowledge, Aisling.");

                break;
            }
            case 0x0015:
            {
                var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
                if (subject == null) return;

                client.SendOptionsDialog(Mundane,
                    $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
                    subject.Name,
                    new OptionsDataItem(0x0013, "Yes"),
                    new OptionsDataItem(0x0010, "No"));

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

            case 8:
            {
                client.SendOptionsDialog(Mundane, "Come back if you're interested in helping me.");
                break;
            }
            case 9:
            {
                if (client.Aisling.QuestManager.DarItem.IsNullOrEmpty())
                {
                    client.Aisling.QuestManager.DarItem = client.Aisling.QuestManager.Dar switch
                    {
                        <= 7 => _retrieve,
                        8 => _retrieveAdv,
                        9 => "Kardi Fur",
                        _ => client.Aisling.QuestManager.DarItem
                    };
                }

                client.SendOptionsDialog(Mundane, $"Currently I'm looking for {{=q{client.Aisling.QuestManager.DarItem}{{=a. *drinks a strong smelling mead*");

                break;
            }

            #region Dark Things

            case 10:
            {
                if (!_0X0A) return;
                var options = new List<OptionsDataItem>
                {
                    new (0x09, "Sure."),
                    new (0x08, "Sorry, I'm busy.")
                };

                client.SendOptionsDialog(Mundane, "I need a few ingredients for my studies, care to help me?", options.ToArray());
                break;
            }
            case 11:
            {
                if (!_0X0B) return;
                if (client.Aisling.HasItem(client.Aisling.QuestManager.DarItem))
                {
                    client.Aisling.QuestManager.Dar++;
                    client.TakeAwayQuantity(client.Aisling, client.Aisling.QuestManager.DarItem, 1);
                    client.Aisling.QuestManager.DarItem = null;
                    client.GiveExp(8000);
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've gained 8,000 experience.");
                    client.SendStats(StatusFlags.WeightMoney);
                    client.SendOptionsDialog(Mundane, $"Ah, there it is.. \n\nFavors Completed: {{=q{client.Aisling.QuestManager.Dar}");
                }
                else
                {
                    client.SendOptionsDialog(Mundane, $"No, I don't see any {{=q{client.Aisling.QuestManager.DarItem}{{=a on your person.");
                }

                break;
            }

            #endregion

            #region Darker Things

            case 12:
            {
                if (!_0X0C) return;
                var options = new List<OptionsDataItem>
                {
                    new (0x09, "Sure."),
                    new (0x08, "Sorry I'm busy.")
                };

                client.SendOptionsDialog(Mundane, $"Hello there again, I need ingredients.. Hic! ..they're a bit harder to come by. \nCurrently I'm looking for {{=q{client.Aisling.QuestManager.DarItem}{{=a.", options.ToArray());
                break;
            }
            case 13:
            {
                if (!_0X0D) return;
                if (client.Aisling.HasItem(client.Aisling.QuestManager.DarItem))
                {
                    client.Aisling.QuestManager.Dar++;
                    client.Aisling.QuestManager.MilethReputation += 1;
                    client.TakeAwayQuantity(client.Aisling, client.Aisling.QuestManager.DarItem, 1);
                    client.Aisling.QuestManager.DarItem = null;
                    client.GiveExp(advExp);
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp} experience.");
                    client.SendStats(StatusFlags.WeightMoney);
                    client.SendOptionsDialog(Mundane, $"Hic! That's what I was looking for. \n\nFavors Completed: {{=q{client.Aisling.QuestManager.Dar}");
                }
                else
                {
                    client.SendOptionsDialog(Mundane, $"No, I don't see any {{=q{client.Aisling.QuestManager.DarItem}{{=a on your person.    Hic!");
                }

                break;
            }

            #endregion

            #region Things that bump in the Twilight

            case 14:
            {
                if (!_0X0E) return;
                var options = new List<OptionsDataItem>
                {
                    new (0x09, "Sure."),
                    new (0x08, "Sorry I'm busy.")
                };

                client.SendOptionsDialog(Mundane, $"Hic! The items I require are ....   \n*snores* ...    {{=q{client.Aisling.QuestManager.DarItem}{{=a.", options.ToArray());
                break;
            }
            case 15:
            {
                if (!_0X0F) return;
                if (client.Aisling.HasItem(client.Aisling.QuestManager.DarItem))
                {
                    client.Aisling.QuestManager.Dar++;
                    client.Aisling.QuestManager.MilethReputation += 1;
                    client.TakeAwayQuantity(client.Aisling, client.Aisling.QuestManager.DarItem, 1);
                    client.Aisling.QuestManager.DarItem = null;
                    client.GiveExp(advExp2);
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've gained {advExp2} experience.");
                    client.SendStats(StatusFlags.WeightMoney);
                    client.SendOptionsDialog(Mundane,
                        "I must of passed out again. Thank you, this will advance my research.");
                }
                else
                {
                    client.SendOptionsDialog(Mundane,
                        $"ZzZzzzZZzzZ *mumbles..*  ..{{=q{client.Aisling.QuestManager.DarItem}{{=a.           Hic!");
                }

                if (client.Aisling.QuestManager.Dar == 10)
                {
                    var item = new Legend.LegendItem
                    {
                        Category = "Adventure",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.Blue,
                        Icon = (byte)LegendIcon.Wizard,
                        Value = "A Dark Favor"
                    };

                    client.Aisling.LegendBook.AddLegend(item, client);
                }

                break;


                #endregion
            }
        }
    }
}