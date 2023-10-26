using System.Numerics;

using Chaos.Common.Identity;

using Darkages.Common;
using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Darkages.GameScripts.Creations;

[Script("Create Monster")]
public class CreateMonster(MonsterTemplate template, Area map) : MonsterCreateScript
{
    public override Monster Create()
    {
        if (template.CastSpeed <= 6000) template.CastSpeed = 6000;
        if (template.AttackSpeed <= 500) template.AttackSpeed = 500;
        if (template.MovementSpeed <= 500) template.MovementSpeed = 500;
        if (template.Level <= 1) template.Level = 1;

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

        LoadSkillScript("Assail", obj);
        MonsterSkillSet(obj);

        // Initialize the dictionary with the maximum level as the key and the hpMultiplier and mpMultiplier as the value
        var levelMultipliers = new SortedDictionary<int, (int hpMultiplier, int mpMultiplier)>
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
            { 249, (9150, 4000) },
            { 255, (40000, 2000) },
            { int.MaxValue, (100000, 15000) } // default case for level > 450
        };

        // Find the first multiplier where the level is less than or equal to the key
        var (hpMultiplier, mpMultiplier) = levelMultipliers.First(x => obj.Template.Level <= x.Key).Value;

        obj.BaseHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * hpMultiplier);
        obj.BaseMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * mpMultiplier);
        obj._Mr = 50;

        MonsterSize(obj);
        MonsterArmorClass(obj);
        MonsterExperience(obj);
        MonsterAbility(obj);

        MonsterBaseAndSizeStats(obj);
        MonsterStatBoostOnPrimary(obj);
        MonsterStatBoostOnType(obj);

        obj.CurrentHp = obj.MaximumHp;
        obj.CurrentMp = obj.MaximumMp;

        SetElementalAlignment(obj);
        SetWalkEnabled(obj);
        SetMood(obj);
        SetSpawn(obj);

        if (obj.Map == null) return null;
        if (obj.Map.IsAStarWall(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;
        if (obj.Map.IsSpriteInLocationOnCreation(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;

        obj.AbandonedDate = DateTime.UtcNow;

        obj.Image = template.ImageVarience > 0
            ? (ushort)Random.Shared.Next(template.Image, template.Image + template.ImageVarience)
            : template.Image;

        return obj;
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
            [MonsterRace.Contruct] = ContructSet,
            [MonsterRace.Demon] = DemonSet,
            [MonsterRace.Dragon] = DragonSet,
            [MonsterRace.Elemental] = ElementalSet,
            [MonsterRace.Fairy] = FairySet,
            [MonsterRace.Fiend] = FiendSet,
            [MonsterRace.Fungi] = FungiSet,
            [MonsterRace.Gargoyle] = GargoyleSet,
            [MonsterRace.Giant] = GiantSet,
            [MonsterRace.Goblin] = GoblinSet,
            [MonsterRace.Grimlok] = GrimlokSet,
            [MonsterRace.Humanoid] = HumanoidSet,
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
            // Dummy, Inanimate, LowerBeing, HigherBeing have no set action, so they are omitted here
        };

        if (monsterRaceActions.TryGetValue(template.MonsterRace, out var raceAction))
        {
            raceAction(obj);
        }

        // Load Random Generated Abilities to Monster
        Assails(obj);
        BasicAbilities(obj);
        BeagSpells(obj);
        NormalSpells(obj);
        MorSpells(obj);
        ArdSpells(obj);
        MasterSpells(obj);

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
                <= 60 and > 35 => obj.Size = "Small",
                <= 85 and > 60 => obj.Size = "Medium",
                <= 100 and > 85 => obj.Size = "Large",
                _ => obj.Size = "Lessor"
            },
            <= 98 => sizeRand switch
            {
                <= 10 => obj.Size = "Lessor",
                <= 30 and > 10 => obj.Size = "Small",
                <= 50 and > 30 => obj.Size = "Medium",
                <= 70 and > 50 => obj.Size = "Large",
                <= 90 and > 70 => obj.Size = "Great",
                <= 100 and > 90 => obj.Size = "Colossal",
                _ => obj.Size = "Lessor"
            },
            _ => sizeRand switch
            {
                <= 10 => obj.Size = "Lessor",
                <= 30 and > 10 => obj.Size = "Small",
                <= 50 and > 30 => obj.Size = "Medium",
                <= 70 and > 50 => obj.Size = "Large",
                <= 90 and > 70 => obj.Size = "Great",
                <= 95 and > 90 => obj.Size = "Colossal",
                <= 100 and > 95 => obj.Size = "Deity",
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
            { 300, (280, 300) },
            { int.MaxValue, (290, 350) } // default case for level > 200
        };

        // Find the first range where the level is less than or equal to the key
        var (start, end) = levelArmorClassRange.First(x => obj.Template.Level <= x.Key).Value;

        obj.BonusAc = Generator.GenerateDeterminedNumberRange(start, end);
        var mrBonus = Generator.GenerateDeterminedNumberRange(start, end);
        mrBonus *= 2;
        obj.BonusMr = mrBonus;
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
            { 200, (3500000, 3899999) },
            { 204, (3500000, 3899999) },
            { 208, (3675000, 4172999) },
            { 212, (3861250, 4463519) },
            { 216, (4054312, 4774565) },
            { 220, (4255028, 5111586) },
            { 224, (4463780, 5470397) },
            { 228, (4680824, 5850126) },
            { 232, (4906865, 6258125) },
            { 236, (5142408, 6693074) },
            { 240, (5396038, 7157987) },
            { 244, (5666840, 7654052) },
            { 248, (5955332, 8183075) },
            { 252, (6262104, 8746983) },
            { int.MaxValue, (7000000, 10000000) }
        };

        var (start, end) = levelExperienceRange.First(x => obj.Template.Level <= x.Key).Value;

        obj.Experience = (uint)Generator.GenerateDeterminedNumberRange(start, end);
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
            { 252, (1, 3) },
            { 256, (1, 3) },
            { 260, (1, 4) },
            { 264, (1, 4) },
            { 268, (2, 5) },
            { 272, (2, 6) },
            { 276, (2, 7) },
            { 280, (3, 8) },
            { 284, (3, 9) },
            { 288, (4, 11) },
            { 292, (4, 12) },
            { 296, (5, 14) },
            { 300, (5, 16) },
            { 304, (6, 18) },
            { 308, (7, 20) },
            { 312, (8, 23) },
            { 316, (8, 25) },
            { 320, (9, 28) },
            { 324, (10, 32) },
            { 328, (11, 36) },
            { 332, (12, 40) },
            { 336, (13, 45) },
            { 340, (14, 50) },
            { 344, (16, 56) },
            { 348, (17, 63) },
            { 352, (19, 70) },
            { 356, (21, 78) },
            { 360, (23, 87) },
            { 364, (25, 97) },
            { 368, (27, 108) },
            { 372, (29, 120) },
            { 376, (32, 134) },
            { 380, (34, 149) },
            { 384, (37, 166) },
            { 388, (40, 184) },
            { 392, (43, 205) },
            { 396, (47, 228) },
            { 400, (50, 254) },
            { 404, (54, 282) },
            { 408, (58, 314) },
            { 412, (63, 348) },
            { 416, (67, 387) },
            { 420, (73, 430) },
            { 424, (78, 478) },
            { 428, (84, 532) },
            { 432, (91, 591) },
            { 436, (97, 656) },
            { 440, (104, 729) },
            { 444, (112, 809) },
            { 448, (120, 899) },
            { 452, (128, 999) },
            { 456, (138, 1110) },
            { 460, (147, 1232) },
            { 464, (158, 1370) },
            { 468, (169, 1521) },
            { 472, (182, 1689) },
            { 476, (194, 1875) },
            { 480, (209, 2082) },
            { 484, (223, 2314) },
            { 488, (239, 2571) },
            { 492, (256, 2854) },
            { 496, (275, 3170) },
            { 500, (294, 3523) }
        };

        var (start, end) = levelExperienceRange.First(x => obj.Template.Level <= x.Key).Value;

        obj.Ability = (uint)Generator.GenerateDeterminedNumberRange(start, end);
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
                    <= 36 and > 18 => ElementManager.Element.Wind,
                    <= 54 and > 36 => ElementManager.Element.Earth,
                    <= 72 and > 54 => ElementManager.Element.Water,
                    <= 90 and > 72 => ElementManager.Element.Fire,
                    <= 95 and > 90 => ElementManager.Element.Void,
                    <= 100 and > 95 => ElementManager.Element.Holy,
                    _ => obj.OffenseElement
                };
                obj.DefenseElement = defRand switch
                {
                    <= 18 => ElementManager.Element.None,
                    <= 36 and > 18 => ElementManager.Element.Wind,
                    <= 54 and > 36 => ElementManager.Element.Earth,
                    <= 72 and > 54 => ElementManager.Element.Water,
                    <= 90 and > 72 => ElementManager.Element.Fire,
                    <= 95 and > 90 => ElementManager.Element.Void,
                    <= 100 and > 95 => ElementManager.Element.Holy,
                    _ => obj.DefenseElement
                };
                break;
            case <= 98:
                obj.OffenseElement = offRand switch
                {
                    <= 20 => ElementManager.Element.Wind,
                    <= 40 and > 20 => ElementManager.Element.Earth,
                    <= 60 and > 40 => ElementManager.Element.Water,
                    <= 80 and > 60 => ElementManager.Element.Fire,
                    <= 90 and > 80 => ElementManager.Element.Void,
                    <= 100 and > 90 => ElementManager.Element.Holy,
                    _ => obj.OffenseElement
                };
                obj.DefenseElement = defRand switch
                {
                    <= 20 => ElementManager.Element.Wind,
                    <= 40 and > 20 => ElementManager.Element.Earth,
                    <= 60 and > 40 => ElementManager.Element.Water,
                    <= 80 and > 60 => ElementManager.Element.Fire,
                    <= 90 and > 80 => ElementManager.Element.Void,
                    <= 100 and > 90 => ElementManager.Element.Holy,
                    _ => obj.DefenseElement
                };
                break;
            default:
                obj.OffenseElement = offRand switch
                {
                    <= 10 => ElementManager.Element.Wind,
                    <= 20 and > 10 => ElementManager.Element.Earth,
                    <= 30 and > 20 => ElementManager.Element.Water,
                    <= 40 and > 30 => ElementManager.Element.Fire,
                    <= 75 and > 40 => ElementManager.Element.Void,
                    <= 100 and > 75 => ElementManager.Element.Holy,
                    _ => obj.OffenseElement
                };
                obj.DefenseElement = defRand switch
                {
                    <= 10 => ElementManager.Element.Wind,
                    <= 20 and > 10 => ElementManager.Element.Earth,
                    <= 30 and > 20 => ElementManager.Element.Water,
                    <= 40 and > 30 => ElementManager.Element.Fire,
                    <= 75 and > 40 => ElementManager.Element.Void,
                    <= 100 and > 75 => ElementManager.Element.Holy,
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
                monster.BonusHp += (int)(monster.MaximumHp * 0.012);
                monster.Experience += (uint)(monster.Experience * 0.02);
                monster.Ability += (uint)(monster.Ability * 0.02);
            },
            ["Great"] = monster =>
            {
                monster._Con += statGen3;
                monster._Str += statGen3;
                monster._Dex -= statGen2;
                monster.BonusHp += (int)(monster.MaximumHp * 0.024);
                monster.Experience += (uint)(monster.Experience * 0.06);
                monster.Ability += (uint)(monster.Ability * 0.06);
            },
            ["Colossal"] = monster =>
            {
                monster._Con += statGen3;
                monster._Str += statGen3;
                monster._Dex += statGen3;
                monster.BonusHp += (int)(monster.MaximumHp * 0.036);
                monster.Experience += (uint)(monster.Experience * 0.10);
                monster.Ability += (uint)(monster.Ability * 0.10);
            },
            ["Deity"] = monster =>
            {
                monster._Con += statGen4;
                monster._Str += statGen4;
                monster._Dex += statGen4;
                monster.BonusHp += (int)(monster.MaximumHp * 0.048);
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

        obj._Hit = (byte)(obj._Dex * 0.2);
        obj.BonusHit = (byte)(10 * (obj.Template.Level / 12));
        obj.BonusMr = (byte)(10 * (obj.Template.Level / 14));
    }

    private static void MonsterStartingStats(Monster obj)
    {
        var levelStatsRange = new SortedDictionary<int, (int start, int end)>
        {
            { 3, (1, 5) },
            { 7, (5, 10) },
            { 11, (8, 15) },
            { 17, (10, 20) },
            { 24, (10, 25) },
            { 31, (10, 35) },
            { 37, (30, 48) },
            { 44, (30, 57) },
            { 51, (30, 66) },
            { 57, (60, 75) },
            { 64, (60, 80) },
            { 71, (60, 85) },
            { 77, (80, 94) },
            { 84, (80, 102) },
            { 91, (80, 109) },
            { 97, (80, 122) },
            { 104, (90, 130) },
            { 110, (90, 140) },
            { 116, (90, 150) },
            { 123, (90, 160) },
            { 129, (90, 170) },
            { 135, (90, 180) },
            { 140, (90, 190) },
            { 144, (100, 200) },
            { 149, (100, 205) },
            { 155, (100, 210) },
            { 160, (125, 220) },
            { 164, (125, 230) },
            { 169, (125, 240) },
            { 175, (125, 250) },
            { 180, (125, 260) },
            { 184, (125, 270) },
            { 190, (125, 280) },
            { 194, (125, 290) },
            { 200, (150, 300) },
            { int.MaxValue, (150, 330) } // default case for level > 200
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
                monster.BonusStr += (byte)(monster._Str * 1.2);
                monster.BonusDmg += (byte)(monster._Dmg * 1.2);
            },
            [PrimaryStat.INT] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 1.2);
                monster.BonusMr += (byte)(monster._Mr * 1.2);
            },
            [PrimaryStat.WIS] = monster =>
            {
                monster.BonusWis += (byte)(monster._Wis * 1.2);
                monster.BonusMp += (int)(monster.BaseMp * 1.2);
            },
            [PrimaryStat.CON] = monster =>
            {
                monster.BonusCon += (byte)(monster._Con * 1.2);
                monster.BonusHp += (int)(monster.BaseHp * 1.2);
            },
            [PrimaryStat.DEX] = monster =>
            {
                monster.BonusDex += (byte)(monster._Dex * 1.2);
                monster.BonusHit += (byte)(monster._Hit * 1.2);
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
                monster.BonusStr += (byte)(monster._Str * 1.2);
                monster.BonusDex += (byte)(monster._Dex * 1.2);
                monster.BonusDmg += (byte)(monster._Dmg * 1.2);
                monster.BonusHp += (int)(monster.MaximumHp * 0.012);
            },
            [MonsterType.Magical] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 1.2);
                monster.BonusWis += (byte)(monster._Wis * 1.2);
                monster.BonusMr += (byte)(monster._Mr * 1.2);
            },
            [MonsterType.GodlyStr] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 5);
                monster.BonusDex += (byte)(monster._Dex * 2.4);
                monster.BonusDmg += (byte)(monster._Dmg * 5);
            },
            [MonsterType.GodlyInt] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 5);
                monster.BonusWis += (byte)(monster._Wis * 2.4);
                monster.BonusMr += (byte)(monster._Mr * 5);
            },
            [MonsterType.GodlyWis] = monster =>
            {
                monster.BonusWis += (byte)(monster._Wis * 5);
                monster.BonusMp += monster.BaseMp * 5;
            },
            [MonsterType.GodlyCon] = monster =>
            {
                monster.BonusCon += (byte)(monster._Con * 5);
                monster.BonusHp += monster.BaseHp * 5;
            },
            [MonsterType.GodlyDex] = monster =>
            {
                monster.BonusDex += (byte)(monster._Dex * 5);
                monster.BonusHit += (byte)(monster._Hit * 5);
                monster.BonusDmg += (byte)(monster._Dmg * 5);
            },
            [MonsterType.Above99P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 5);
                monster.BonusDex += (byte)(monster._Dex * 5);
                monster.BonusDmg += (byte)(monster._Dmg * 5);
                monster.BonusHp += (int)(monster.MaximumHp * 0.03);
            },
            [MonsterType.Above99M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 5);
                monster.BonusWis += (byte)(monster._Wis * 5);
                monster.BonusMr += (byte)(monster._Mr * 5);
            },
            [MonsterType.Above150P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 7);
                monster.BonusDex += (byte)(monster._Dex * 7);
                monster.BonusDmg += (byte)(monster._Dmg * 7);
                monster.BonusHp += (int)(monster.MaximumHp * 0.05);
            },
            [MonsterType.Above150M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 7);
                monster.BonusWis += (byte)(monster._Wis * 7);
                monster.BonusMr += (byte)(monster._Mr * 7);
            },
            [MonsterType.Above200P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 9);
                monster.BonusDex += (byte)(monster._Dex * 9);
                monster.BonusDmg += (byte)(monster._Dmg * 9);
                monster.BonusHp += (int)(monster.MaximumHp * 0.07);
            },
            [MonsterType.Above200M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 9);
                monster.BonusWis += (byte)(monster._Wis * 9);
                monster.BonusMr += (byte)(monster._Mr * 9);
            },
            [MonsterType.Above250P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 10);
                monster.BonusDex += (byte)(monster._Dex * 10);
                monster.BonusDmg += (byte)(monster._Dmg * 10);
                monster.BonusHp += (int)(monster.MaximumHp * 0.08);
            },
            [MonsterType.Above250M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 10);
                monster.BonusWis += (byte)(monster._Wis * 10);
                monster.BonusMr += (byte)(monster._Mr * 10);
            },
            [MonsterType.MasterStr] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 11);
                monster.BonusDex += (byte)(monster._Dex * 10);
                monster.BonusDmg += (byte)(monster._Dmg * 11);
            },
            [MonsterType.MasterInt] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 11);
                monster.BonusWis += (byte)(monster._Wis * 10);
                monster.BonusMr += (byte)(monster._Mr * 11);
            },
            [MonsterType.MasterWis] = monster =>
            {
                monster.BonusWis += (byte)(monster._Wis * 11);
                monster.BonusMp += monster.BaseMp * 11;
            },
            [MonsterType.MasterCon] = monster =>
            {
                monster.BonusCon += (byte)(monster._Con * 11);
                monster.BonusHp += monster.BaseHp * 11;
            },
            [MonsterType.MasterDex] = monster =>
            {
                monster.BonusDex += (byte)(monster._Dex * 11);
                monster.BonusHit += (byte)(monster._Hit * 11);
                monster.BonusDmg += (byte)(monster._Dmg * 11);
            },
            [MonsterType.Forsaken] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 12);
                monster.BonusInt += (byte)(monster._Int * 12);
                monster.BonusWis += (byte)(monster._Wis * 12);
                monster.BonusCon += (byte)(monster._Con * 12);
                monster.BonusDex += (byte)(monster._Dex * 12);
                monster.BonusMr += (byte)(monster._Mr * 12);
                monster.BonusHit += (byte)(monster._Hit * 12);
                monster.BonusDmg += (byte)(monster._Dmg * 12);
                monster.BonusHp += monster.BaseHp * 12;
                monster.BonusMp += monster.BaseMp * 12;
            },
            [MonsterType.Above300P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 13);
                monster.BonusDex += (byte)(monster._Dex * 13);
                monster.BonusDmg += (byte)(monster._Dmg * 13);
                monster.BonusHp += (int)(monster.MaximumHp * 0.11);
            },
            [MonsterType.Above300M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 13);
                monster.BonusWis += (byte)(monster._Wis * 13);
                monster.BonusMr += (byte)(monster._Mr * 13);
            },
            [MonsterType.Above350P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 14);
                monster.BonusDex += (byte)(monster._Dex * 14);
                monster.BonusDmg += (byte)(monster._Dmg * 14);
                monster.BonusHp += (int)(monster.MaximumHp * 0.13);
            },
            [MonsterType.Above350M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 14);
                monster.BonusWis += (byte)(monster._Wis * 14);
                monster.BonusMr += (byte)(monster._Mr * 14);
            },
            [MonsterType.Above400P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 16);
                monster.BonusDex += (byte)(monster._Dex * 16);
                monster.BonusDmg += (byte)(monster._Dmg * 16);
                monster.BonusHp += (int)(monster.MaximumHp * 0.15);
            },
            [MonsterType.Above400M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 16);
                monster.BonusWis += (byte)(monster._Wis * 16);
                monster.BonusMr += (byte)(monster._Mr * 16);
            },
            [MonsterType.Above450P] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 19);
                monster.BonusDex += (byte)(monster._Dex * 19);
                monster.BonusDmg += (byte)(monster._Dmg * 19);
                monster.BonusHp += (int)(monster.MaximumHp * 0.17);
            },
            [MonsterType.Above450M] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 19);
                monster.BonusWis += (byte)(monster._Wis * 19);
                monster.BonusMr += (byte)(monster._Mr * 19);
            },
            [MonsterType.DivineStr] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 25);
                monster.BonusDex += (byte)(monster._Dex * 22);
                monster.BonusDmg += (byte)(monster._Dmg * 22);
            },
            [MonsterType.DivineInt] = monster =>
            {
                monster.BonusInt += (byte)(monster._Int * 25);
                monster.BonusWis += (byte)(monster._Wis * 22);
                monster.BonusMr += (byte)(monster._Mr * 25);
            },
            [MonsterType.DivineWis] = monster =>
            {
                monster.BonusWis += (byte)(monster._Wis * 25);
                monster.BonusMp += monster.BaseMp * 25;
            },
            [MonsterType.DivineCon] = monster =>
            {
                monster.BonusCon += (byte)(monster._Con * 25);
                monster.BonusHp += monster.BaseHp * 25;
            },
            [MonsterType.DivineDex] = monster =>
            {
                monster.BonusDex += (byte)(monster._Dex * 25);
                monster.BonusHit += (byte)(monster._Hit * 25);
                monster.BonusDmg += (byte)(monster._Dmg * 25);
            },
            [MonsterType.Boss] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 35);
                monster.BonusInt += (byte)(monster._Int * 35);
                monster.BonusWis += (byte)(monster._Wis * 25);
                monster.BonusCon += (byte)(monster._Con * 25);
                monster.BonusDex += (byte)(monster._Dex * 35);
                monster.BonusMr += (byte)(monster._Mr * 25);
                monster.BonusHit += (byte)(monster._Hit * 25);
                monster.BonusDmg += (byte)(monster._Dmg * 30);
                monster.BonusHp += monster.BaseHp * 40;
                monster.BonusMp += monster.BaseMp * 40;
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
                Analytics.TrackEvent($"{template.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

            if (!scripts.TryGetValue(script.ScriptName, out var skillScript)) return;
            skillScript.Skill.NextAvailableUse = DateTime.UtcNow;
            skillScript.Skill.Level = 100;
            obj.SkillScripts.Add(skillScript);
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
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
                Analytics.TrackEvent($"{template.Name}: is missing a script for {spellScriptStr}\n");
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
            Crashes.TrackError(ex);
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
                Analytics.TrackEvent($"{template.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

            if (!scripts.TryGetValue(script.ScriptName, out var skillScript)) return;
            skillScript.Skill.NextAvailableUse = DateTime.UtcNow;
            skillScript.Skill.Level = 100;
            obj.AbilityScripts.Add(skillScript);
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
        }
    }

    /// <summary>
    /// Give monsters random assails depending on their level
    /// </summary>
    private void Assails(Monster monster)
    {
        var skillList = monster.Template.Level switch
        {
            <= 11 => new List<string>
            {
                "Onslaught", "Assault", "Clobber", "Bite", "Claw"
            },
            > 11 and <= 50 => new List<string>
            {
                "Double Punch", "Punch", "Clobber x2", "Onslaught", "Thrust",
                "Wallop", "Assault", "Clobber", "Bite", "Claw", "Stomp", "Tail Slap"
            },
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
            if (!randomIndices.Any()) // All skills have been assigned
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
            <= 25 => new List<string>
            {
                "Stab", "Dual Slice", "Wind Slice", "Wind Blade",
            },
            > 25 and <= 60 => new List<string>
            {
                "Claw Fist", "Cross Body Punch", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist", "Stab", "Stab'n Twist", "Stab Twice",
                "Desolate", "Dual Slice", "Rush", "Wind Slice", "Beag Suain", "Wind Blade", "Double-Edged Dance", "Bite'n Shake", "Howl'n Call", "Death From Above",
                "Pounce", "Roll Over", "Corrosive Touch"
            },
            > 60 and <= 75 => new List<string>
            {
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist",
                "Stab", "Stab'n Twist", "Stab Twice", "Desolate", "Dual Slice", "Lullaby Strike", "Rush", "Sever", "Wind Slice", "Beag Suain", "Charge",
                "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over",
                "Swallow Whole", "Tentacle", "Corrosive Touch"
            },
            > 75 and <= 120 => new List<string>
            {
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike",
                "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice", "Titan's Cleave", "Desolate", "Dual Slice", "Lullaby Strike", "Rush",
                "Sever", "Wind Slice", "Beag Suain", "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Retribution", "Flame Thrower",
                "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole", "Tentacle", "Corrosive Touch", "Tantalizing Gaze"
            },
            _ => new List<string>
            {
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Kelberoth Strike", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike",
                "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice", "Titan's Cleave", "Crasher", "Desolate", "Dual Slice", "Lullaby Strike", "Rush",
                "Sever", "Wind Slice", "Beag Suain", "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Retribution", "Flame Thrower",
                "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole", "Tentacle", "Corrosive Touch", "Tantalizing Gaze"
            }
        };

        var skillCount = Math.Round(monster.Level / 30d) + 1;
        skillCount = Math.Min(skillCount, 5); // Max 5 abilities regardless of level
        var randomIndices = Enumerable.Range(0, skillList.Count).ToList();

        for (var i = 0; i < skillCount; i++)
        {
            if (!randomIndices.Any()) // All abilities have been assigned
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
            "Beag Srad", "Beag Sal", "Beag Athar", "Beag Creag", "Beag Dorcha", "Beag Eadrom", "Beag Puinsein", "Beag Cradh", "Ao Beag Cradh"
        };

        var spellCount = Math.Round(monster.Level / 20d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (!randomIndices.Any()) // All spells have been assigned
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
            if (!randomIndices.Any()) // All spells have been assigned
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
            "Mor Srad", "Mor Sal", "Mor Athar", "Mor Creag", "Mor Dorcha", "Mor Eadrom", "Mor Puinsein", "Mor Cradh", "Fas Nadur", "Blind", "Pramh", "Ao Mor Cradh"
        };

        var spellCount = Math.Round(monster.Level / 70d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (!randomIndices.Any()) // All spells have been assigned
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
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh", "Mor Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh"
        };

        var spellCount = Math.Round(monster.Level / 100d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (!randomIndices.Any()) // All spells have been assigned
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
        if (monster.Template.Level <= 120) return;

        var spellList = new[]
        {
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh", "Ard Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh", "Ao Puinsein", "Dark Chain", "Defensive Stance"
        };

        var spellCount = Math.Round(monster.Level / 150d) + 2;
        spellCount = Math.Min(spellCount, 5); // Max 5 spells regardless of level
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (!randomIndices.Any()) // All spells have been assigned
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
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
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
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Contruct)) return;
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

    private void ElementalSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Elemental)) return;
        var skillList = new List<string> { "Onslaught", "Assault" };
        var abilityList = new List<string> { "Atlantean Weapon", "Elemental Bane" };
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
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
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
    }

    private void GargoyleSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Gargoyle)) return;
        var skillList = new List<string> { "Slash" };
        var abilityList = new List<string> { "Kelberoth Strike", "Palm Heel Strike" };
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
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
    }

    private void InsectSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Insect)) return;
        var skillList = new List<string> { "Bite" };
        var abilityList = new List<string> { "Corrosive Touch" };
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
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
        MonsterLoader(new List<string>(), new List<string>(), spellList, monster);
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
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
    }

    private void RoboticSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Robotic)) return;
        var spellList = new List<string> { "Mor Dion", "Perfect Defense" };
        MonsterLoader(new List<string>(), new List<string>(), spellList, monster);
    }

    private void ShadowSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Shadow)) return;
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Lullaby Strike", "Vampiric Slash" };
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
    }

    private void RodentSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Rodent)) return;
        var skillList = new List<string> { "Bite", "Assault" };
        var abilityList = new List<string> { "Rush" };
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
    }

    private void UndeadSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead)) return;
        var skillList = new List<string> { "Wallop" };
        var abilityList = new List<string> { "Corrosive Touch", "Retribution" };
        MonsterLoader(skillList, abilityList, new List<string>(), monster);
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