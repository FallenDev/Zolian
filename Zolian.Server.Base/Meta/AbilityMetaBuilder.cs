using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Server;

namespace Darkages.Meta;

public class AbilityMetaBuilder : MetafileManager
{
    public static void AbilityMeta()
    {
        foreach (var abilityTuple in WorldServer.SkillMap)
        {
            var sClass = new Metafile { Name = abilityTuple.Value, Nodes = [] };
            var race = abilityTuple.Key.race;
            var class1 = abilityTuple.Key.path;
            var class2 = abilityTuple.Key.pastClass;

            sClass.Nodes.Add(new MetafileNode("Skill", ""));

            foreach (var template in ServerSetup.Instance.GlobalSkillTemplateCache
                         .Where(p => p.Value.Prerequisites != null)
                         .OrderBy(p => p.Value.Prerequisites.ExpLevelRequired)
                         .Select(p => p.Value)
                         .Distinct())
            {
                if (template.Prerequisites.RaceRequired == race || ((template.Prerequisites.ClassRequired == class1 || template.Prerequisites.ClassRequired == Class.Peasant) || template.Prerequisites.SecondaryClassRequired == class2))
                {
                    sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
                    continue;
                }

                if (class1 is Class.Berserker or Class.Defender && template.Prerequisites.SecondaryClassRequired == Class.DualBash)
                {
                    sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
                    continue;
                }

                if (class1 is Class.Cleric or Class.Arcanus && template.Prerequisites.SecondaryClassRequired == Class.DualCast)
                {
                    sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
                }
            }

            sClass.Nodes.Add(new MetafileNode("Skill_End", ""));
            sClass.Nodes.Add(new MetafileNode("", ""));
            sClass.Nodes.Add(new MetafileNode("Spell", ""));

            foreach (var template in ServerSetup.Instance.GlobalSpellTemplateCache
                         .Where(p => p.Value.Prerequisites != null)
                         .OrderBy(p => p.Value.Prerequisites.ExpLevelRequired)
                         .Select(p => p.Value)
                         .Distinct())
            {
                if (template.Prerequisites.RaceRequired == race || ((template.Prerequisites.ClassRequired == class1 || template.Prerequisites.ClassRequired == Class.Peasant) || template.Prerequisites.SecondaryClassRequired == class2))
                {
                    sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
                    continue;
                }

                if (class1 is Class.Berserker or Class.Defender && template.Prerequisites.SecondaryClassRequired == Class.DualBash)
                {
                    sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
                    continue;
                }

                if (class1 is Class.Cleric or Class.Arcanus && template.Prerequisites.SecondaryClassRequired == Class.DualCast)
                {
                    sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
                }
            }

            sClass.Nodes.Add(new MetafileNode("Spell_End", ""));
            CompileTemplate(sClass);
            Metafiles.Add(sClass);
        }
    }
}