using System.Numerics;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Darkages.GameScripts.Creations;

[Script("Create Monster")]
public class CreateMonster : MonsterCreateScript
{
    private readonly Area _map;
    private readonly MonsterTemplate _monsterTemplate;

    public CreateMonster(MonsterTemplate template, Area map)
    {
        _map = map;
        _monsterTemplate = template;
    }

    public override Monster Create()
    {
        if (_monsterTemplate.CastSpeed <= 6000) _monsterTemplate.CastSpeed = 6000;
        if (_monsterTemplate.AttackSpeed <= 500) _monsterTemplate.AttackSpeed = 500;
        if (_monsterTemplate.MovementSpeed <= 500) _monsterTemplate.MovementSpeed = 500;
        if (_monsterTemplate.Level <= 1) _monsterTemplate.Level = 1;

        var obj = new Monster
        {
            Template = _monsterTemplate,
            BashTimer = new GameServerTimer(TimeSpan.FromMilliseconds(_monsterTemplate.AttackSpeed)),
            AbilityTimer = new GameServerTimer(TimeSpan.FromMilliseconds(_monsterTemplate.CastSpeed)),
            CastTimer = new GameServerTimer(TimeSpan.FromMilliseconds(_monsterTemplate.CastSpeed)),
            WalkTimer = new GameServerTimer(TimeSpan.FromMilliseconds(_monsterTemplate.MovementSpeed)),
            ObjectUpdateTimer = new GameServerTimer(TimeSpan.FromMilliseconds(ServerSetup.Instance.Config.GlobalBaseSkillDelay)),
            CastEnabled = true,
            TaggedAislings = new HashSet<int>(),
            AggroList = new List<int>(),
            Serial = Generator.GenerateNumber(),
            Size = "",
            CurrentMapId = _map.ID
        };

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
            { 119, (1000, 800) },
            { 150, (1500, 1300) },
            { 200, (2000, 1500) },
            { 250, (4000, 2000) },
            { 300, (6000, 4000) },
            { 350, (8000, 6000) },
            { 400, (10000, 8000) },
            { 450, (15000, 10000) },
            { int.MaxValue, (20000, 15000) } // default case for level > 450
        };

        // Find the first multiplier where the level is less than or equal to the key
        var (hpMultiplier, mpMultiplier) = levelMultipliers.First(x => obj.Template.Level <= x.Key).Value;

