using Darkages.Enums;
using Darkages.Models;

namespace Darkages.Meta;

public class HighElfMeta : MetafileManager
{
    public static void GenerateHighElfBezerkerMeta()
    {
        var sClass1 = new Metafile { Name = "SClass73", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass74", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass75", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass76", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass77", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass78", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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

    public static void GenerateHighElfDefenderMeta()
    {
        var sClass1 = new Metafile { Name = "SClass79", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass80", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass81", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass82", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass83", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass84", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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

    public static void GenerateHighElfAssassinMeta()
    {
        var sClass1 = new Metafile { Name = "SClass85", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass86", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass87", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass88", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass89", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass90", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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

    public static void GenerateHighElfClericMeta()
    {
        var sClass1 = new Metafile { Name = "SClass91", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass92", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass93", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass94", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass95", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass96", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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

    public static void GenerateHighElfArcanusMeta()
    {
        var sClass1 = new Metafile { Name = "SClass97", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass98", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass99", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass100", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass101", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass102", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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

    public static void GenerateHighElfMonkMeta()
    {
        var sClass1 = new Metafile { Name = "SClass103", Nodes = new List<MetafileNode>() };
        var sClass2 = new Metafile { Name = "SClass104", Nodes = new List<MetafileNode>() };
        var sClass3 = new Metafile { Name = "SClass105", Nodes = new List<MetafileNode>() };
        var sClass4 = new Metafile { Name = "SClass106", Nodes = new List<MetafileNode>() };
        var sClass5 = new Metafile { Name = "SClass107", Nodes = new List<MetafileNode>() };
        var sClass6 = new Metafile { Name = "SClass108", Nodes = new List<MetafileNode>() };

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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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
            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

            if (template.Prerequisites is { RaceRequired: Race.HighElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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