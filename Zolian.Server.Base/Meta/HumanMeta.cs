using Darkages.Enums;
using Darkages.Models;

namespace Darkages.Meta;

public class HumanMeta : MetafileManager
{
    public static void GenerateHumanBezerkerMeta()
    {
        var sClass1 = new Metafile { Name = "SClass1", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass2", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass3", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass4", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass5", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass6", Nodes = new List<MetafileNode>() };

        sClass1.Nodes.Add(new MetafileNode("Skill", ""));
        sClass2.Nodes.Add(new MetafileNode("Skill", ""));
        sClass3.Nodes.Add(new MetafileNode("Skill", ""));
        sClass4.Nodes.Add(new MetafileNode("Skill", ""));
        sClass5.Nodes.Add(new MetafileNode("Skill", ""));
        sClass6.Nodes.Add(new MetafileNode("Skill", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSkillTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass1.Nodes.Add(new MetafileNode("", ""));
        sClass1.Nodes.Add(new MetafileNode("Spell", ""));

        sClass2.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass2.Nodes.Add(new MetafileNode("", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell", ""));

        sClass3.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass3.Nodes.Add(new MetafileNode("", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell", ""));

        sClass4.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass4.Nodes.Add(new MetafileNode("", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell", ""));

        sClass5.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass5.Nodes.Add(new MetafileNode("", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell", ""));

        sClass6.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass6.Nodes.Add(new MetafileNode("", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSpellTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell_End", ""));


        CompileTemplate(sClass1);
        CompileTemplate(sClass2);
        CompileTemplate(sClass3);
        CompileTemplate(sClass4);
        CompileTemplate(sClass5);
        CompileTemplate(sClass6);

        Metafiles.Add(sClass1);
        Metafiles.Add(sClass2);
        Metafiles.Add(sClass3);
        Metafiles.Add(sClass4);
        Metafiles.Add(sClass5);
        Metafiles.Add(sClass6);
    }

    public static void GenerateHumanDefenderMeta()
    {
        var sClass1 = new Metafile { Name = "SClass7", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass8", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass9", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass10", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass11", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass12", Nodes = new List<MetafileNode>() };

        sClass1.Nodes.Add(new MetafileNode("Skill", ""));
        sClass2.Nodes.Add(new MetafileNode("Skill", ""));
        sClass3.Nodes.Add(new MetafileNode("Skill", ""));
        sClass4.Nodes.Add(new MetafileNode("Skill", ""));
        sClass5.Nodes.Add(new MetafileNode("Skill", ""));
        sClass6.Nodes.Add(new MetafileNode("Skill", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSkillTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass1.Nodes.Add(new MetafileNode("", ""));
        sClass1.Nodes.Add(new MetafileNode("Spell", ""));

        sClass2.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass2.Nodes.Add(new MetafileNode("", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell", ""));

        sClass3.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass3.Nodes.Add(new MetafileNode("", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell", ""));

        sClass4.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass4.Nodes.Add(new MetafileNode("", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell", ""));

        sClass5.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass5.Nodes.Add(new MetafileNode("", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell", ""));

        sClass6.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass6.Nodes.Add(new MetafileNode("", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSpellTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell_End", ""));


        CompileTemplate(sClass1);
        CompileTemplate(sClass2);
        CompileTemplate(sClass3);
        CompileTemplate(sClass4);
        CompileTemplate(sClass5);
        CompileTemplate(sClass6);

        Metafiles.Add(sClass1);
        Metafiles.Add(sClass2);
        Metafiles.Add(sClass3);
        Metafiles.Add(sClass4);
        Metafiles.Add(sClass5);
        Metafiles.Add(sClass6);
    }

    public static void GenerateHumanAssassinMeta()
    {
        var sClass1 = new Metafile { Name = "SClass13", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass14", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass15", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass16", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass17", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass18", Nodes = new List<MetafileNode>() };

        sClass1.Nodes.Add(new MetafileNode("Skill", ""));
        sClass2.Nodes.Add(new MetafileNode("Skill", ""));
        sClass3.Nodes.Add(new MetafileNode("Skill", ""));
        sClass4.Nodes.Add(new MetafileNode("Skill", ""));
        sClass5.Nodes.Add(new MetafileNode("Skill", ""));
        sClass6.Nodes.Add(new MetafileNode("Skill", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSkillTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass1.Nodes.Add(new MetafileNode("", ""));
        sClass1.Nodes.Add(new MetafileNode("Spell", ""));

        sClass2.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass2.Nodes.Add(new MetafileNode("", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell", ""));

        sClass3.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass3.Nodes.Add(new MetafileNode("", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell", ""));

        sClass4.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass4.Nodes.Add(new MetafileNode("", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell", ""));

        sClass5.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass5.Nodes.Add(new MetafileNode("", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell", ""));

        sClass6.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass6.Nodes.Add(new MetafileNode("", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSpellTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell_End", ""));


        CompileTemplate(sClass1);
        CompileTemplate(sClass2);
        CompileTemplate(sClass3);
        CompileTemplate(sClass4);
        CompileTemplate(sClass5);
        CompileTemplate(sClass6);

        Metafiles.Add(sClass1);
        Metafiles.Add(sClass2);
        Metafiles.Add(sClass3);
        Metafiles.Add(sClass4);
        Metafiles.Add(sClass5);
        Metafiles.Add(sClass6);
    }

    public static void GenerateHumanClericMeta()
    {
        var sClass1 = new Metafile { Name = "SClass19", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass20", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass21", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass22", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass23", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass24", Nodes = new List<MetafileNode>() };

        sClass1.Nodes.Add(new MetafileNode("Skill", ""));
        sClass2.Nodes.Add(new MetafileNode("Skill", ""));
        sClass3.Nodes.Add(new MetafileNode("Skill", ""));
        sClass4.Nodes.Add(new MetafileNode("Skill", ""));
        sClass5.Nodes.Add(new MetafileNode("Skill", ""));
        sClass6.Nodes.Add(new MetafileNode("Skill", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSkillTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass1.Nodes.Add(new MetafileNode("", ""));
        sClass1.Nodes.Add(new MetafileNode("Spell", ""));

        sClass2.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass2.Nodes.Add(new MetafileNode("", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell", ""));

        sClass3.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass3.Nodes.Add(new MetafileNode("", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell", ""));

        sClass4.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass4.Nodes.Add(new MetafileNode("", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell", ""));

        sClass5.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass5.Nodes.Add(new MetafileNode("", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell", ""));

        sClass6.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass6.Nodes.Add(new MetafileNode("", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSpellTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell_End", ""));


        CompileTemplate(sClass1);
        CompileTemplate(sClass2);
        CompileTemplate(sClass3);
        CompileTemplate(sClass4);
        CompileTemplate(sClass5);
        CompileTemplate(sClass6);

        Metafiles.Add(sClass1);
        Metafiles.Add(sClass2);
        Metafiles.Add(sClass3);
        Metafiles.Add(sClass4);
        Metafiles.Add(sClass5);
        Metafiles.Add(sClass6);
    }

    public static void GenerateHumanArcanusMeta()
    {
        var sClass1 = new Metafile { Name = "SClass25", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass26", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass27", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass28", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass29", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass30", Nodes = new List<MetafileNode>() };

        sClass1.Nodes.Add(new MetafileNode("Skill", ""));
        sClass2.Nodes.Add(new MetafileNode("Skill", ""));
        sClass3.Nodes.Add(new MetafileNode("Skill", ""));
        sClass4.Nodes.Add(new MetafileNode("Skill", ""));
        sClass5.Nodes.Add(new MetafileNode("Skill", ""));
        sClass6.Nodes.Add(new MetafileNode("Skill", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSkillTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass1.Nodes.Add(new MetafileNode("", ""));
        sClass1.Nodes.Add(new MetafileNode("Spell", ""));

        sClass2.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass2.Nodes.Add(new MetafileNode("", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell", ""));

        sClass3.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass3.Nodes.Add(new MetafileNode("", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell", ""));

        sClass4.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass4.Nodes.Add(new MetafileNode("", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell", ""));

        sClass5.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass5.Nodes.Add(new MetafileNode("", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell", ""));

        sClass6.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass6.Nodes.Add(new MetafileNode("", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSpellTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell_End", ""));


        CompileTemplate(sClass1);
        CompileTemplate(sClass2);
        CompileTemplate(sClass3);
        CompileTemplate(sClass4);
        CompileTemplate(sClass5);
        CompileTemplate(sClass6);

        Metafiles.Add(sClass1);
        Metafiles.Add(sClass2);
        Metafiles.Add(sClass3);
        Metafiles.Add(sClass4);
        Metafiles.Add(sClass5);
        Metafiles.Add(sClass6);
    }

    public static void GenerateHumanMonkMeta()
    {
        var sClass1 = new Metafile { Name = "SClass31", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass32", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass33", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass34", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass35", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass36", Nodes = new List<MetafileNode>() };

        sClass1.Nodes.Add(new MetafileNode("Skill", ""));
        sClass2.Nodes.Add(new MetafileNode("Skill", ""));
        sClass3.Nodes.Add(new MetafileNode("Skill", ""));
        sClass4.Nodes.Add(new MetafileNode("Skill", ""));
        sClass5.Nodes.Add(new MetafileNode("Skill", ""));
        sClass6.Nodes.Add(new MetafileNode("Skill", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSkillTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass1.Nodes.Add(new MetafileNode("", ""));
        sClass1.Nodes.Add(new MetafileNode("Spell", ""));

        sClass2.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass2.Nodes.Add(new MetafileNode("", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell", ""));

        sClass3.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass3.Nodes.Add(new MetafileNode("", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell", ""));

        sClass4.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass4.Nodes.Add(new MetafileNode("", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell", ""));

        sClass5.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass5.Nodes.Add(new MetafileNode("", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell", ""));

        sClass6.Nodes.Add(new MetafileNode("Skill_End", ""));
        sClass6.Nodes.Add(new MetafileNode("", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell", ""));

        foreach (var template in from v in ServerSetup.Instance.GlobalSpellTemplateCache
                 let prerequisites = v.Value.Prerequisites
                 where prerequisites != null
                 orderby prerequisites.ExpLevelRequired
                 select v.Value)
        {
            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.Human } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
                sClass6.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
        }

        sClass1.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass2.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass3.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass4.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass5.Nodes.Add(new MetafileNode("Spell_End", ""));
        sClass6.Nodes.Add(new MetafileNode("Spell_End", ""));


        CompileTemplate(sClass1);
        CompileTemplate(sClass2);
        CompileTemplate(sClass3);
        CompileTemplate(sClass4);
        CompileTemplate(sClass5);
        CompileTemplate(sClass6);

        Metafiles.Add(sClass1);
        Metafiles.Add(sClass2);
        Metafiles.Add(sClass3);
        Metafiles.Add(sClass4);
        Metafiles.Add(sClass5);
        Metafiles.Add(sClass6);
    }
}