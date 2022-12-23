using Darkages.Enums;
using Darkages.Models;

namespace Darkages.Meta
{
    public class HalfElfMeta : MetafileManager
    {
        public static void GenerateHalfElfBezerkerMeta()
        {
            var sClass1 = new Metafile { Name = "SClass37", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass38", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass39", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass40", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass41", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass42", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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

        public static void GenerateHalfElfDefenderMeta()
        {
            var sClass1 = new Metafile { Name = "SClass43", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass44", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass45", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass46", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass47", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass48", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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

        public static void GenerateHalfElfAssassinMeta()
        {
            var sClass1 = new Metafile { Name = "SClass49", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass50", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass51", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass52", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass53", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass54", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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

        public static void GenerateHalfElfClericMeta()
        {
            var sClass1 = new Metafile { Name = "SClass55", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass56", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass57", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass58", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass59", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass60", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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

        public static void GenerateHalfElfArcanusMeta()
        {
            var sClass1 = new Metafile { Name = "SClass61", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass62", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass63", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass64", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass65", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass66", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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

        public static void GenerateHalfElfMonkMeta()
        {
            var sClass1 = new Metafile { Name = "SClass67", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass68", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass69", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass70", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass71", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass72", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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
                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.HalfElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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
}
