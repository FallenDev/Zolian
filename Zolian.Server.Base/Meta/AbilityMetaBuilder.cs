using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Server;
using Darkages.Templates;

namespace Darkages.Meta;

public abstract class AbilityMetaBuilder : MetafileManager
{
    public static void AbilityMeta()
    {
        foreach (var abilityTuple in SClassDictionary.SkillMap)
        {
            var sClass = new Metafile { Name = abilityTuple.Value, Nodes = [] };
            var race = abilityTuple.Key.race;
            var class1 = abilityTuple.Key.path;
            var class2 = abilityTuple.Key.pastClass;
            var job = abilityTuple.Key.job;

            SkillBuilder(sClass, race, class1, class2, job);
            sClass.Nodes.Add(new MetafileNode("", ""));
            SpellBuilder(sClass, race, class1, class2, job);
            CompileTemplate(sClass);
            ServerSetup.Instance.Game.Metafiles.Add(sClass);
        }
    }

    private static void SkillBuilder(Metafile sClass, Race race, Class currentClass, Class previousClass, Job job)
    {
        sClass.Nodes.Add(new MetafileNode("Skill", ""));

        foreach (var template in ServerSetup.Instance.GlobalSkillTemplateCache
                     .Where(p => p.Value.Prerequisites != null)
                     .OrderBy(p => p.Value.Prerequisites.StageRequired)
                     .ThenBy(p => p.Value.Prerequisites.ExpLevelRequired)
                     .Select(p => p.Value)
                     .Distinct())
        {
            if (template.Prerequisites.JobRequired == job && template.Prerequisites.JobRequired != Job.None)
            {
                AddSkillNodeToMetaFile(sClass, template);
                continue;
            }

            if (template.Prerequisites.RaceRequired == race ||
                ((template.Prerequisites.ClassRequired == currentClass ||
                  template.Prerequisites.ClassRequired == Class.Peasant) ||
                 template.Prerequisites.SecondaryClassRequired == previousClass))
            {
                AddSkillNodeToMetaFile(sClass, template);
                continue;
            }

            if ((currentClass is Class.Berserker or Class.Defender && template.Prerequisites.SecondaryClassRequired == Class.DualBash)
                || (previousClass is Class.Berserker or Class.Defender && template.Prerequisites.SecondaryClassRequired == Class.DualBash))
            {
                AddSkillNodeToMetaFile(sClass, template);
                continue;
            }

            if ((currentClass is Class.Cleric or Class.Arcanus && template.Prerequisites.SecondaryClassRequired == Class.DualCast)
                || (previousClass is Class.Cleric or Class.Arcanus && template.Prerequisites.SecondaryClassRequired == Class.DualCast))
            {
                AddSkillNodeToMetaFile(sClass, template);
            }
        }

        sClass.Nodes.Add(new MetafileNode("Skill_End", ""));
    }

    private static void SpellBuilder(Metafile sClass, Race race, Class currentClass, Class previousClass, Job job)
    {
        sClass.Nodes.Add(new MetafileNode("Spell", ""));

        foreach (var template in ServerSetup.Instance.GlobalSpellTemplateCache
                     .Where(p => p.Value.Prerequisites != null)
                     .OrderBy(p => p.Value.Prerequisites.StageRequired)
                     .ThenBy(p => p.Value.Prerequisites.ExpLevelRequired)
                     .Select(p => p.Value)
                     .Distinct())
        {
            if (template.Prerequisites.JobRequired == job && template.Prerequisites.JobRequired != Job.None)
            {
                AddSpellNodeToMetaFile(sClass, template);
                continue;
            }

            if (template.Prerequisites.RaceRequired == race ||
                ((template.Prerequisites.ClassRequired == currentClass ||
                 template.Prerequisites.ClassRequired == Class.Peasant) ||
                 template.Prerequisites.SecondaryClassRequired == previousClass))
            {
                AddSpellNodeToMetaFile(sClass, template);
                continue;
            }

            if ((currentClass is Class.Berserker or Class.Defender && template.Prerequisites.SecondaryClassRequired == Class.DualBash)
                || (previousClass is Class.Berserker or Class.Defender && template.Prerequisites.SecondaryClassRequired == Class.DualBash))
            {
                AddSpellNodeToMetaFile(sClass, template);
                continue;
            }

            if ((currentClass is Class.Cleric or Class.Arcanus && template.Prerequisites.SecondaryClassRequired == Class.DualCast)
                || (previousClass is Class.Cleric or Class.Arcanus && template.Prerequisites.SecondaryClassRequired == Class.DualCast))
            {
                AddSpellNodeToMetaFile(sClass, template);
            }
        }

        sClass.Nodes.Add(new MetafileNode("Spell_End", ""));
    }

    private static void AddSkillNodeToMetaFile(Metafile sClass, SkillTemplate template)
    {
        sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
    }

    private static void AddSpellNodeToMetaFile(Metafile sClass, SpellTemplate template)
    {
        sClass.Nodes.Add(new MetafileNode(template.Prerequisites.DisplayName, template.GetMetaData()));
    }
}