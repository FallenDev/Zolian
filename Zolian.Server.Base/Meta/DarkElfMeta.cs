using Darkages.Enums;
using Darkages.Models;

namespace Darkages.Meta;

public class DarkElfMeta : MetafileManager
{
    public static void GenerateDarkElfBezerkerMeta()
    {
        var sClass1 = new Metafile { Name = "SClass109", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass110", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass111", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass112", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass113", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass114", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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

    public static void GenerateDarkElfDefenderMeta()
    {
        var sClass1 = new Metafile { Name = "SClass115", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass116", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass117", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass118", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass119", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass120", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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

    public static void GenerateDarkElfAssassinMeta()
    {
        var sClass1 = new Metafile { Name = "SClass121", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass122", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass123", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass124", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass125", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass126", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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

    public static void GenerateDarkElfClericMeta()
    {
        var sClass1 = new Metafile { Name = "SClass127", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass128", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass129", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass130", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass131", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass132", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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

    public static void GenerateDarkElfArcanusMeta()
    {
        var sClass1 = new Metafile { Name = "SClass133", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass134", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass135", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass136", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass137", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass138", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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

    public static void GenerateDarkElfMonkMeta()
    {
        var sClass1 = new Metafile { Name = "SClass139", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass140", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass141", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass142", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass143", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass144", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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
            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.DarkElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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