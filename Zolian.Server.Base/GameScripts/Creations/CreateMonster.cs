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

        switch (obj.Template.Level)
        {
            case <= 9:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 100);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 80);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 90:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 1000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 800);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 150:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 1500);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 1300);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 200:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 2000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 1500);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 250:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 4000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 2000);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 300:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 6000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 4000);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 350:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 8000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 6000);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 400:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 10000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 8000);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 450:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 15000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 10000);
                    obj.BaseMp = monsterMp;
                    break;
                }
            case <= 500:
                {
                    var monsterHp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 20000);
                    obj.BaseHp = monsterHp;
                    var monsterMp = Generator.RandomMonsterStatVariance((int)obj.Template.Level * 15000);
                    obj.BaseMp = monsterMp;
                    break;
                }
        }

        MonsterSize(obj);
        MonsterArmorClass(obj);
        MonsterExperience(obj);

        MonsterBaseAndSizeStats(obj);
        MonsterStatBoostOnPrimary(obj);
        MonsterStatBoostOnType(obj);

        obj.CurrentHp = obj.MaximumHp;
        obj.CurrentMp = obj.MaximumMp;

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

        if ((_monsterTemplate.PathQualifer & PathQualifer.Wander) == PathQualifer.Wander)
            obj.WalkEnabled = true;
        else if ((_monsterTemplate.PathQualifer & PathQualifer.Fixed) == PathQualifer.Fixed)
            obj.WalkEnabled = false;
        else if ((_monsterTemplate.PathQualifer & PathQualifer.Patrol) == PathQualifer.Patrol)
            obj.WalkEnabled = true;

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

        if (obj.Map.IsAStarWall(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;
        if (obj.Map.IsSpriteInLocationOnCreation(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;

        obj.AbandonedDate = DateTime.Now;

        obj.Image = _monsterTemplate.ImageVarience > 0
            ? (ushort)Random.Shared.Next(_monsterTemplate.Image, _monsterTemplate.Image + _monsterTemplate.ImageVarience)
            : _monsterTemplate.Image;

        return obj;
    }

    private void MonsterSkillSet(Monster obj)
    {
        switch (_monsterTemplate.MonsterRace)
        {
            case MonsterRace.Aberration:
                MonsterExtensions.AberrationSet(obj);
                break;
            case MonsterRace.Animal:
                MonsterExtensions.AnimalSet(obj);
                break;
            case MonsterRace.Aquatic:
                MonsterExtensions.AquaticSet(obj);
                break;
            case MonsterRace.Beast:
                MonsterExtensions.BeastSet(obj);
                break;
            case MonsterRace.Celestial:
                MonsterExtensions.CelestialSet(obj);
                break;
            case MonsterRace.Contruct:
                MonsterExtensions.ContructSet(obj);
                break;
            case MonsterRace.Demon:
                MonsterExtensions.DemonSet(obj);
                break;
            case MonsterRace.Dragon:
                MonsterExtensions.DragonSet(obj);
                break;
            case MonsterRace.Elemental:
                MonsterExtensions.ElementalSet(obj);
                break;
            case MonsterRace.Fairy:
                MonsterExtensions.FairySet(obj);
                break;
            case MonsterRace.Fiend:
                MonsterExtensions.FiendSet(obj);
                break;
            case MonsterRace.Fungi:
                MonsterExtensions.FungiSet(obj);
                break;
            case MonsterRace.Gargoyle:
                MonsterExtensions.GargoyleSet(obj);
                break;
            case MonsterRace.Giant:
                MonsterExtensions.GiantSet(obj);
                break;
            case MonsterRace.Goblin:
                MonsterExtensions.GoblinSet(obj);
                break;
            case MonsterRace.Grimlok:
                MonsterExtensions.GrimlokSet(obj);
                break;
            case MonsterRace.Humanoid:
                MonsterExtensions.HumanoidSet(obj);
                break;
            case MonsterRace.Insect:
                MonsterExtensions.InsectSet(obj);
                break;
            case MonsterRace.Kobold:
                MonsterExtensions.KoboldSet(obj);
                break;
            case MonsterRace.Magical:
                MonsterExtensions.MagicalSet(obj);
                break;
            case MonsterRace.Mukul:
                MonsterExtensions.MukulSet(obj);
                break;
            case MonsterRace.Ooze:
                MonsterExtensions.OozeSet(obj);
                break;
            case MonsterRace.Orc:
                MonsterExtensions.OrcSet(obj);
                break;
            case MonsterRace.Plant:
                MonsterExtensions.PlantSet(obj);
                break;
            case MonsterRace.Reptile:
                MonsterExtensions.ReptileSet(obj);
                break;
            case MonsterRace.Robotic:
                MonsterExtensions.RoboticSet(obj);
                break;
            case MonsterRace.Shadow:
                MonsterExtensions.ShadowSet(obj);
                break;
            case MonsterRace.Rodent:
                MonsterExtensions.RodentSet(obj);
                break;
            case MonsterRace.Undead:
                MonsterExtensions.UndeadSet(obj);
                break;
            case MonsterRace.Dummy:
            case MonsterRace.Inanimate:
            case MonsterRace.LowerBeing:
            case MonsterRace.HigherBeing:
                break;
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
            <= 98 and >= 12 => sizeRand switch
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
        obj.BonusAc = obj.Template.Level switch
        {
            >= 1 and <= 3 => Generator.GenerateDeterminedNumberRange(1, 4),
            >= 4 and <= 7 => Generator.GenerateDeterminedNumberRange(5, 7),
            >= 8 and <= 11 => Generator.GenerateDeterminedNumberRange(8, 13),
            >= 12 and <= 17 => Generator.GenerateDeterminedNumberRange(14, 18),
            >= 18 and <= 24 => Generator.GenerateDeterminedNumberRange(19, 26),
            >= 25 and <= 31 => Generator.GenerateDeterminedNumberRange(27, 35),
            >= 32 and <= 37 => Generator.GenerateDeterminedNumberRange(36, 45),
            >= 38 and <= 44 => Generator.GenerateDeterminedNumberRange(46, 52),
            >= 45 and <= 51 => Generator.GenerateDeterminedNumberRange(53, 59),
            >= 52 and <= 57 => Generator.GenerateDeterminedNumberRange(60, 66),
            >= 58 and <= 64 => Generator.GenerateDeterminedNumberRange(67, 74),
            >= 65 and <= 71 => Generator.GenerateDeterminedNumberRange(75, 79),
            >= 72 and <= 77 => Generator.GenerateDeterminedNumberRange(80, 86),
            >= 78 and <= 84 => Generator.GenerateDeterminedNumberRange(87, 94),
            >= 85 and <= 91 => Generator.GenerateDeterminedNumberRange(95, 99),
            >= 92 and <= 97 => Generator.GenerateDeterminedNumberRange(100, 110),
            >= 98 and <= 104 => Generator.GenerateDeterminedNumberRange(110, 125),
            >= 105 and <= 110 => Generator.GenerateDeterminedNumberRange(120, 135),
            >= 111 and <= 116 => Generator.GenerateDeterminedNumberRange(130, 145),
            >= 117 and <= 123 => Generator.GenerateDeterminedNumberRange(140, 155),
            >= 124 and <= 129 => Generator.GenerateDeterminedNumberRange(150, 165),
            >= 130 and <= 135 => Generator.GenerateDeterminedNumberRange(160, 175),
            >= 136 and <= 140 => Generator.GenerateDeterminedNumberRange(160, 185),
            >= 141 and <= 144 => Generator.GenerateDeterminedNumberRange(180, 195),
            >= 145 and <= 149 => Generator.GenerateDeterminedNumberRange(190, 205),
            >= 150 and <= 155 => Generator.GenerateDeterminedNumberRange(190, 210),
            >= 156 and <= 160 => Generator.GenerateDeterminedNumberRange(190, 220),
            >= 161 and <= 164 => Generator.GenerateDeterminedNumberRange(200, 230),
            >= 165 and <= 169 => Generator.GenerateDeterminedNumberRange(200, 240),
            >= 170 and <= 175 => Generator.GenerateDeterminedNumberRange(200, 250),
            >= 176 and <= 180 => Generator.GenerateDeterminedNumberRange(210, 260),
            >= 181 and <= 184 => Generator.GenerateDeterminedNumberRange(220, 270),
            >= 185 and <= 190 => Generator.GenerateDeterminedNumberRange(230, 280),
            >= 191 and <= 194 => Generator.GenerateDeterminedNumberRange(240, 290),
            >= 195 and <= 200 => Generator.GenerateDeterminedNumberRange(250, 300),
            >= 201 => Generator.GenerateDeterminedNumberRange(260, 330),
            _ => obj.BonusAc
        };
    }

    private static void MonsterExperience(Monster obj)
    {
        obj.Experience = obj.Template.Level switch
        {
            >= 1 and <= 3 => (uint)Generator.GenerateDeterminedNumberRange(200, 600),
            >= 4 and <= 7 => (uint)Generator.GenerateDeterminedNumberRange(600, 1500),
            >= 8 and <= 11 => (uint)Generator.GenerateDeterminedNumberRange(5000, 15000),
            >= 12 and <= 17 => (uint)Generator.GenerateDeterminedNumberRange(9000, 17000),
            >= 18 and <= 24 => (uint)Generator.GenerateDeterminedNumberRange(15000, 24000),
            >= 25 and <= 31 => (uint)Generator.GenerateDeterminedNumberRange(17000, 35000),
            >= 32 and <= 37 => (uint)Generator.GenerateDeterminedNumberRange(24000, 50000),
            >= 38 and <= 44 => (uint)Generator.GenerateDeterminedNumberRange(35000, 58000),
            >= 45 and <= 51 => (uint)Generator.GenerateDeterminedNumberRange(52000, 65000),
            >= 52 and <= 57 => (uint)Generator.GenerateDeterminedNumberRange(60000, 90000),
            >= 58 and <= 64 => (uint)Generator.GenerateDeterminedNumberRange(74000, 108000),
            >= 65 and <= 71 => (uint)Generator.GenerateDeterminedNumberRange(94000, 120000),
            >= 72 and <= 77 => (uint)Generator.GenerateDeterminedNumberRange(110000, 134000),
            >= 78 and <= 84 => (uint)Generator.GenerateDeterminedNumberRange(125000, 150000),
            >= 85 and <= 91 => (uint)Generator.GenerateDeterminedNumberRange(138000, 165000),
            >= 92 and <= 97 => (uint)Generator.GenerateDeterminedNumberRange(250000, 295000),
            >= 98 and <= 104 => (uint)Generator.GenerateDeterminedNumberRange(290000, 345000),
            >= 105 and <= 110 => (uint)Generator.GenerateDeterminedNumberRange(350000, 467000),
            >= 111 and <= 116 => (uint)Generator.GenerateDeterminedNumberRange(400000, 535000),
            >= 117 and <= 123 => (uint)Generator.GenerateDeterminedNumberRange(615000, 750000),
            >= 124 and <= 129 => (uint)Generator.GenerateDeterminedNumberRange(775000, 890000),
            >= 130 and <= 135 => (uint)Generator.GenerateDeterminedNumberRange(910000, 1200000),
            >= 136 and <= 140 => (uint)Generator.GenerateDeterminedNumberRange(1000000, 1300000),
            >= 141 and <= 144 => (uint)Generator.GenerateDeterminedNumberRange(1250000, 1390000),
            >= 145 and <= 149 => (uint)Generator.GenerateDeterminedNumberRange(1375000, 1450000),
            >= 150 and <= 155 => (uint)Generator.GenerateDeterminedNumberRange(1400000, 1600000),
            >= 156 and <= 160 => (uint)Generator.GenerateDeterminedNumberRange(1500000, 1700000),
            >= 161 and <= 164 => (uint)Generator.GenerateDeterminedNumberRange(1600000, 1800000),
            >= 165 and <= 169 => (uint)Generator.GenerateDeterminedNumberRange(1700000, 1900000),
            >= 170 and <= 175 => (uint)Generator.GenerateDeterminedNumberRange(1800000, 2000000),
            >= 176 and <= 180 => (uint)Generator.GenerateDeterminedNumberRange(1900000, 2100000),
            >= 181 and <= 184 => (uint)Generator.GenerateDeterminedNumberRange(2200000, 2499999),
            >= 185 and <= 190 => (uint)Generator.GenerateDeterminedNumberRange(2500000, 2899999),
            >= 191 and <= 194 => (uint)Generator.GenerateDeterminedNumberRange(2900000, 3499999),
            >= 195 and <= 200 => (uint)Generator.GenerateDeterminedNumberRange(3500000, 3899999),
            >= 201 => (uint)Generator.GenerateDeterminedNumberRange(3900000, 4200000),
            _ => (uint)Generator.GenerateDeterminedNumberRange(200, 600)
        };
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
            case <= 98 and >= 12:
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

        switch (obj.Size)
        {
            case "Lessor":
                obj._Con -= statGen1;
                obj._Str -= statGen1;
                obj._Dex -= statGen1;
                break;
            case "Small":
                obj._Con -= statGen1;
                obj._Str -= statGen1;
                obj._Dex += statGen2;
                break;
            case "Medium":
                obj._Con += statGen2;
                obj._Str += statGen2;
                obj._Dex += statGen2;
                break;
            case "Large":
                obj._Con += statGen3;
                obj._Str += statGen2;
                obj._Dex -= statGen1;
                obj.BonusHp += (int)(obj.MaximumHp * 0.012);
                obj.Experience += (uint)(obj.Experience * 0.02);
                break;
            case "Great":
                obj._Con += statGen3;
                obj._Str += statGen3;
                obj._Dex -= statGen2;
                obj.BonusHp += (int)(obj.MaximumHp * 0.024);
                obj.Experience += (uint)(obj.Experience * 0.06);
                break;
            case "Colossal":
                obj._Con += statGen3;
                obj._Str += statGen3;
                obj._Dex += statGen3;
                obj.BonusHp += (int)(obj.MaximumHp * 0.036);
                obj.Experience += (uint)(obj.Experience * 0.10);
                break;
            case "Deity":
                obj._Con += statGen4;
                obj._Str += statGen4;
                obj._Dex += statGen4;
                obj.BonusHp += (int)(obj.MaximumHp * 0.048);
                obj.Experience += (uint)(obj.Experience * 0.15);
                break;
            default:
                obj.Size = "Lessor";
                obj._Con -= statGen1;
                obj._Str -= statGen1;
                obj._Dex -= statGen1;
                break;
        }

        obj._Hit = (byte)(obj._Dex * 0.2);
        obj.BonusHit = (byte)(10 * (obj.Template.Level / 12));
        obj.BonusMr = (byte)(10 * (obj.Template.Level / 14));
    }

    private static void MonsterStartingStats(Monster obj)
    {
        for (var i = 0; i < 5; i++)
        {
            var x = obj.Template.Level switch
            {
                >= 1 and <= 3 => Generator.GenerateDeterminedNumberRange(1, 8),
                >= 4 and <= 7 => Generator.GenerateDeterminedNumberRange(5, 10),
                >= 8 and <= 11 => Generator.GenerateDeterminedNumberRange(8, 15),
                >= 12 and <= 17 => Generator.GenerateDeterminedNumberRange(10, 20),
                >= 18 and <= 24 => Generator.GenerateDeterminedNumberRange(10, 25),
                >= 25 and <= 31 => Generator.GenerateDeterminedNumberRange(10, 35),
                >= 32 and <= 37 => Generator.GenerateDeterminedNumberRange(30, 48),
                >= 38 and <= 44 => Generator.GenerateDeterminedNumberRange(30, 57),
                >= 45 and <= 51 => Generator.GenerateDeterminedNumberRange(30, 66),
                >= 52 and <= 57 => Generator.GenerateDeterminedNumberRange(60, 75),
                >= 58 and <= 64 => Generator.GenerateDeterminedNumberRange(60, 80),
                >= 65 and <= 71 => Generator.GenerateDeterminedNumberRange(60, 85),
                >= 72 and <= 77 => Generator.GenerateDeterminedNumberRange(80, 94),
                >= 78 and <= 84 => Generator.GenerateDeterminedNumberRange(80, 102),
                >= 85 and <= 91 => Generator.GenerateDeterminedNumberRange(80, 109),
                >= 92 and <= 97 => Generator.GenerateDeterminedNumberRange(80, 122),
                >= 98 and <= 104 => Generator.GenerateDeterminedNumberRange(90, 130),
                >= 105 and <= 110 => Generator.GenerateDeterminedNumberRange(90, 140),
                >= 111 and <= 116 => Generator.GenerateDeterminedNumberRange(90, 150),
                >= 117 and <= 123 => Generator.GenerateDeterminedNumberRange(90, 160),
                >= 124 and <= 129 => Generator.GenerateDeterminedNumberRange(90, 170),
                >= 130 and <= 135 => Generator.GenerateDeterminedNumberRange(90, 180),
                >= 136 and <= 140 => Generator.GenerateDeterminedNumberRange(90, 190),
                >= 141 and <= 144 => Generator.GenerateDeterminedNumberRange(100, 200),
                >= 145 and <= 149 => Generator.GenerateDeterminedNumberRange(100, 205),
                >= 150 and <= 155 => Generator.GenerateDeterminedNumberRange(100, 210),
                >= 156 and <= 160 => Generator.GenerateDeterminedNumberRange(125, 220),
                >= 161 and <= 164 => Generator.GenerateDeterminedNumberRange(125, 230),
                >= 165 and <= 169 => Generator.GenerateDeterminedNumberRange(125, 240),
                >= 170 and <= 175 => Generator.GenerateDeterminedNumberRange(125, 250),
                >= 176 and <= 180 => Generator.GenerateDeterminedNumberRange(125, 260),
                >= 181 and <= 184 => Generator.GenerateDeterminedNumberRange(125, 270),
                >= 185 and <= 190 => Generator.GenerateDeterminedNumberRange(125, 280),
                >= 191 and <= 194 => Generator.GenerateDeterminedNumberRange(125, 290),
                >= 195 and <= 200 => Generator.GenerateDeterminedNumberRange(150, 300),
                >= 201 => Generator.GenerateDeterminedNumberRange(150, 330),
                _ => Generator.GenerateDeterminedNumberRange(1, 4)
            };

            switch (i)
            {
                case 0:
                    obj._Str = x;
                    break;
                case 1:
                    obj._Int = x;
                    break;
                case 2:
                    obj._Wis = x;
                    break;
                case 3:
                    obj._Con = x;
                    break;
                case 4:
                    obj._Dex = x;
                    break;
            }
        }
    }

    private static void MonsterStatBoostOnPrimary(Monster obj)
    {
        var stat = Generator.RandomEnumValue<PrimaryStat>();

        switch (stat)
        {
            case PrimaryStat.STR:
                obj.BonusStr += (byte)(obj._Str * 1.2);
                obj.BonusDmg += (byte)(obj._Dmg * 1.2);
                break;

            case PrimaryStat.INT:
                obj.BonusInt += (byte)(obj._Int * 1.2);
                obj.BonusMr += (byte)(obj._Mr * 1.2);
                break;

            case PrimaryStat.WIS:
                obj.BonusWis += (byte)(obj._Wis * 1.2);
                obj.BonusMp += (int)(obj.BaseMp * 1.2);
                break;

            case PrimaryStat.CON:
                obj.BonusCon += (byte)(obj._Con * 1.2);
                obj.BonusHp += (int)(obj.BaseHp * 1.2);
                break;

            case PrimaryStat.DEX:
                obj.BonusDex += (byte)(obj._Dex * 1.2);
                obj.BonusHit += (byte)(obj._Hit * 1.2);
                break;
        }

        obj.MajorAttribute = stat;
    }

    private static void MonsterStatBoostOnType(Monster obj)
    {
        switch (obj.Template.MonsterType)
        {
            case MonsterType.None:
                break;
            case MonsterType.Physical:
                obj.BonusStr += (byte)(obj._Str * 1.2);
                obj.BonusDex += (byte)(obj._Dex * 1.2);
                obj.BonusDmg += (byte)(obj._Dmg * 1.2);
                obj.BonusHp += (int)(obj.MaximumHp * 0.012);
                break;
            case MonsterType.Magical:
                obj.BonusInt += (byte)(obj._Int * 1.2);
                obj.BonusWis += (byte)(obj._Wis * 1.2);
                obj.BonusMr += (byte)(obj._Mr * 1.2);
                break;
            case MonsterType.GodlyStr:
                obj.BonusStr += (byte)(obj._Str * 5);
                obj.BonusDex += (byte)(obj._Dex * 2.4);
                obj.BonusDmg += (byte)(obj._Dmg * 5);
                break;
            case MonsterType.GodlyInt:
                obj.BonusInt += (byte)(obj._Int * 5);
                obj.BonusWis += (byte)(obj._Wis * 2.4);
                obj.BonusMr += (byte)(obj._Mr * 5);
                break;
            case MonsterType.GodlyWis:
                obj.BonusWis += (byte)(obj._Wis * 5);
                obj.BonusMp += obj.BaseMp * 5;
                break;
            case MonsterType.GodlyCon:
                obj.BonusCon += (byte)(obj._Con * 5);
                obj.BonusHp += obj.BaseHp * 5;
                break;
            case MonsterType.GodlyDex:
                obj.BonusDex += (byte)(obj._Dex * 5);
                obj.BonusHit += (byte)(obj._Hit * 5);
                obj.BonusDmg += (byte)(obj._Dmg * 5);
                break;
            case MonsterType.Above99P:
                obj.BonusStr += (byte)(obj._Str * 5);
                obj.BonusDex += (byte)(obj._Dex * 5);
                obj.BonusDmg += (byte)(obj._Dmg * 5);
                obj.BonusHp += (int)(obj.MaximumHp * 0.03);
                break;
            case MonsterType.Above99M:
                obj.BonusInt += (byte)(obj._Int * 5);
                obj.BonusWis += (byte)(obj._Wis * 5);
                obj.BonusMr += (byte)(obj._Mr * 5);
                break;
            case MonsterType.Forsaken:
                obj.BonusStr += (byte)(obj._Str * 8);
                obj.BonusInt += (byte)(obj._Int * 8);
                obj.BonusWis += (byte)(obj._Wis * 8);
                obj.BonusCon += (byte)(obj._Con * 8);
                obj.BonusDex += (byte)(obj._Dex * 8);
                obj.BonusMr += (byte)(obj._Mr * 5);
                obj.BonusHit += (byte)(obj._Hit * 3);
                obj.BonusDmg += (byte)(obj._Dmg * 3);
                obj.BonusHp += obj.BaseHp * 4;
                obj.BonusMp += obj.BaseMp * 4;
                break;
            case MonsterType.Boss:
                obj.BonusStr += (byte)(obj._Str * 13);
                obj.BonusInt += (byte)(obj._Int * 13);
                obj.BonusWis += (byte)(obj._Wis * 13);
                obj.BonusCon += (byte)(obj._Con * 13);
                obj.BonusDex += (byte)(obj._Dex * 13);
                obj.BonusMr += (byte)(obj._Mr * 9);
                obj.BonusHit += (byte)(obj._Hit * 5);
                obj.BonusDmg += (byte)(obj._Dmg * 5);
                obj.BonusHp += obj.BaseHp * 15;
                obj.BonusMp += obj.BaseMp * 8;
                break;
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