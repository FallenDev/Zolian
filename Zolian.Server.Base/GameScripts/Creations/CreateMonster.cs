using Chaos.Common.Identity;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

using System.Numerics;
using Chaos.Extensions.Common;
using Darkages.GameScripts.Spells;

namespace Darkages.GameScripts.Creations;

[Script("Create Monster")]
public class CreateMonster(MonsterTemplate template, Area map) : MonsterCreateScript
{
    public override Monster Create()
    {
        var obj = new Monster
        {
            Template = template,
            BashTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(template.AttackSpeed)),
            AbilityTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(template.CastSpeed)),
            CastTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(template.CastSpeed)),
            WalkTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(template.MovementSpeed)),
            ObjectUpdateTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(ServerSetup.Instance.Config.GlobalBaseSkillDelay)),
            CastEnabled = true,
            Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId,
            Size = "",
            CurrentMapId = map.ID
        };

        SummonedLevel(obj);

        if (template.MonsterRace != MonsterRace.Dummy)
            LoadSkillScript("Assail", obj);
        if (template.Level > 10 && template.MonsterRace != MonsterRace.Dummy)
            MonsterSkillSet(obj);

        // Initialize the dictionary with the maximum level as the key and the hpMultiplier and mpMultiplier as the value
        var levelMultipliers = new SortedDictionary<int, (long hpMultiplier, long mpMultiplier)>
        {
            { 9, (115, 80)},
            { 19, (150, 100)},
            { 29, (200, 120)},
            { 39, (300, 150)},
            { 49, (400, 210)},
            { 59, (500, 350)},
            { 69, (600, 400)},
            { 79, (700, 550)},
            { 89, (800, 700) },
            { 99, (900, 750)},
            { 119, (1200, 800) },
            { 129, (1500, 900) },
            { 139, (2300, 1000) },
            { 149, (2700, 1100) },
            { 159, (3500, 1200) },
            { 169, (3900, 1300) },
            { 179, (4300, 1400) },
            { 189, (4900, 1500) },
            { 200, (5400, 1600) },
            { 209, (6700, 2000) },
            { 219, (7300, 2400) },
            { 229, (7800, 2800) },
            { 239, (8500, 3500) },
            { 250, (9150, 4000) }
            // Above level 250, the multipliers are generated
        };

        if (obj.Template.Level >= 250)
        {
            // Starting values for level > 249
            var currentHpMultiplier = 9150;
            var currentMpMultiplier = 4000;

            // Generate multipliers for levels above 249
            for (var level = 250; level <= 1000; level += 5)
            {
                currentHpMultiplier = (int)(currentHpMultiplier * 1.05);
                currentMpMultiplier = (int)(currentMpMultiplier * 1.05);
                levelMultipliers[level] = (currentHpMultiplier, currentMpMultiplier);
            }
        }

        // Find the first multiplier where the level is less than or equal to the key
        var (hpMultiplier, mpMultiplier) = levelMultipliers.First(x => obj.Template.Level <= x.Key).Value;

        obj.BaseHp = Generator.RandomMonsterStatVariance(obj.Template.Level * hpMultiplier);
        obj.BaseMp = Generator.RandomMonsterStatVariance(obj.Template.Level * mpMultiplier);
        obj._Mr = 50;

        MonsterSize(obj);
        MonsterArmorClass(obj);
        MonsterExperience(obj);
        MonsterAbility(obj);

        MonsterBaseAndSizeStats(obj);
        MonsterStatBoostOnPrimary(obj);
        MonsterStatBoostOnType(obj);
        AdjustOnArmorType(obj);
        SetElementalAlignment(obj);
        SetWalkEnabled(obj);
        SetMood(obj);
        SetSpawn(obj);

        if (obj.Map == null) return null;
        if (Area.IsAStarWall(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;
        if (Area.IsSpriteInLocationOnCreation(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;

        obj.AbandonedDate = DateTime.UtcNow;
        obj.Image = template.Image;
        obj.CurrentHp = obj.MaximumHp;
        obj.CurrentMp = obj.MaximumMp;

        Monster.InitScripting(template, map, obj);

        return obj;
    }

    private static void SummonedLevel(Monster obj)
    {
        if (obj.Template.SpawnType == SpawnQualifer.Summoned)
            obj.SummonerAdjLevel = (ushort)(obj.Summoner.ExpLevel / 3 + obj.Summoner.AbpLevel / 2);
    }

    private static void SetElementalAlignment(Monster obj)
    {
        switch (obj.Template.ElementType)
        {
            case ElementQualifer.Random:
                MonsterElementalAlignment(obj);
                break;
            case ElementQualifer.Defined:
                obj.DefenseElement = obj.Template.DefenseElement;
                obj.OffenseElement = obj.Template.OffenseElement;
                break;
            case ElementQualifer.None:
                obj.DefenseElement = ElementManager.Element.None;
                obj.OffenseElement = ElementManager.Element.None;
                break;
        }
    }

    private void SetWalkEnabled(Monster obj)
    {
        var pathQualifier = template.PathQualifer;

        if ((pathQualifier & PathQualifer.Wander) == PathQualifer.Wander || (pathQualifier & PathQualifer.Patrol) == PathQualifer.Patrol)
        {
            obj.WalkEnabled = true;
        }
        else if ((pathQualifier & PathQualifer.Fixed) == PathQualifer.Fixed)
        {
            obj.WalkEnabled = false;
        }
    }

    private void SetMood(Monster obj)
    {
        if (template.MoodType.MoodFlagIsSet(MoodQualifer.Aggressive) || template.MoodType.MoodFlagIsSet(MoodQualifer.VeryAggressive))
        {
            obj.Aggressive = true;
        }
        else if (template.MoodType.MoodFlagIsSet(MoodQualifer.Unpredicable))
        {
            var aggro = Generator.RandNumGen100() > 50;
            if (aggro) obj.Aggressive = true;
        }
        else
        {
            obj.Aggressive = false;
        }
    }

    private void SetSpawn(Monster obj)
    {
        switch (template.SpawnType)
        {
            case SpawnQualifer.Random:
                {
                    var x = Generator.GenerateMapLocation(map.Height);
                    var y = Generator.GenerateMapLocation(map.Width);
                    obj.Pos = new Vector2(x, y);
                    break;
                }
            case SpawnQualifer.Event:
                if (obj.Aggressive == false) return;
                obj.Pos = new Vector2(template.DefinedX, template.DefinedY);
                break;
            case SpawnQualifer.Summoned:
                if (obj.Summoner == null) return;
                obj.Pos = new Vector2(obj.Summoner.X + Random.Shared.Next(-3, 3), obj.Summoner.Y + Random.Shared.Next(-3, 3));
                obj.CurrentMapId = obj.Summoner.CurrentMapId;
                break;
            default:
                obj.Pos = new Vector2(template.DefinedX, template.DefinedY);
                break;
        }
    }

    private void MonsterSkillSet(Monster obj)
    {
        var monsterRaceActions = new Dictionary<MonsterRace, Action<Monster>>
        {
            [MonsterRace.Aberration] = AberrationSet,
            [MonsterRace.Animal] = AnimalSet,
            [MonsterRace.Aquatic] = AquaticSet,
            [MonsterRace.Beast] = BeastSet,
            [MonsterRace.Celestial] = CelestialSet,
            [MonsterRace.Construct] = ContructSet,
            [MonsterRace.Demon] = DemonSet,
            [MonsterRace.Dragon] = DragonSet,
            [MonsterRace.Bahamut] = BahamutDragonSet,
            [MonsterRace.Elemental] = ElementalSet,
            [MonsterRace.Fairy] = FairySet,
            [MonsterRace.Fiend] = FiendSet,
            [MonsterRace.Fungi] = FungiSet,
            [MonsterRace.Gargoyle] = GargoyleSet,
            [MonsterRace.Giant] = GiantSet,
            [MonsterRace.Goblin] = GoblinSet,
            [MonsterRace.Grimlok] = GrimlokSet,
            [MonsterRace.Humanoid] = HumanoidSet,
            [MonsterRace.ShapeShifter] = ShapeShifter,
            [MonsterRace.Insect] = InsectSet,
            [MonsterRace.Kobold] = KoboldSet,
            [MonsterRace.Magical] = MagicalSet,
            [MonsterRace.Mukul] = MukulSet,
            [MonsterRace.Ooze] = OozeSet,
            [MonsterRace.Orc] = OrcSet,
            [MonsterRace.Plant] = PlantSet,
            [MonsterRace.Reptile] = ReptileSet,
            [MonsterRace.Robotic] = RoboticSet,
            [MonsterRace.Shadow] = ShadowSet,
            [MonsterRace.Rodent] = RodentSet,
            [MonsterRace.Undead] = UndeadSet,
            // Dummy, LowerBeing, HigherBeing have no set action, so they are omitted here
        };

        if (monsterRaceActions.TryGetValue(template.MonsterRace, out var raceAction))
        {
            raceAction(obj);
        }

        // Load Random Generated Abilities to Monster
        if (!obj.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.ShapeShifter))
        {
            Assails(obj);

            if (!obj.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Bahamut))
            {
                BasicAbilities(obj);
                BeagSpells(obj);
                NormalSpells(obj);
                MorSpells(obj);
                ArdSpells(obj);
                MasterSpells(obj);
                JobSpells(obj);
            }
        }

        // Load Abilities from Template to Monster
        if (template.SkillScripts != null)
            foreach (var skillScriptStr in template.SkillScripts.Where(skillScriptStr => !string.IsNullOrWhiteSpace(skillScriptStr)))
                LoadSkillScript(skillScriptStr, obj);

        if (template.AbilityScripts != null)
            foreach (var abilityScriptStr in template.AbilityScripts.Where(abilityScriptStr => !string.IsNullOrWhiteSpace(abilityScriptStr)))
                LoadAbilityScript(abilityScriptStr, obj);

        if (template.SpellScripts == null) return;
        foreach (var spellScriptStr in template.SpellScripts.Where(spellScriptStr => !string.IsNullOrWhiteSpace(spellScriptStr)))
            LoadSpellScript(spellScriptStr, obj);
    }

    private static void MonsterSize(Monster obj)
    {
        var sizeRand = Generator.RandNumGen100();

        obj.Size = obj.Level switch
        {
            >= 1 and <= 11 => sizeRand switch
            {
                <= 35 => obj.Size = "Lessor",
                <= 60 => obj.Size = "Small",
                <= 85 => obj.Size = "Medium",
                <= 100 => obj.Size = "Large",
                _ => obj.Size = "Lessor"
            },
            <= 135 => sizeRand switch
            {
                <= 10 => obj.Size = "Lessor",
                <= 30 => obj.Size = "Small",
                <= 50 => obj.Size = "Medium",
                <= 70 => obj.Size = "Large",
                <= 90 => obj.Size = "Great",
                <= 100 => obj.Size = "Colossal",
                _ => obj.Size = "Lessor"
            },
            _ => sizeRand switch
            {
                <= 10 => obj.Size = "Lessor",
                <= 30 => obj.Size = "Small",
                <= 50 => obj.Size = "Medium",
                <= 70 => obj.Size = "Large",
                <= 90 => obj.Size = "Great",
                <= 95 => obj.Size = "Colossal",
                <= 100 => obj.Size = "Deity",
                _ => obj.Size = "Lessor"
            }
        };
    }

    private static void MonsterArmorClass(Monster obj)
    {
        // Initialize the dictionary with the maximum level as the key and the range of armor class as the value
        var levelArmorClassRange = new SortedDictionary<int, (int start, int end)>
        {
            { 3, (1, 4) },
            { 7, (5, 7) },
            { 11, (8, 13) },
            { 17, (14, 18) },
            { 24, (19, 26) },
            { 31, (27, 35) },
            { 37, (36, 45) },
            { 44, (46, 52) },
            { 51, (53, 59) },
            { 57, (55, 60) },
            { 64, (60, 65) },
            { 71, (65, 70) },
            { 77, (70, 75) },
            { 84, (75, 80) },
            { 91, (80, 85) },
            { 97, (85, 90) },
            { 104, (90, 95) },
            { 110, (95, 100) },
            { 116, (100, 110) },
            { 123, (110, 120) },
            { 129, (120, 130) },
            { 135, (130, 140) },
            { 140, (140, 150) },
            { 144, (150, 160) },
            { 149, (160, 170) },
            { 155, (170, 180) },
            { 160, (180, 190) },
            { 164, (190, 200) },
            { 169, (200, 205) },
            { 175, (205, 210) },
            { 180, (210, 215) },
            { 184, (215, 220) },
            { 190, (220, 225) },
            { 194, (225, 230) },
            { 200, (230, 240) },
            { 210, (235, 245) },
            { 220, (240, 250) },
            { 230, (245, 255) },
            { 240, (250, 260) },
            { 250, (255, 265) },
            { 260, (260, 270) },
            { 270, (265, 275) },
            { 280, (270, 280) },
            { 290, (275, 285) },
            { 300, (280, 295) },
            { 310, (290, 300) },
            { 320, (295, 305) },
            { 330, (300, 310) },
            { 340, (305, 315) },
            { 350, (310, 320) },
            { 360, (315, 325) },
            { 370, (320, 330) },
            { 380, (325, 335) },
            { 390, (330, 340) },
            { 400, (335, 350) },
            { 410, (345, 355) },
            { 420, (350, 360) },
            { 430, (355, 365) },
            { 440, (360, 370) },
            { 450, (365, 375) },
            { 460, (370, 380) },
            { 470, (375, 385) },
            { 480, (380, 390) },
            { 490, (385, 395) },
            { int.MaxValue, (390, 405) }
        };

        // Find the first range where the level is less than or equal to the key
        var (start, end) = levelArmorClassRange.First(x => obj.Template.Level <= x.Key).Value;

        obj.BonusAc = Generator.GenerateDeterminedNumberRange(start, end);
        var mrBonus = Generator.GenerateDeterminedNumberRange(start, end);
        mrBonus *= 2;
        obj.BonusMr = mrBonus;
    }

    private static void AdjustOnArmorType(Monster obj)
    {
        switch (obj.Template.MonsterArmorType)
        {
            default:
            case MonsterArmorType.Common:
                obj.BonusAc = (int)(obj.BonusAc * 0.75d);
                obj.BonusMr = (int)(obj.BonusMr * 0.75d);
                break;
            case MonsterArmorType.Tank:
                obj.BonusMr = (int)(obj.BonusMr * 0.30d);
                break;
            case MonsterArmorType.Caster:
                obj.BonusAc = (int)(obj.BonusAc * 0.35d);
                break;
        }
    }

    private static void MonsterExperience(Monster obj)
    {
        var levelExperienceRange = new SortedDictionary<int, (int start, int end)>
        {
            { 3, (200, 600) },
            { 7, (600, 1500) },
            { 11, (5000, 15000) },
            { 17, (9000, 17000) },
            { 24, (15000, 24000) },
            { 31, (17000, 35000) },
            { 37, (24000, 50000) },
            { 44, (35000, 58000) },
            { 51, (52000, 65000) },
            { 57, (60000, 90000) },
            { 64, (74000, 108000) },
            { 71, (94000, 120000) },
            { 77, (110000, 134000) },
            { 84, (125000, 150000) },
            { 91, (138000, 165000) },
            { 97, (250000, 295000) },
            { 104, (290000, 345000) },
            { 110, (350000, 467000) },
            { 116, (400000, 535000) },
            { 123, (615000, 750000) },
            { 129, (775000, 890000) },
            { 135, (910000, 1200000) },
            { 140, (1000000, 1300000) },
            { 144, (1250000, 1390000) },
            { 149, (1375000, 1450000) },
            { 155, (1400000, 1600000) },
            { 160, (1500000, 1700000) },
            { 164, (1600000, 1800000) },
            { 169, (1700000, 1900000) },
            { 175, (1800000, 2000000) },
            { 180, (1900000, 2100000) },
            { 184, (2200000, 2499999) },
            { 190, (2500000, 2899999) },
            { 194, (2900000, 3499999) },
            { 200, (3500000, 3899999) }
        };

        const int startLevel = 200;
        const int endLevel = 1000;
        // ToDo: Increment this in the future if higher levels need more experience
        var stepSize = 5;

        for (var level = startLevel + stepSize; level <= endLevel; level += stepSize)
        {
            // Retrieve the last entry's value
            var lastEntry = levelExperienceRange[level - stepSize];
            var newEntry = IncrementByFivePercent(lastEntry);
            levelExperienceRange.Add(level, newEntry);
        }

        var (start, end) = levelExperienceRange.First(x => obj.Template.Level <= x.Key).Value;
        var minXp = (int)(start * 0.9);
        var maxXp = (int)(end * 1.1);

        obj.Experience = (uint)Generator.GenerateDeterminedNumberRange(minXp, maxXp);
    }

    private static (int, int) IncrementByFivePercent((int, int) prevValues)
    {
        var newValue1 = (int)(prevValues.Item1 * 1.05);
        var newValue2 = (int)(prevValues.Item2 * 1.05);
        return (newValue1, newValue2);
    }

    private static void MonsterAbility(Monster obj)
    {
        if (obj.Level < 250)
        {
            obj.Ability = 0;
            return;
        }

        var levelExperienceRange = new SortedDictionary<int, (int start, int end)>
        {
            { 250, (20, 30) },
            { 256, (20, 30) },
            { 260, (25, 40) },
            { 264, (25, 40) },
            { 268, (35, 50) },
            { 272, (40, 60) },
            { 276, (45, 70) },
            { 280, (55, 80) },
            { 284, (60, 90) },
            { 288, (75, 110) },
            { 292, (80, 120) },
            { 296, (95, 140) },
            { 300, (105, 160) },
            { 304, (120, 180) },
            { 308, (135, 200) },
            { 312, (155, 230) },
            { 316, (165, 250) },
            { 320, (185, 280) },
            { 324, (210, 320) },
            { 328, (235, 360) },
            { 332, (260, 400) },
            { 336, (290, 450) },
            { 340, (320, 500) },
            { 344, (360, 560) },
            { 348, (400, 630) },
            { 352, (445, 700) },
            { 356, (495, 780) },
            { 360, (550, 870) },
            { 364, (610, 970) },
            { 368, (675, 1080) },
            { 372, (745, 1200) },
            { 376, (830, 1340) },
            { 380, (915, 1490) },
            { 384, (1015, 1660) },
            { 388, (1120, 1840) },
            { 392, (1240, 2050) },
            { 396, (1375, 2280) },
            { 400, (1520, 2540) },
            { 404, (1680, 2820) },
            { 408, (1860, 3140) },
            { 412, (2055, 3480) },
            { 416, (2270, 3870) },
            { 420, (2515, 4300) },
            { 424, (2780, 4780) },
            { 428, (3080, 5320) },
            { 432, (3410, 5910) },
            { 436, (3765, 6560) },
            { 440, (4165, 7290) },
            { 444, (4605, 8090) },
            { 448, (5095, 8990) },
            { 452, (5635, 9990) },
            { 456, (6240, 11100) },
            { 460, (6895, 12320) },
            { 464, (7640, 13700) },
            { 468, (8450, 15210) },
            { 472, (9355, 16890) },
            { 476, (10345, 18750) },
            { 480, (11455, 20820) },
            { 484, (12685, 23140) },
            { 488, (14050, 25710) },
            { 492, (15550, 28540) },
            { 496, (17225, 31700) },
            { 500, (19085, 35230) }
        };

        const int startLevel = 500;
        const int endLevel = 1000;
        // ToDo: Increment this in the future if higher levels need more experience
        var stepSize = 5;

        for (var level = startLevel + stepSize; level <= endLevel; level += stepSize)
        {
            // Retrieve the last entry's value
            var lastEntry = levelExperienceRange[level - stepSize];
            var newEntry = IncrementByFivePercent(lastEntry);
            levelExperienceRange.Add(level, newEntry);
        }

        var (start, end) = levelExperienceRange.First(x => obj.Template.Level <= x.Key).Value;
        var minXp = (int)(start * 0.9);
        var maxXp = (int)(end * 1.1);

        obj.Ability = (uint)Generator.GenerateDeterminedNumberRange(minXp, maxXp);
    }

    private static void MonsterElementalAlignment(Sprite obj)
    {
        var offRand = Generator.RandNumGen100();
        var defRand = Generator.RandNumGen100();

        switch (obj.Level)
        {
            case >= 1 and <= 11:
                obj.OffenseElement = offRand switch
                {
                    <= 18 => ElementManager.Element.None,
                    <= 36 => ElementManager.Element.Wind,
                    <= 54 => ElementManager.Element.Earth,
                    <= 72 => ElementManager.Element.Water,
                    <= 90 => ElementManager.Element.Fire,
                    <= 95 => ElementManager.Element.Void,
                    <= 100 => ElementManager.Element.Holy,
                    _ => obj.OffenseElement
                };
                obj.DefenseElement = defRand switch
                {
                    <= 18 => ElementManager.Element.None,
                    <= 36 => ElementManager.Element.Wind,
                    <= 54 => ElementManager.Element.Earth,
                    <= 72 => ElementManager.Element.Water,
                    <= 90 => ElementManager.Element.Fire,
                    <= 95 => ElementManager.Element.Void,
                    <= 100 => ElementManager.Element.Holy,
                    _ => obj.DefenseElement
                };
                break;
            case <= 98:
                obj.OffenseElement = offRand switch
                {
                    <= 20 => ElementManager.Element.Wind,
                    <= 40 => ElementManager.Element.Earth,
                    <= 60 => ElementManager.Element.Water,
                    <= 80 => ElementManager.Element.Fire,
                    <= 90 => ElementManager.Element.Void,
                    <= 100 => ElementManager.Element.Holy,
                    _ => obj.OffenseElement
                };
                obj.DefenseElement = defRand switch
                {
                    <= 20 => ElementManager.Element.Wind,
                    <= 40 => ElementManager.Element.Earth,
                    <= 60 => ElementManager.Element.Water,
                    <= 80 => ElementManager.Element.Fire,
                    <= 90 => ElementManager.Element.Void,
                    <= 100 => ElementManager.Element.Holy,
                    _ => obj.DefenseElement
                };
                break;
            case <= 250:
                obj.OffenseElement = offRand switch
                {
                    <= 10 => ElementManager.Element.Wind,
                    <= 20 => ElementManager.Element.Earth,
                    <= 30 => ElementManager.Element.Water,
                    <= 40 => ElementManager.Element.Fire,
                    <= 75 => ElementManager.Element.Void,
                    <= 100 => ElementManager.Element.Holy,
                    _ => obj.OffenseElement
                };
                obj.DefenseElement = defRand switch
                {
                    <= 10 => ElementManager.Element.Wind,
                    <= 20 => ElementManager.Element.Earth,
                    <= 30 => ElementManager.Element.Water,
                    <= 40 => ElementManager.Element.Fire,
                    <= 75 => ElementManager.Element.Void,
                    <= 100 => ElementManager.Element.Holy,
                    _ => obj.DefenseElement
                };
                break;
            default:
                obj.OffenseElement = offRand switch
                {
                    <= 10 => ElementManager.Element.Wind,
                    <= 20 => ElementManager.Element.Earth,
                    <= 30 => ElementManager.Element.Water,
                    <= 40 => ElementManager.Element.Fire,
                    <= 60 => ElementManager.Element.Void,
                    <= 80 => ElementManager.Element.Holy,
                    <= 90 => ElementManager.Element.Rage,
                    <= 100 => ElementManager.Element.Sorrow,
                    _ => obj.OffenseElement
                };
                obj.DefenseElement = defRand switch
                {
                    <= 10 => ElementManager.Element.Wind,
                    <= 20 => ElementManager.Element.Earth,
                    <= 30 => ElementManager.Element.Water,
                    <= 40 => ElementManager.Element.Fire,
                    <= 60 => ElementManager.Element.Void,
                    <= 80 => ElementManager.Element.Holy,
                    <= 90 => ElementManager.Element.Rage,
                    <= 100 => ElementManager.Element.Sorrow,
                    _ => obj.DefenseElement
                };
                break;
        }
    }

    private static void MonsterBaseAndSizeStats(Monster obj)
    {
        var statGen1 = Random.Shared.Next(1, 3);
        var statGen2 = Random.Shared.Next(2, 4);
        var statGen3 = Random.Shared.Next(4, 7);
        var statGen4 = Random.Shared.Next(6, 11);

        MonsterStartingStats(obj);

        var sizeStats = new Dictionary<string, Action<Monster>>
        {
            ["Lessor"] = monster =>
            {
                monster._Con -= statGen1;
                monster._Str -= statGen1;
                monster._Dex -= statGen1;
            },
            ["Small"] = monster =>
            {
                monster._Con -= statGen1;
                monster._Str -= statGen1;
                monster._Dex += statGen2;
            },
            ["Medium"] = monster =>
            {
                monster._Con += statGen2;
                monster._Str += statGen2;
                monster._Dex += statGen2;
            },
            ["Large"] = monster =>
            {
                monster._Con += statGen3;
                monster._Str += statGen2;
                monster._Dex -= statGen1;
                monster.BonusHp += (long)(monster.MaximumHp * 0.012);
                monster.Experience += (uint)(monster.Experience * 0.02);
                monster.Ability += (uint)(monster.Ability * 0.02);
            },
            ["Great"] = monster =>
            {
                monster._Con += statGen3;
                monster._Str += statGen3;
                monster._Dex -= statGen2;
                monster.BonusHp += (long)(monster.MaximumHp * 0.024);
                monster.Experience += (uint)(monster.Experience * 0.06);
                monster.Ability += (uint)(monster.Ability * 0.06);
            },
            ["Colossal"] = monster =>
            {
                monster._Con += statGen3;
                monster._Str += statGen3;
                monster._Dex += statGen3;
                monster.BonusHp += (long)(monster.MaximumHp * 0.036);
                monster.Experience += (uint)(monster.Experience * 0.10);
                monster.Ability += (uint)(monster.Ability * 0.10);
            },
            ["Deity"] = monster =>
            {
                monster._Con += statGen4;
                monster._Str += statGen4;
                monster._Dex += statGen4;
                monster.BonusHp += (long)(monster.MaximumHp * 0.048);
                monster.Experience += (uint)(monster.Experience * 0.15);
                monster.Ability += (uint)(monster.Ability * 0.15);
            },
        };

        if (!sizeStats.TryGetValue(obj.Size, out var statAdjustment))
        {
            obj.Size = "Lessor";
            statAdjustment = sizeStats[obj.Size];
        }

        statAdjustment(obj);

        obj._Hit = (int)(obj._Dex * 0.2);
        obj.BonusHit = 10 * (obj.Template.Level / 12);
        obj.BonusMr = 10 * (obj.Template.Level / 14);
    }

    private static void MonsterStartingStats(Monster obj)
    {
        // Level, Min, Max
        // Min is calculated as the lowest number of the previous level - 35%
        // Max is calculated as (level * 2) - 15% as to mimic stat generation
        var levelStatsRange = new SortedDictionary<int, (int start, int end)>
        {
            { 3, (2, 5) },
            { 7, (5, 12) },
            { 11, (7, 19) },
            { 17, (11, 29) },
            { 24, (16, 41) },
            { 31, (20, 53) },
            { 37, (24, 73) },
            { 44, (29, 75) },
            { 51, (33, 87) },
            { 57, (37, 97) },
            { 64, (42, 109) },
            { 71, (46, 121) },
            { 77, (50, 131) },
            { 84, (55, 143) },
            { 91, (59, 155) },
            { 99, (64, 168) },
            { 104, (68, 177) },
            { 110, (72, 187) },
            { 116, (75, 197) },
            { 123, (80, 209) },
            { 129, (84, 219) },
            { 135, (88, 230) },
            { 140, (91, 238) },
            { 144, (94, 245) },
            { 149, (97, 253) },
            { 155, (101, 264) },
            { 160, (104, 272) },
            { 164, (107, 279) },
            { 169, (110, 287) },
            { 175, (114, 298) },
            { 180, (117, 306) },
            { 184, (120, 313) },
            { 190, (124, 323) },
            { 194, (126, 330) },
            { 200, (130, 340) },
            { 215, (140, 366)},
            { 230, (150, 391)},
            { 245, (159, 417)},
            { 260, (169, 442)},
            { 275, (179, 468)},
            { 300, (195, 510)},
            { 315, (205, 536)},
            { 330, (215, 561)},
            { 345, (224, 587)},
            { 360, (234, 612)},
            { 375, (244, 638)},
            { 400, (260, 680)},
            { 415, (270, 706)},
            { 430, (280, 731)},
            { 445, (289, 757)},
            { 460, (299, 782)},
            { 475, (309, 808)},
            { 500, (325, 850)},
            { int.MaxValue, (345, 900) }
        };

        var statsToUpdate = new List<Action<int>>
        {
            x => obj._Str = x,
            x => obj._Int = x,
            x => obj._Wis = x,
            x => obj._Con = x,
            x => obj._Dex = x
        };

        foreach (var updateStat in statsToUpdate)
        {
            var (start, end) = levelStatsRange.First(x => obj.Template.Level <= x.Key).Value;
            updateStat(Generator.GenerateDeterminedNumberRange(start, end));
        }
    }

    private static void MonsterStatBoostOnPrimary(Monster obj)
    {
        if (obj.Level <= 20) return;

        var stat = Generator.RandomEnumValue<PrimaryStat>();
        obj.MajorAttribute = stat;

        var primaryStatBoosts = new Dictionary<PrimaryStat, Action<Monster>>
        {
            [PrimaryStat.STR] = monster =>
            {
                monster.BonusStr += (int)(monster._Str * 1.2);
                monster.BonusDmg += (int)(monster._Dmg * 1.2);
            },
            [PrimaryStat.INT] = monster =>
            {
                monster.BonusInt += (int)(monster._Int * 1.2);
                monster.BonusMr += (int)(monster._Mr * 1.2);
            },
            [PrimaryStat.WIS] = monster =>
            {
                monster.BonusWis += (int)(monster._Wis * 1.2);
                monster.BonusMp += (long)(monster.BaseMp * 1.2);
            },
            [PrimaryStat.CON] = monster =>
            {
                monster.BonusCon += (int)(monster._Con * 1.2);
                monster.BonusHp += (long)(monster.BaseHp * 1.2);
            },
            [PrimaryStat.DEX] = monster =>
            {
                monster.BonusDex += (int)(monster._Dex * 1.2);
                monster.BonusHit += (int)(monster._Hit * 1.2);
            }
        };

        if (primaryStatBoosts.TryGetValue(stat, out var boostAction))
        {
            boostAction(obj);
        }
    }

    private static void MonsterStatBoostOnType(Monster obj)
    {
        if (obj.Level <= 20) return;

        var monsterTypeBoosts = new Dictionary<MonsterType, Action<Monster>>
        {
            [MonsterType.Physical] = monster =>
            {
                monster.BonusStr += (int)(monster._Str * 1.2);
                monster.BonusDex += (int)(monster._Dex * 1.2);
                monster.BonusDmg += (int)(monster._Dmg * 1.2);
                monster.BonusHp += (long)(monster.MaximumHp * 0.012);
            },
            [MonsterType.Magical] = monster =>
            {
                monster.BonusInt += (int)(monster._Int * 1.2);
                monster.BonusWis += (int)(monster._Wis * 1.2);
                monster.BonusMr += (int)(monster._Mr * 1.2);
            },
            [MonsterType.GodlyStr] = monster =>
            {
                monster.BonusStr += monster._Str * 5;
                monster.BonusDex += (int)(monster._Dex * 2.4);
                monster.BonusDmg += monster._Dmg * 5;
            },
            [MonsterType.GodlyInt] = monster =>
            {
                monster.BonusInt += monster._Int * 5;
                monster.BonusWis += (int)(monster._Wis * 2.4);
                monster.BonusMr += monster._Mr * 5;
            },
            [MonsterType.GodlyWis] = monster =>
            {
                monster.BonusWis += monster._Wis * 5;
                monster.BonusMp += monster.BaseMp * 5;
            },
            [MonsterType.GodlyCon] = monster =>
            {
                monster.BonusCon += monster._Con * 5;
                monster.BonusHp += monster.BaseHp * 5;
            },
            [MonsterType.GodlyDex] = monster =>
            {
                monster.BonusDex += monster._Dex * 5;
                monster.BonusHit += monster._Hit * 5;
                monster.BonusDmg += monster._Dmg * 5;
            },
            [MonsterType.Above99P] = monster =>
            {
                monster.BonusStr += monster._Str * 5;
                monster.BonusDex += monster._Dex * 5;
                monster.BonusDmg += monster._Dmg * 5;
                monster.BonusHp += (long)(monster.MaximumHp * 0.03);
            },
            [MonsterType.Above99M] = monster =>
            {
                monster.BonusInt += monster._Int * 5;
                monster.BonusWis += monster._Wis * 5;
                monster.BonusMr += monster._Mr * 5;
            },
            [MonsterType.Above150P] = monster =>
            {
                monster.BonusStr += monster._Str * 7;
                monster.BonusDex += monster._Dex * 7;
                monster.BonusDmg += monster._Dmg * 7;
                monster.BonusHp += (long)(monster.MaximumHp * 0.05);
            },
            [MonsterType.Above150M] = monster =>
            {
                monster.BonusInt += monster._Int * 7;
                monster.BonusWis += monster._Wis * 7;
                monster.BonusMr += monster._Mr * 7;
            },
            [MonsterType.Above200P] = monster =>
            {
                monster.BonusStr += monster._Str * 9;
                monster.BonusDex += monster._Dex * 9;
                monster.BonusDmg += monster._Dmg * 9;
                monster.BonusHp += (long)(monster.MaximumHp * 0.07);
            },
            [MonsterType.Above200M] = monster =>
            {
                monster.BonusInt += monster._Int * 9;
                monster.BonusWis += monster._Wis * 9;
                monster.BonusMr += monster._Mr * 9;
            },
            [MonsterType.Above250P] = monster =>
            {
                monster.BonusStr += monster._Str * 10;
                monster.BonusDex += monster._Dex * 10;
                monster.BonusDmg += monster._Dmg * 10;
                monster.BonusHp += (long)(monster.MaximumHp * 0.08);
            },
            [MonsterType.Above250M] = monster =>
            {
                monster.BonusInt += monster._Int * 10;
                monster.BonusWis += monster._Wis * 10;
                monster.BonusMr += monster._Mr * 10;
            },
            [MonsterType.MasterStr] = monster =>
            {
                monster.BonusStr += monster._Str * 11;
                monster.BonusDex += monster._Dex * 10;
                monster.BonusDmg += monster._Dmg * 11;
            },
            [MonsterType.MasterInt] = monster =>
            {
                monster.BonusInt += monster._Int * 11;
                monster.BonusWis += monster._Wis * 10;
                monster.BonusMr += monster._Mr * 11;
            },
            [MonsterType.MasterWis] = monster =>
            {
                monster.BonusWis += monster._Wis * 11;
                monster.BonusMp += monster.BaseMp * 11;
            },
            [MonsterType.MasterCon] = monster =>
            {
                monster.BonusCon += monster._Con * 11;
                monster.BonusHp += monster.BaseHp * 11;
            },
            [MonsterType.MasterDex] = monster =>
            {
                monster.BonusDex += monster._Dex * 11;
                monster.BonusHit += monster._Hit * 11;
                monster.BonusDmg += monster._Dmg * 11;
            },
            [MonsterType.Forsaken] = monster =>
            {
                monster.BonusStr += monster._Str * 12;
                monster.BonusInt += monster._Int * 12;
                monster.BonusWis += monster._Wis * 12;
                monster.BonusCon += monster._Con * 12;
                monster.BonusDex += monster._Dex * 12;
                monster.BonusMr += monster._Mr * 12;
                monster.BonusHit += monster._Hit * 12;
                monster.BonusDmg += monster._Dmg * 12;
                monster.BonusHp += monster.BaseHp * 12;
                monster.BonusMp += monster.BaseMp * 12;
            },
            [MonsterType.Above300P] = monster =>
            {
                monster.BonusStr += monster._Str * 13;
                monster.BonusDex += monster._Dex * 13;
                monster.BonusDmg += monster._Dmg * 13;
                monster.BonusHp += (long)(monster.MaximumHp * 0.11);
            },
            [MonsterType.Above300M] = monster =>
            {
                monster.BonusInt += monster._Int * 13;
                monster.BonusWis += monster._Wis * 13;
                monster.BonusMr += monster._Mr * 13;
            },
            [MonsterType.Above350P] = monster =>
            {
                monster.BonusStr += monster._Str * 14;
                monster.BonusDex += monster._Dex * 14;
                monster.BonusDmg += monster._Dmg * 14;
                monster.BonusHp += (long)(monster.MaximumHp * 0.13);
            },
            [MonsterType.Above350M] = monster =>
            {
                monster.BonusInt += monster._Int * 14;
                monster.BonusWis += monster._Wis * 14;
                monster.BonusMr += monster._Mr * 14;
            },
            [MonsterType.Above400P] = monster =>
            {
                monster.BonusStr += monster._Str * 16;
                monster.BonusDex += monster._Dex * 16;
                monster.BonusDmg += monster._Dmg * 16;
                monster.BonusHp += (long)(monster.MaximumHp * 0.15);
            },
            [MonsterType.Above400M] = monster =>
            {
                monster.BonusInt += monster._Int * 16;
                monster.BonusWis += monster._Wis * 16;
                monster.BonusMr += monster._Mr * 16;
            },
            [MonsterType.Above450P] = monster =>
            {
                monster.BonusStr += monster._Str * 19;
                monster.BonusDex += monster._Dex * 19;
                monster.BonusDmg += monster._Dmg * 19;
                monster.BonusHp += (long)(monster.MaximumHp * 0.17);
            },
            [MonsterType.Above450M] = monster =>
            {
                monster.BonusInt += monster._Int * 19;
                monster.BonusWis += monster._Wis * 19;
                monster.BonusMr += monster._Mr * 19;
            },
            [MonsterType.Above500P] = monster =>
            {
                monster.BonusStr += monster._Str * 21;
                monster.BonusDex += monster._Dex * 21;
                monster.BonusDmg += monster._Dmg * 21;
                monster.BonusHp += (long)(monster.MaximumHp * 0.20);
            },
            [MonsterType.Above500M] = monster =>
            {
                monster.BonusInt += monster._Int * 21;
                monster.BonusWis += monster._Wis * 21;
                monster.BonusMr += monster._Mr * 21;
            },
            [MonsterType.DivineStr] = monster =>
            {
                monster.BonusStr += monster._Str * 25;
                monster.BonusDex += monster._Dex * 22;
                monster.BonusDmg += monster._Dmg * 22;
            },
            [MonsterType.DivineInt] = monster =>
            {
                monster.BonusInt += monster._Int * 25;
                monster.BonusWis += monster._Wis * 22;
                monster.BonusMr += monster._Mr * 25;
            },
            [MonsterType.DivineWis] = monster =>
            {
                monster.BonusWis += monster._Wis * 25;
                monster.BonusMp += monster.BaseMp * 25;
            },
            [MonsterType.DivineCon] = monster =>
            {
                monster.BonusCon += monster._Con * 25;
                monster.BonusHp += monster.BaseHp * 25;
            },
            [MonsterType.DivineDex] = monster =>
            {
                monster.BonusDex += monster._Dex * 25;
                monster.BonusHit += monster._Hit * 25;
                monster.BonusDmg += monster._Dmg * 25;
            },
            [MonsterType.MiniBoss] = monster =>
            {
                monster.BonusStr += monster._Str * 20;
                monster.BonusInt += monster._Int * 20;
                monster.BonusWis += monster._Wis * 20;
                monster.BonusCon += monster._Con * 20;
                monster.BonusDex += monster._Dex * 20;
                monster.BonusMr += monster._Mr * 20;
                monster.BonusHit += monster._Hit * 20;
                monster.BonusDmg += monster._Dmg * 20;
                monster.BonusHp += monster.BaseHp * 20;
                monster.BonusMp += monster.BaseMp * 20;
            },
            [MonsterType.Boss] = monster =>
            {
                monster.BonusStr += monster._Str * 30;
                monster.BonusInt += monster._Int * 30;
                monster.BonusWis += monster._Wis * 30;
                monster.BonusCon += monster._Con * 30;
                monster.BonusDex += monster._Dex * 30;
                monster.BonusMr += monster._Mr * 20;
                monster.BonusHit += monster._Hit * 20;
                monster.BonusDmg += monster._Dmg * 20;
                monster.BonusHp += monster.BaseHp * 27;
                monster.BonusMp += monster.BaseMp * 27;
            },
        };

        if (monsterTypeBoosts.TryGetValue(obj.Template.MonsterType, out var boostAction))
        {
            boostAction(obj);
        }
    }

    private void LoadSkillScript(string skillScriptStr, Monster obj)
    {
        try
        {
            if (!ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(skillScriptStr, out var script)) return;
            var scripts = ScriptManager.Load<SkillScript>(script.ScriptName, Skill.Create(1, script));

            if (scripts == null)
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

            if (!scripts.TryGetValue(script.ScriptName, out var skillScript)) return;
            skillScript.Skill.NextAvailableUse = DateTime.UtcNow;
            skillScript.Skill.Level = 100;
            obj.SkillScripts.Add(skillScript);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private void LoadSpellScript(string spellScriptStr, Monster obj, bool primary = false)
    {
        try
        {
            if (!ServerSetup.Instance.GlobalSpellTemplateCache.TryGetValue(spellScriptStr, out var script)) return;
            var scripts = ScriptManager.Load<SpellScript>(spellScriptStr,
                Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache[spellScriptStr]));

            if (scripts == null)
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {spellScriptStr}\n");
                return;
            }

            if (!scripts.TryGetValue(script.ScriptName, out var spellScript)) return;
            {
                spellScript.Spell.Level = 100;
                spellScript.IsScriptDefault = primary;
                obj.SpellScripts.Add(spellScript);
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private void LoadAbilityScript(string skillScriptStr, Monster obj)
    {
        try
        {
            if (!ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(skillScriptStr, out var script)) return;
            var scripts = ScriptManager.Load<SkillScript>(script.ScriptName, Skill.Create(1, script));

            if (scripts == null)
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

            if (!scripts.TryGetValue(script.ScriptName, out var skillScript)) return;
            skillScript.Skill.NextAvailableUse = DateTime.UtcNow;
            skillScript.Skill.Level = 100;
            obj.AbilityScripts.Add(skillScript);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    /// <summary>
    /// Give monsters random assails depending on their level
    /// </summary>
    private void Assails(Monster monster)
    {
        var skillList = monster.Template.Level switch
        {
            <= 11 => ["Onslaught", "Assault", "Clobber", "Bite", "Claw"],
            <= 50 =>
            [
                "Double Punch", "Punch", "Clobber x2", "Onslaught", "Thrust",
                "Wallop", "Assault", "Clobber", "Bite", "Claw", "Stomp", "Tail Slap"
            ],
            _ => new List<string>
            {
                "Double Punch", "Punch", "Thrash", "Clobber x2", "Onslaught",
                "Thrust", "Wallop", "Assault", "Clobber", "Slash", "Bite", "Claw",
                "Head Butt", "Mule Kick", "Stomp", "Tail Slap"
            }
        };

        var skillCount = Math.Round(monster.Level / 30d) + 1;
        skillCount = Math.Min(skillCount, 12); // Max 12 skills regardless of level
        var randomIndices = Enumerable.Range(0, skillList.Count).ToList();

        for (var i = 0; i < skillCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var skill = skillList[randomIndices[index]];
            var check = monster.SkillScripts.Any(script => script.Skill.Template.ScriptName == skill);

            if (!check)
                LoadSkillScript(skill, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same skill again
        }
    }

    /// <summary>
    /// Give monsters additional random abilities depending on their level
    /// This is additional to the monster racial abilities
    /// </summary>
    private void BasicAbilities(Monster monster)
    {
        if (monster.Template.Level <= 11) return;

        var skillList = monster.Template.Level switch
        {
            <= 25 =>
            [
                "Stab", "Dual Slice", "Wind Slice", "Wind Blade"
            ],
            <= 60 =>
            [
                "Claw Fist", "Cross Body Punch", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike",
                "Wolf Fang Fist", "Stab", "Stab'n Twist", "Stab Twice",
                "Desolate", "Dual Slice", "Rush", "Wind Slice", "Beag Suain", "Wind Blade", "Double-Edged Dance",
                "Bite'n Shake", "Howl'n Call", "Death From Above",
                "Pounce", "Roll Over", "Corrosive Touch"
            ],
            <= 75 =>
            [
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
                "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist",
                "Stab", "Stab'n Twist", "Stab Twice", "Desolate", "Dual Slice", "Lullaby Strike", "Rush", "Sever",
                "Wind Slice", "Beag Suain", "Charge",
                "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Bite'n Shake", "Howl'n Call",
                "Death From Above", "Pounce", "Roll Over",
                "Swallow Whole", "Tentacle", "Corrosive Touch"
            ],
            <= 120 =>
            [
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
                "Krane Kick", "Palm Heel Strike",
                "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice", "Titan's Cleave", "Desolate",
                "Dual Slice", "Lullaby Strike", "Rush",
                "Sever", "Wind Slice", "Beag Suain", "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance",
                "Ebb'n Flow", "Retribution", "Flame Thrower",
                "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole", "Tentacle",
                "Corrosive Touch", "Tantalizing Gaze"
            ],
            _ => new List<string>
            {
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike",
                "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice", "Titan's Cleave", "Desolate", "Dual Slice", "Lullaby Strike", "Rush",
                "Sever", "Wind Slice", "Beag Suain", "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Retribution", "Flame Thrower",
                "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole", "Tentacle", "Corrosive Touch", "Tantalizing Gaze"
            }
        };

        var skillCount = Math.Round(monster.Level / 30d) + 1;
        skillCount = Math.Min(skillCount, 5); // Max 5 abilities regardless of level
        var randomIndices = Enumerable.Range(0, skillList.Count).ToList();

        for (var i = 0; i < skillCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var skill = skillList[randomIndices[index]];
            var check = monster.AbilityScripts.Any(script => script.Skill.Template.ScriptName == skill);

            if (!check)
                LoadAbilityScript(skill, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same ability again
        }
    }

    /// <summary>
    /// Give beag spells randomly depending on their level
    /// </summary>
    private void BeagSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case < 11:
            case > 20:
                return;
        }

        var spellList = new[]
        {
            "Beag Srad", "Beag Sal", "Beag Athar", "Beag Creag", "Beag Dorcha", "Beag Eadrom", "Beag Puinsein", "Beag Cradh", 
            "Ao Beag Cradh"
        };

        var spellCount = Math.Round(monster.Level / 20d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give normal spells randomly depending on their level
    /// </summary>
    private void NormalSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case <= 20:
            case > 50:
                return;
        }

        var spellList = new[]
        {
            "Srad", "Sal", "Athar", "Creag", "Dorcha", "Eadrom", "Puinsein", "Cradh", "Ao Cradh"
        };

        var spellCount = Math.Round(monster.Level / 30d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give mor spells randomly depending on their level
    /// </summary>
    private void MorSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case <= 50:
            case > 80:
                return;
        }

        var spellList = new[]
        {
            "Mor Srad", "Mor Sal", "Mor Athar", "Mor Creag", "Mor Dorcha", "Mor Eadrom", "Mor Puinsein", "Mor Cradh", 
            "Fas Nadur", "Blind", "Pramh", "Ao Mor Cradh"
        };

        var spellCount = Math.Round(monster.Level / 70d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give ard spells randomly depending on their level
    /// </summary>
    private void ArdSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case <= 80:
            case > 120:
                return;
        }

        var spellList = new[]
        {
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh", 
            "Mor Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh"
        };

        var spellCount = Math.Round(monster.Level / 100d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give master spells randomly depending on their level
    /// </summary>
    private void MasterSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case <= 120:
            case > 250:
                return;
        }

        var spellList = new[]
        {
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh", 
            "Ard Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh", "Ao Puinsein", "Dark Chain", "Defensive Stance"
        };

        var spellCount = Math.Round(monster.Level / 150d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give job level spells randomly depending on their level
    /// </summary>
    private void JobSpells(Monster monster)
    {
        if (monster.Template.Level <= 250) return;

        var spellList = new List<string>
        {
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", 
            "Croich Beag Cradh", "Ard Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh", "Ao Puinsein", 
            "Dark Chain", "Defensive Stance"
        };

        if (monster.Template.Level >= 500)
        {
            spellList.AddRange(["Uas Athar", "Uas Creag", "Uas Sal", "Uas Srad", "Uas Dorcha", "Uas Eadrom", 
                "Croich Mor Cradh", "Penta Seal", "Decay"]);
        }

        var spellCount = Math.Round(monster.Level / 200d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Count).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) // All abilities have been assigned
            {
                break;
            }

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    private void AberrationSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Aberration)) return;
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Lullaby Strike", "Vampiric Slash" };
        var spellList = new List<string> { "Spectral Shield", "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void AnimalSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Animal)) return;
        var skillList = new List<string> { "Bite", "Claw" };
        var abilityList = new List<string> { "Howl'n Call", "Bite'n Shake" };
        var spellList = new List<string> { "Defensive Stance" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void AquaticSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Aquatic)) return;
        var skillList = new List<string> { "Bite", "Tail Slap" };
        var abilityList = new List<string> { "Bubble Burst", "Swallow Whole" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void BeastSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Beast)) return;
        var skillList = new List<string> { "Bite", "Claw" };
        var abilityList = new List<string> { "Bite'n Shake", "Pounce", "Poison Talon" };
        var spellList = new List<string> { "Asgall" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void CelestialSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Celestial)) return;
        var skillList = new List<string> { "Thrash", "Divine Thrust", "Slash", "Wallop" };
        var abilityList = new List<string> { "Titan's Cleave", "Shadow Step", "Entice", "Smite" };
        var spellList = new List<string> { "Deireas Faileas", "Asgall", "Perfect Defense", "Dion", "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void ContructSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Construct)) return;
        var skillList = new List<string> { "Stomp" };
        var abilityList = new List<string> { "Titan's Cleave", "Earthly Delights" };
        var spellList = new List<string> { "Dion", "Defensive Stance" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void DemonSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Demon)) return;
        var skillList = new List<string> { "Onslaught", "Two-Handed Attack", "Dual Wield", "Slash", "Thrash" };
        var abilityList = new List<string> { "Titan's Cleave", "Sever", "Earthly Delights", "Entice", "Atlantean Weapon" };
        var spellList = new List<string> { "Asgall", "Perfect Defense", "Dion" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void DragonSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dragon)) return;
        var skillList = new List<string> { "Thrash", "Ambidextrous", "Slash", "Claw", "Tail Slap" };
        var abilityList = new List<string> { "Titan's Cleave", "Sever", "Earthly Delights", "Hurricane Kick" };
        var spellList = new List<string> { "Asgall", "Perfect Defense", "Dion", "Deireas Faileas" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void BahamutDragonSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Bahamut)) return;
        var skillList = new List<string> { "Fire Wheel", "Thrash", "Ambidextrous", "Slash", "Claw" };
        var abilityList = new List<string> { "Megaflare", "Lava Armor", "Ember Strike", "Silent Siren" };
        var spellList = new List<string> { "Heavens Fall", "Liquid Hell", "Ao Sith Gar" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void ElementalSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Elemental)) return;
        var skillList = new List<string> { "Onslaught", "Assault" };
        var abilityList = new List<string> { "Atlantean Weapon", "Elemental Bane" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void FairySet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Fairy)) return;
        var skillList = new List<string> { "Ambidextrous", "Divine Thrust", "Clobber x2" };
        var abilityList = new List<string> { "Earthly Delights", "Claw Fist", "Lullaby Strike" };
        var spellList = new List<string> { "Asgall", "Spectral Shield", "Deireas Faileas" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void FiendSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Fiend)) return;
        var skillList = new List<string> { "Punch", "Double Punch" };
        var abilityList = new List<string> { "Stab", "Stab Twice" };
        var spellList = new List<string> { "Blind" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void FungiSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Fungi)) return;
        var skillList = new List<string> { "Wallop", "Clobber" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade", "Vampiric Slash" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void GargoyleSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Gargoyle)) return;
        var skillList = new List<string> { "Slash" };
        var abilityList = new List<string> { "Palm Heel Strike" };
        var spellList = new List<string> { "Mor Dion" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void GiantSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Giant)) return;
        var skillList = new List<string> { "Stomp", "Head Butt" };
        var abilityList = new List<string> { "Golden Lair", "Double-Edged Dance" };
        var spellList = new List<string> { "Silence", "Pramh" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void GoblinSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Goblin)) return;
        var skillList = new List<string> { "Assault", "Clobber", "Wallop" };
        var abilityList = new List<string> { "Wind Slice", "Wind Blade" };
        var spellList = new List<string> { "Beag Puinsein" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void GrimlokSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Grimlok)) return;
        var skillList = new List<string> { "Wallop", "Clobber" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade" };
        var spellList = new List<string> { "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void HumanoidSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Humanoid)) return;
        var skillList = new List<string> { "Thrust", "Thrash", "Wallop" };
        var abilityList = new List<string> { "Camouflage", "Adrenaline" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void ShapeShifter(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.ShapeShifter)) return;
        var skillList = new List<string> { "Thrust", "Thrash", "Wallop" };
        var spellList = new List<string> { "Spring Trap", "Snare Trap", "Blind", "Prahm" };
        MonsterLoader(skillList, [], spellList, monster);
    }

    private void InsectSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Insect)) return;
        var skillList = new List<string> { "Bite" };
        var abilityList = new List<string> { "Corrosive Touch" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void KoboldSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Kobold)) return;
        var skillList = new List<string> { "Clobber x2", "Assault" };
        var abilityList = new List<string> { "Ebb'n Flow", "Stab", "Stab'n Twist" };
        var spellList = new List<string> { "Blind" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void MagicalSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Magical)) return;
        var spellList = new List<string> { "Aite", "Mor Fas Nadur", "Deireas Faileas" };
        MonsterLoader([], [], spellList, monster);
    }

    private void MukulSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Mukul)) return;
        var skillList = new List<string> { "Clobber", "Mule Kick", "Onslaught" };
        var abilityList = new List<string> { "Krane Kick", "Wolf Fang Fist", "Flurry", "Desolate" };
        var spellList = new List<string> { "Perfect Defense" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void OozeSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Ooze)) return;
        var skillList = new List<string> { "Wallop", "Clobber", "Clobber x2" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade", "Vampiric Slash", "Retribution" };
        var spellList = new List<string> { "Asgall" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void OrcSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Orc)) return;
        var skillList = new List<string> { "Clobber", "Thrash" };
        var abilityList = new List<string> { "Titan's Cleave", "Corrosive Touch" };
        var spellList = new List<string> { "Asgall" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void PlantSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Plant)) return;
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Corrosive Touch" };
        var spellList = new List<string> { "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    private void ReptileSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Reptile)) return;
        var skillList = new List<string> { "Tail Slap", "Head Butt" };
        var abilityList = new List<string> { "Pounce", "Death From Above" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void RoboticSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Robotic)) return;
        var spellList = new List<string> { "Mor Dion", "Perfect Defense" };
        MonsterLoader([], [], spellList, monster);
    }

    private void ShadowSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Shadow)) return;
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Lullaby Strike", "Vampiric Slash" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void RodentSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Rodent)) return;
        var skillList = new List<string> { "Bite", "Assault" };
        var abilityList = new List<string> { "Rush" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void UndeadSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead)) return;
        var skillList = new List<string> { "Wallop" };
        var abilityList = new List<string> { "Corrosive Touch", "Retribution" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private void MonsterLoader(List<string> skills, List<string> abilities, List<string> spells, Monster monster)
    {
        if (skills.Count != 0)
            foreach (var skill in skills)
            {
                LoadSkillScript(skill, monster);
            }

        if (abilities.Count != 0)
            foreach (var ability in abilities)
            {
                LoadAbilityScript(ability, monster);
            }

        if (spells.Count == 0) return;
        foreach (var spell in spells)
        {
            LoadSpellScript(spell, monster);
        }
    }
}