using Darkages.Enums;
using Darkages.Models;

namespace Darkages.Meta
{
    public class WoodElfMeta : MetafileManager
    {
        public static void GenerateWoodElfBezerkerMeta()
        {
            var sClass1 = new Metafile { Name = "SClass145", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass146", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass147", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass148", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass149", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass150", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Berserker })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Berserker })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Berserker })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Berserker })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Berserker })
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

        public static void GenerateWoodElfDefenderMeta()
        {
            var sClass1 = new Metafile { Name = "SClass151", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass152", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass153", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass154", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass155", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass156", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Defender })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Defender })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Defender })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Defender })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Defender })
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

        public static void GenerateWoodElfAssassinMeta()
        {
            var sClass1 = new Metafile { Name = "SClass157", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass158", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass159", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass160", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass161", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass162", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Assassin })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Assassin })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Assassin })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Assassin })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Assassin })
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

        public static void GenerateWoodElfClericMeta()
        {
            var sClass1 = new Metafile { Name = "SClass163", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass164", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass165", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass166", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass167", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass168", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Cleric })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Cleric })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Cleric })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Cleric })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Cleric })
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

        public static void GenerateWoodElfArcanusMeta()
        {
            var sClass1 = new Metafile { Name = "SClass169", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass170", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass171", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass172", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass173", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass174", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Arcanus })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Arcanus })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Arcanus })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Arcanus })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk or Class.Arcanus })
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

        public static void GenerateWoodElfMonkMeta()
        {
            var sClass1 = new Metafile { Name = "SClass175", Nodes = new List<MetafileNode>() };
            var sClass2 = new Metafile { Name = "SClass176", Nodes = new List<MetafileNode>() };
            var sClass3 = new Metafile { Name = "SClass177", Nodes = new List<MetafileNode>() };
            var sClass4 = new Metafile { Name = "SClass178", Nodes = new List<MetafileNode>() };
            var sClass5 = new Metafile { Name = "SClass179", Nodes = new List<MetafileNode>() };
            var sClass6 = new Metafile { Name = "SClass180", Nodes = new List<MetafileNode>() };

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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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
                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Berserker or Class.Peasant } or { SecondaryClassRequired: Class.Berserker or Class.DualBash or Class.Monk })
                    sClass1.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Defender or Class.Peasant } or { SecondaryClassRequired: Class.Defender or Class.DualBash or Class.Monk })
                    sClass2.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Assassin or Class.Peasant } or { SecondaryClassRequired: Class.Assassin or Class.Monk })
                    sClass3.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Cleric or Class.Peasant } or { SecondaryClassRequired: Class.Cleric or Class.DualCast or Class.Monk })
                    sClass4.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Arcanus or Class.Peasant } or { SecondaryClassRequired: Class.Arcanus or Class.DualCast or Class.Monk })
                    sClass5.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));

                if (template.Prerequisites is { RaceRequired: Race.WoodElf } or { ClassRequired: Class.Monk or Class.Peasant } or { SecondaryClassRequired: Class.Monk })
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
