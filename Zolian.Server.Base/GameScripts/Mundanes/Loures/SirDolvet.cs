﻿using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Loures;

[Script("Sir Dolvet")]
public class SirDolvet : MundaneScript
{
    private readonly List<SkillTemplate> _skillList;
    private readonly List<SpellTemplate> _spellList;

    public SirDolvet(WorldServer server, Mundane mundane) : base(server, mundane)
    {
        _skillList = ObtainSkillList();
        _spellList = ObtainSpellList();
    }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (_skillList.Count > 0 && client.Aisling.JobClass == Job.DarkKnight)
            options.Add(new(0x20, "Learn Dark Knight Skills"));
        if (_spellList.Count > 0 && client.Aisling.JobClass == Job.DarkKnight)
            options.Add(new(0x30, "Learn Dark Knight Spells"));

        if (client.Aisling.Stage <= ClassStage.Master
            && client.Aisling.ExpLevel >= 250
            && client.Aisling.QuestManager.AssassinsGuildReputation >= 4
            && client.Aisling.QuestManager.UndineReputation >= 4
            && (client.Aisling.Path == Class.Berserker || client.Aisling.PastClass == Class.Berserker)
            && (client.Aisling.Path == Class.Assassin || client.Aisling.PastClass == Class.Assassin))
        {
            options.Add(new(0x01, "I am but a canvas, teach me"));
            client.SendOptionsDialog(Mundane, "Ya know, you'd make a fine Dark Knight", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.ExpLevel >= 250
                ? "Seasons change, but never should your character."
                : "Hello there young one. Kneel before the king.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                {
                    client.CloseDialog();
                }
                break;
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>();
                    var qualifiedItem = CheckForForsakenDragonSlayer(client);

                    if (qualifiedItem != null)
                    {
                        options.Add(new(0x02, "Advance"));
                        client.SendOptionsDialog(Mundane, "What a beautiful blade, are you ready to delve into the dark arts of swordplay?", options.ToArray());
                        return;
                    }

                    options.Add(new(0x00, "On it"));
                    client.SendOptionsDialog(Mundane, "Ah ha! *laughs loudly* Well then, bring me a worthy blade.\n" +
                                                      $"{{=qEnhance or find a Dragon Slayer of Forsaken quality", options.ToArray());
                }
                break;
            case 0x02:
                {
                    var options = new List<Dialog.OptionsDataItem> { new(0x00, "Thank you Sir Dolvet") };
                    var qualifiedItem = CheckForForsakenDragonSlayer(client);

                    if (qualifiedItem != null)
                        OnResponse(client, 0x999, $"{client.Aisling.Serial}");

                    client.SendOptionsDialog(Mundane, "I will now perform the seal which binds. Congratulations Dark Knight, come back to me " +
                                                      "whenever you're ready to advance your techniques.", options.ToArray());
                }
                break;
            case 0x999:
                {
                    if (responseId != client.Aisling.Serial) return;
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=q{client.Aisling.Username} has advanced to Dark Knight"));
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(67, client.Aisling.Position));
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(116, false));
                    client.Aisling.Stage = ClassStage.Job;
                    client.Aisling.JobClass = Job.DarkKnight;

                    var legend = new Legend.LegendItem
                    {
                        Key = "LJob1",
                        Time = DateTime.UtcNow,
                        Color = LegendColor.TurquoiseG7,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Advanced to Job - Dark Knight"
                    };

                    client.Aisling.LegendBook.AddLegend(legend, client);
                }
                break;
            case 0x20:
                {
                    ShowSkillList(client);
                }
                break;
            case 0x21:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the method of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x22, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x23, "Learn"),
                        new Dialog.OptionsDataItem(0x00, "No, thank you."));
                }
                break;
            case 0x22:
                {
                    ShowSkillDescription(client, args);
                }
                break;
            case 0x23:
                {
                    CheckSkillPrerequisites(client, args);
                }
                break;
            case 0x24:
                {
                    LearnSkill(client, args);
                }
                break;
            case 0x30:
                {
                    ShowSpellList(client);
                }
                break;
            case 0x31:
                {
                    client.SendOptionsDialog(Mundane, "Are you sure you want to learn the secret of " + args + "? \nLet me test if you're ready.", args,
                        new Dialog.OptionsDataItem(0x32, $"What does {args} do?"),
                        new Dialog.OptionsDataItem(0x33, "Learn"),
                        new Dialog.OptionsDataItem(0x00, "No, thank you."));
                }
                break;
            case 0x32:
                {
                    ShowSpellDescription(client, args);
                }
                break;
            case 0x33:
                {
                    CheckSpellPrerequisites(client, args);
                }
                break;
            case 0x34:
                {
                    LearnSpell(client, args);
                }
                break;
        }
    }

    public override void OnGossip(WorldClient client, string message) { }

    private static Item CheckForForsakenDragonSlayer(WorldClient client)
    {
        var item = client.Aisling.HasItemReturnItem("Dragon Slayer");
        return item.OriginalQuality == Item.Quality.Forsaken ? item : null;
    }

    #region Skills & Spells

    private void ShowSkillList(WorldClient client)
    {
        var learnedSkills = client.Aisling.SkillBook.Skills.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
        var newSkills = _skillList.Except(learnedSkills).Where(i => i.Prerequisites.StageRequired.StageFlagIsSet(ClassStage.Job)
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(Job.Samurai)).ToList();

        newSkills = newSkills.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

        if (newSkills.Count > 0)
        {
            client.SendSkillLearnDialog(Mundane, "What ability are we attempting? \nThese job abilities are unique to you.", 0x21, newSkills);
        }
        else
        {
            client.CloseDialog();
            client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
        }
    }

    private void CheckSkillPrerequisites(WorldClient client, string args)
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
            client.SendOptionsDialog(Mundane, "Do you have what is required?",
                subject.Name,
                new Dialog.OptionsDataItem(0x24, "Yes, Sensei"),
                new Dialog.OptionsDataItem(0x00, "I will return"));
        }
    }

    private void ShowSkillDescription(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
        if (subject == null) return;

        client.SendOptionsDialog(Mundane,
            $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
            subject.Name,
            new Dialog.OptionsDataItem(0x23, "Yes"),
            new Dialog.OptionsDataItem(0x00, "No"));
    }

    private void LearnSkill(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSkillTemplateCache[args];
        if (subject == null) return;
        client.LearnSkill(Mundane, subject, "Remember, it is more honorable to die protecting those weaker than you. Than to run.");
    }

    private void ShowSpellList(WorldClient client)
    {
        var learnedSpells = client.Aisling.SpellBook.Spells.Where(i => i.Value != null).Select(i => i.Value.Template).ToList();
        var newSpells = _spellList.Except(learnedSpells).Where(i => i.Prerequisites.StageRequired.StageFlagIsSet(ClassStage.Job)
                                                                    && i.Prerequisites.JobRequired.JobFlagIsSet(Job.Samurai)).ToList();

        newSpells = newSpells.OrderBy(i => Math.Abs(i.Prerequisites.ExpLevelRequired - client.Aisling.ExpLevel)).ToList();

        if (newSpells.Count > 0)
        {
            client.SendSpellLearnDialog(Mundane, "Do you dare unravel the power of your mind? \nThese are the secrets available to you.", 0x31, newSpells);
        }
        else
        {
            client.CloseDialog();
            client.SendServerMessage(ServerMessageType.OrangeBar1, "I have nothing left to teach you, for now.");
        }
    }

    private void CheckSpellPrerequisites(WorldClient client, string args)
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
            client.SendOptionsDialog(Mundane, "Do you have what is required?",
                subject.Name,
                new Dialog.OptionsDataItem(0x24, "Yes, Sensei"),
                new Dialog.OptionsDataItem(0x00, "I will return"));
        }
    }

    private void ShowSpellDescription(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
        if (subject == null) return;

        client.SendOptionsDialog(Mundane,
            $"{args} - {(string.IsNullOrEmpty(subject.Description) ? "No more information is available." : subject.Description)}" + "\n" + subject.Prerequisites,
            subject.Name,
            new Dialog.OptionsDataItem(0x33, "Yes"),
            new Dialog.OptionsDataItem(0x00, "No"));
    }

    private void LearnSpell(WorldClient client, string args)
    {
        var subject = ServerSetup.Instance.GlobalSpellTemplateCache[args];
        if (subject == null) return;
        client.LearnSpell(Mundane, subject, "Training your mind is just as important as sharpening your blade.");
    }

    #endregion
}