        obj.BaseHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * hpMultiplier);
        obj.BaseMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * mpMultiplier);

        MonsterSize(obj);
        MonsterArmorClass(obj);
        MonsterExperience(obj);

        MonsterBaseAndSizeStats(obj);
        MonsterStatBoostOnPrimary(obj);
        MonsterStatBoostOnType(obj);

        obj.CurrentHp = obj.MaximumHp;
        obj.CurrentMp = obj.MaximumMp;

        SetElementalAlignment(obj);
        SetWalkEnabled(obj);
        SetMood(obj);
        SetSpawn(obj);

        if (obj.Map.IsAStarWall(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;
        if (obj.Map.IsSpriteInLocationOnCreation(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;

        obj.AbandonedDate = DateTime.Now;

        obj.Image = _monsterTemplate.ImageVarience > 0
            ? (ushort)Random.Shared.Next(_monsterTemplate.Image, _monsterTemplate.Image + _monsterTemplate.ImageVarience)
            : _monsterTemplate.Image;

        return obj;
    }

    private void SetElementalAlignment(Monster obj)
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
        var pathQualifier = _monsterTemplate.PathQualifer;
    
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
        if (_monsterTemplate.MoodType.MoodFlagIsSet(MoodQualifer.Aggressive) || _monsterTemplate.MoodType.MoodFlagIsSet(MoodQualifer.VeryAggressive))
        {
            obj.Aggressive = true;
        }
        else if (_monsterTemplate.MoodType.MoodFlagIsSet(MoodQualifer.Unpredicable))
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
        if (_monsterTemplate.SpawnType == SpawnQualifer.Random)
        {
            var x = Generator.GenerateMapLocation(_map.Rows);
            var y = Generator.GenerateMapLocation(_map.Cols);
            obj.Pos = new Vector2(x, y);
        }
        else
        {
            obj.Pos = new Vector2(_monsterTemplate.DefinedX, _monsterTemplate.DefinedY);
        }
    }

    private void MonsterSkillSet(Monster obj)
    {
        var monsterRaceActions = new Dictionary<MonsterRace, Action<Monster>>
        {
            [MonsterRace.Aberration] = MonsterExtensions.AberrationSet,
            [MonsterRace.Animal] = MonsterExtensions.AnimalSet,
            [MonsterRace.Aquatic] = MonsterExtensions.AquaticSet,
            [MonsterRace.Beast] = MonsterExtensions.BeastSet,
            [MonsterRace.Celestial] = MonsterExtensions.CelestialSet,
            [MonsterRace.Contruct] = MonsterExtensions.ContructSet,
            [MonsterRace.Demon] = MonsterExtensions.DemonSet,
            [MonsterRace.Dragon] = MonsterExtensions.DragonSet,
            [MonsterRace.Elemental] = MonsterExtensions.ElementalSet,
            [MonsterRace.Fairy] = MonsterExtensions.FairySet,
            [MonsterRace.Fiend] = MonsterExtensions.FiendSet,
            [MonsterRace.Fungi] = MonsterExtensions.FungiSet,
            [MonsterRace.Gargoyle] = MonsterExtensions.GargoyleSet,
            [MonsterRace.Giant] = MonsterExtensions.GiantSet,
            [MonsterRace.Goblin] = MonsterExtensions.GoblinSet,
            [MonsterRace.Grimlok] = MonsterExtensions.GrimlokSet,
            [MonsterRace.Humanoid] = MonsterExtensions.HumanoidSet,
            [MonsterRace.Insect] = MonsterExtensions.InsectSet,
            [MonsterRace.Kobold] = MonsterExtensions.KoboldSet,
            [MonsterRace.Magical] = MonsterExtensions.MagicalSet,
            [MonsterRace.Mukul] = MonsterExtensions.MukulSet,
            [MonsterRace.Ooze] = MonsterExtensions.OozeSet,
            [MonsterRace.Orc] = MonsterExtensions.OrcSet,
            [MonsterRace.Plant] = MonsterExtensions.PlantSet,
            [MonsterRace.Reptile] = MonsterExtensions.ReptileSet,
            [MonsterRace.Robotic] = MonsterExtensions.RoboticSet,
            [MonsterRace.Shadow] = MonsterExtensions.ShadowSet,
            [MonsterRace.Rodent] = MonsterExtensions.RodentSet,
            [MonsterRace.Undead] = MonsterExtensions.UndeadSet,
            // Dummy, Inanimate, LowerBeing, HigherBeing have no set action, so they are omitted here
        };

        if (monsterRaceActions.TryGetValue(_monsterTemplate.MonsterRace, out var raceAction))
        {
            raceAction(obj);
        }

        MonsterExtensions.Assails(obj);
        MonsterExtensions.BasicAbilities(obj);
        MonsterExtensions.BeagSpells(obj);
        MonsterExtensions.NormalSpells(obj);
        MonsterExtensions.MorSpells(obj);
        MonsterExtensions.ArdSpells(obj);
        MonsterExtensions.MasterSpells(obj);

        if (_monsterTemplate.SkillScripts != null)
            foreach (var skillScriptStr in _monsterTemplate.SkillScripts.Where(skillScriptStr => !string.IsNullOrWhiteSpace(skillScriptStr)))
                LoadSkillScript(skillScriptStr, obj);

        if (_monsterTemplate.AbilityScripts != null)
            foreach (var abilityScriptStr in _monsterTemplate.AbilityScripts.Where(abilityScriptStr => !string.IsNullOrWhiteSpace(abilityScriptStr)))
                LoadAbilityScript(abilityScriptStr, obj);

        if (_monsterTemplate.SpellScripts == null) return;
        foreach (var spellScriptStr in _monsterTemplate.SpellScripts.Where(spellScriptStr => !string.IsNullOrWhiteSpace(spellScriptStr)))
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
            { 57, (60, 66) },
            { 64, (67, 74) },
            { 71, (75, 79) },
            { 77, (80, 86) },
            { 84, (87, 94) },
            { 91, (95, 99) },
            { 97, (100, 110) },
            { 104, (110, 125) },
            { 110, (120, 135) },
            { 116, (130, 145) },
            { 123, (140, 155) },
            { 129, (150, 165) },
            { 135, (160, 175) },
            { 140, (160, 185) },
            { 144, (180, 195) },
            { 149, (190, 205) },
            { 155, (190, 210) },
            { 160, (190, 220) },
            { 164, (200, 230) },
            { 169, (200, 240) },
            { 175, (200, 250) },
            { 180, (210, 260) },
            { 184, (220, 270) },
            { 190, (230, 280) },
            { 194, (240, 290) },
            { 200, (250, 300) },
            { int.MaxValue, (260, 330) } // default case for level > 200
        };

        // Find the first range where the level is less than or equal to the key
        var (start, end) = levelArmorClassRange.First(x => obj.Template.Level <= x.Key).Value;

        obj.BonusAc = Generator.GenerateDeterminedNumberRange(start, end);
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
            { int.MaxValue, (3900000, 4200000) } // default case for level > 200
        };

        var (start, end) = levelExperienceRange.First(x => obj.Template.Level <= x.Key).Value;

        obj.Experience = (uint)Generator.GenerateDeterminedNumberRange(start, end);
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
            },
            ["Great"] = monster =>
            {
                monster._Con += statGen3;
                monster._Str += statGen3;
                monster._Dex -= statGen2;
                monster.BonusHp += (int)(monster.MaximumHp * 0.024);
                monster.Experience += (uint)(monster.Experience * 0.06);
            },
            ["Colossal"] = monster =>
            {
                monster._Con += statGen3;
                monster._Str += statGen3;
                monster._Dex += statGen3;
                monster.BonusHp += (int)(monster.MaximumHp * 0.036);
                monster.Experience += (uint)(monster.Experience * 0.10);
            },
            ["Deity"] = monster =>
            {
                monster._Con += statGen4;
                monster._Str += statGen4;
                monster._Dex += statGen4;
                monster.BonusHp += (int)(monster.MaximumHp * 0.048);
                monster.Experience += (uint)(monster.Experience * 0.15);
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
            [MonsterType.Forsaken] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 8);
                monster.BonusInt += (byte)(monster._Int * 8);
                monster.BonusWis += (byte)(monster._Wis * 8);
                monster.BonusCon += (byte)(monster._Con * 8);
                monster.BonusDex += (byte)(monster._Dex * 8);
                monster.BonusMr += (byte)(monster._Mr * 5);
                monster.BonusHit += (byte)(monster._Hit * 3);
                monster.BonusDmg += (byte)(monster._Dmg * 3);
                monster.BonusHp += monster.BaseHp * 4;
                monster.BonusMp += monster.BaseMp * 4;
            },
            [MonsterType.Boss] = monster =>
            {
                monster.BonusStr += (byte)(monster._Str * 13);
                monster.BonusInt += (byte)(monster._Int * 13);
                monster.BonusWis += (byte)(monster._Wis * 13);
                monster.BonusCon += (byte)(monster._Con * 13);
                monster.BonusDex += (byte)(monster._Dex * 13);
                monster.BonusMr += (byte)(monster._Mr * 9);
                monster.BonusHit += (byte)(monster._Hit * 5);
                monster.BonusDmg += (byte)(monster._Dmg * 5);
                monster.BonusHp += monster.BaseHp * 15;
                monster.BonusMp += monster.BaseMp * 8;
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
                Analytics.TrackEvent($"{_monsterTemplate.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

            if (!scripts.TryGetValue(script.ScriptName, out var skillScript)) return;
            skillScript.Skill.NextAvailableUse = DateTime.Now;
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
                Analytics.TrackEvent($"{_monsterTemplate.Name}: is missing a script for {spellScriptStr}\n");
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
                Analytics.TrackEvent($"{_monsterTemplate.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

            if (!scripts.TryGetValue(script.ScriptName, out var skillScript)) return;
            skillScript.Skill.NextAvailableUse = DateTime.Now;
            skillScript.Skill.Level = 100;
            obj.AbilityScripts.Add(skillScript);
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
        }
    }
}