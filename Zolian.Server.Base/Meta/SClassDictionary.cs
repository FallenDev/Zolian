using System.Collections.Frozen;
using Darkages.Enums;

namespace Darkages.Meta;

public static class SClassDictionary
{
    internal static FrozenDictionary<(Race race, Class path, Class pastClass, Job job), string> SkillMap;

    public static void SkillMapper()
    {
        // Pre-allocation to a prime number
        var skillMap = GenerateSkillMap();
        SkillMap = skillMap.ToFrozenDictionary();

    }

    private static Dictionary<(Race race, Class path, Class pastClass, Job job), string> GenerateSkillMap()
    {
        var skillMap = new Dictionary<(Race race, Class path, Class pastClass, Job job), string>();
        var sClassCounter = 1; // Starting number for SClass
        var races = Enum.GetValues<Race>().Where(race => race != Race.UnDecided);
        var paths = Enum.GetValues<SClassMapper>();
        var pastClasses = Enum.GetValues<SClassMapper>();
        var jobs = Enum.GetValues<Job>();

        foreach (var race in races)
        {
            foreach (var path in paths)
            {
                foreach (var pastClass in pastClasses)
                {
                    // Handle cases where path equals pastClass
                    if (path == pastClass)
                    {
                        AddSkillMapEntry(skillMap, (race, (Class)path, (Class)pastClass, Job.None), ref sClassCounter);
                        continue;
                    }

                    // Handle cases where path and pastClass differ
                    foreach (var job in jobs)
                    {
                        AddSkillMapEntry(skillMap, (race, (Class)path, (Class)pastClass, job), ref sClassCounter);
                    }
                }
            }
        }

        return skillMap;
    }

    private static void AddSkillMapEntry(Dictionary<(Race race, Class path, Class pastClass, Job job), string> skillMap, (Race race, Class path, Class pastClass, Job job) key, ref int sClassCounter)
    {
        var value = $"SClass{sClassCounter++}";
        skillMap[key] = value;
    }
}