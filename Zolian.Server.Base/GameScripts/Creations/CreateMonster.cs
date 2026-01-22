using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

using System.Numerics;

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

        // Vitality multipliers (math delegated)
        var (hpMultiplier, mpMultiplier) = MonsterMath.GetVitalityMultipliers(obj.Template.Level);

        obj.BaseHp = Generator.RandomMonsterStatVariance(obj.Template.Level * hpMultiplier);
        obj.BaseMp = Generator.RandomMonsterStatVariance(obj.Template.Level * mpMultiplier);
        obj._Mr = 50;

        // Math-driven rolls / scaling (delegated)
        MonsterMath.RollMonsterSize(obj);
        MonsterMath.RollArmorAndMrBonuses(obj);
        MonsterMath.RollExperience(obj);
        MonsterMath.RollAbility(obj);

        MonsterMath.ApplySizeStatEffects(obj);
        MonsterMath.ApplyPrimaryStatBoost(obj);
        MonsterMath.ApplyTypeBoost(obj);
        MonsterMath.ApplyArmorTypeAdjustments(obj);

        // Element alignment (delegated for Random)
        SetElementalAlignment(obj);

        // Behavior/spawn logic stays here
        SetWalkEnabled(obj);
        SetMood(obj);
        SetSpawn(obj);

        if (obj.Map == null) return null;
        if (obj.Map.IsWall(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;
        if (obj.Map.IsSpriteInLocationOnCreation(obj, (int)obj.Pos.X, (int)obj.Pos.Y)) return null;

        obj.AbandonedDate = DateTime.UtcNow;
        obj.Image = template.Image;

        Monster.InitScripting(template, map, obj);

        // Cap Monster Bonuses and Diminishing Returns (delegated)
        MonsterMath.CapAndDiminishBonuses(obj);

        // Final pass: tune endgame TTK (delegated)
        MonsterMath.ApplyHighLevelVitalityScaling(obj);

        // Set Vitality after Calculations
        obj.CurrentHp = obj.MaximumHp;
        obj.CurrentMp = obj.MaximumMp;
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
                MonsterMath.RollElementalAlignment(obj);
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
        if (_monsterRaceActions.TryGetValue(template.MonsterRace, out var raceAction))
        {
            raceAction(this, obj);
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
        var skillNames = template.SkillScripts;
        if (skillNames != null && skillNames.Count > 0)
        {
            obj.SkillScripts.EnsureCapacity(obj.SkillScripts.Count + skillNames.Count);

            for (var i = 0; i < skillNames.Count; i++)
            {
                var name = skillNames[i];
                if (!string.IsNullOrWhiteSpace(name))
                    LoadSkillScript(name, obj);
            }
        }

        var abilityNames = template.AbilityScripts;
        if (abilityNames != null && abilityNames.Count > 0)
        {
            obj.AbilityScripts.EnsureCapacity(obj.AbilityScripts.Count + abilityNames.Count);

            for (var i = 0; i < abilityNames.Count; i++)
            {
                var name = abilityNames[i];
                if (!string.IsNullOrWhiteSpace(name))
                    LoadAbilityScript(name, obj);
            }
        }

        var spellNames = template.SpellScripts;
        if (spellNames != null && spellNames.Count > 0)
        {
            obj.SpellScripts.EnsureCapacity(obj.SpellScripts.Count + spellNames.Count);

            for (var i = 0; i < spellNames.Count; i++)
            {
                var name = spellNames[i];
                if (!string.IsNullOrWhiteSpace(name))
                    LoadSpellScript(name, obj);
            }
        }
    }

    private void LoadSkillScript(string skillScriptStr, Monster obj)
    {
        if (string.IsNullOrWhiteSpace(skillScriptStr)) return;

        try
        {
            if (!ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(skillScriptStr, out var script)) return;

            var scriptName = script.ScriptName;

            if (!ScriptManager.TryCreate<SkillScript>(scriptName, out var skillScript, Skill.Create(1, script)))
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

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
        if (string.IsNullOrWhiteSpace(spellScriptStr)) return;

        try
        {
            if (!ServerSetup.Instance.GlobalSpellTemplateCache.TryGetValue(spellScriptStr, out var script)) return;

            var scriptName = script.ScriptName;

            if (!ScriptManager.TryCreate<SpellScript>(scriptName, out var spellScript, Spell.Create(1, script)))
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {spellScriptStr}\n");
                return;
            }

            spellScript.Spell.Level = 100;
            spellScript.IsScriptDefault = primary;
            obj.SpellScripts.Add(spellScript);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private void LoadAbilityScript(string skillScriptStr, Monster obj)
    {
        if (string.IsNullOrWhiteSpace(skillScriptStr)) return;

        try
        {
            if (!ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(skillScriptStr, out var script)) return;

            var scriptName = script.ScriptName;

            if (!ScriptManager.TryCreate<SkillScript>(scriptName, out var skillScript, Skill.Create(1, script)))
            {
                SentrySdk.CaptureMessage($"{template.Name}: is missing a script for {skillScriptStr}\n");
                return;
            }

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
        IReadOnlyList<string> skillList = monster.Template.Level switch
        {
            <= 11 => ["Onslaught", "Assault", "Clobber", "Bite", "Claw"],
            <= 50 =>
            [
                "Double Punch", "Punch", "Clobber x2", "Onslaught", "Thrust",
                "Wallop", "Assault", "Clobber", "Bite", "Claw", "Stomp", "Tail Slap"
            ],
            _ =>
            [
                "Double Punch", "Punch", "Thrash", "Clobber x2", "Onslaught",
                "Thrust", "Wallop", "Assault", "Clobber", "Slash", "Bite", "Claw",
                "Head Butt", "Mule Kick", "Stomp", "Tail Slap"
            ]
        };

        var skillCount = (int)(Math.Round(monster.Level / 30d) + 1);
        skillCount = Math.Min(skillCount, 12);

        PickUniqueAndApply(skillList, skillCount, skill =>
        {
            if (!ContainsSkill(monster.SkillScripts, skill))
                LoadSkillScript(skill, monster);
        });
    }

    /// <summary>
    /// Give monsters additional random abilities depending on their level
    /// This is additional to the monster racial abilities
    /// </summary>
    private void BasicAbilities(Monster monster)
    {
        if (monster.Template.Level <= 11) return;

        IReadOnlyList<string> skillList = monster.Template.Level switch
        {
            <= 25 => ["Stab", "Dual Slice", "Wind Slice", "Wind Blade"],
            <= 60 =>
            [
                "Claw Fist", "Cross Body Punch", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike",
                "Wolf Fang Fist", "Stab", "Stab'n Twist", "Stab Twice", "Desolate", "Dual Slice", "Rush",
                "Wind Slice", "Beag Suain", "Wind Blade", "Double-Edged Dance", "Bite'n Shake", "Howl'n Call",
                "Death From Above", "Pounce", "Roll Over", "Corrosive Touch"
            ],
            <= 75 =>
            [
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
                "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist", "Stab", "Stab'n Twist", "Stab Twice", "Desolate",
                "Dual Slice", "Lullaby Strike", "Rush", "Sever", "Wind Slice", "Beag Suain", "Charge", "Vampiric Slash",
                "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Bite'n Shake", "Howl'n Call", "Death From Above",
                "Pounce", "Roll Over", "Swallow Whole", "Tentacle", "Corrosive Touch"
            ],
            <= 120 =>
            [
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
                "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice",
                "Titan's Cleave", "Desolate", "Dual Slice", "Lullaby Strike", "Rush", "Sever", "Wind Slice", "Beag Suain",
                "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Retribution",
                "Flame Thrower", "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole",
                "Tentacle", "Corrosive Touch", "Tantalizing Gaze"
            ],
            _ =>
            [
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
                "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice",
                "Titan's Cleave", "Desolate", "Dual Slice", "Lullaby Strike", "Rush", "Sever", "Wind Slice", "Beag Suain",
                "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Retribution",
                "Flame Thrower", "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole",
                "Tentacle", "Corrosive Touch", "Tantalizing Gaze"
            ]
        };

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(skillList, count, ability =>
        {
            if (!ContainsAbility(monster.AbilityScripts, ability))
                LoadAbilityScript(ability, monster);
        });
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

        IReadOnlyList<string> spellList = 
        [
            "Beag Srad", "Beag Sal", "Beag Athar", "Beag Creag", "Beag Dorcha", "Beag Eadrom", "Beag Puinsein", "Beag Cradh",
            "Ao Beag Cradh"
        ];

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(spellList, count, spell =>
        {
            if (!ContainsSpell(monster.SpellScripts, spell))
                LoadSpellScript(spell, monster);
        });
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

        IReadOnlyList<string> spellList =
        [
            "Srad", "Sal", "Athar", "Creag", "Dorcha", "Eadrom", "Puinsein", "Cradh", "Ao Cradh"
        ];

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(spellList, count, spell =>
        {
            if (!ContainsSpell(monster.SpellScripts, spell))
                LoadSpellScript(spell, monster);
        });
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

        IReadOnlyList<string> spellList =
        [
            "Mor Srad", "Mor Sal", "Mor Athar", "Mor Creag", "Mor Dorcha", "Mor Eadrom", "Mor Puinsein", "Mor Cradh",
            "Fas Nadur", "Blind", "Pramh", "Ao Mor Cradh"
        ];

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(spellList, count, spell =>
        {
            if (!ContainsSpell(monster.SpellScripts, spell))
                LoadSpellScript(spell, monster);
        });
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

        IReadOnlyList<string> spellList =
        [
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh",
            "Mor Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh"
        ];

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(spellList, count, spell =>
        {
            if (!ContainsSpell(monster.SpellScripts, spell))
                LoadSpellScript(spell, monster);
        });
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

        IReadOnlyList<string> spellList =
        [
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh",
            "Ard Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh", "Ao Puinsein", "Dark Chain", "Defensive Stance"
        ];

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(spellList, count, spell =>
        {
            if (!ContainsSpell(monster.SpellScripts, spell))
                LoadSpellScript(spell, monster);
        });
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
            spellList.AddRange([
                "Uas Athar", "Uas Creag", "Uas Sal", "Uas Srad", "Uas Dorcha", "Uas Eadrom",
                "Croich Mor Cradh", "Penta Seal", "Decay"
            ]);
        }

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(spellList, count, spell =>
        {
            if (!ContainsSpell(monster.SpellScripts, spell))
                LoadSpellScript(spell, monster);
        });
    }

    internal void AberrationSet(Monster monster)
    {
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Lullaby Strike", "Vampiric Slash" };
        var spellList = new List<string> { "Spectral Shield", "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void AnimalSet(Monster monster)
    {
        var skillList = new List<string> { "Bite", "Claw" };
        var abilityList = new List<string> { "Howl'n Call", "Bite'n Shake" };
        var spellList = new List<string> { "Defensive Stance" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void AquaticSet(Monster monster)
    {
        var skillList = new List<string> { "Bite", "Tail Slap" };
        var abilityList = new List<string> { "Bubble Burst", "Swallow Whole" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void BeastSet(Monster monster)
    {
        var skillList = new List<string> { "Bite", "Claw" };
        var abilityList = new List<string> { "Bite'n Shake", "Pounce", "Poison Talon" };
        var spellList = new List<string> { "Asgall" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void CelestialSet(Monster monster)
    {
        var skillList = new List<string> { "Thrash", "Divine Thrust", "Slash", "Wallop" };
        var abilityList = new List<string> { "Titan's Cleave", "Shadow Step", "Entice", "Smite" };
        var spellList = new List<string> { "Deireas Faileas", "Asgall", "Perfect Defense", "Dion", "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void ConstructSet(Monster monster)
    {
        var skillList = new List<string> { "Stomp" };
        var abilityList = new List<string> { "Titan's Cleave", "Earthly Delights" };
        var spellList = new List<string> { "Dion", "Defensive Stance" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void DemonSet(Monster monster)
    {
        var skillList = new List<string> { "Onslaught", "Two-Handed Attack", "Dual Wield", "Slash", "Thrash" };
        var abilityList = new List<string> { "Titan's Cleave", "Sever", "Earthly Delights", "Entice" };
        var spellList = new List<string> { "Asgall", "Perfect Defense", "Dion" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void DragonSet(Monster monster)
    {
        var skillList = new List<string> { "Thrash", "Ambidextrous", "Slash", "Claw", "Tail Slap" };
        var abilityList = new List<string> { "Titan's Cleave", "Sever", "Earthly Delights", "Hurricane Kick" };
        var spellList = new List<string> { "Asgall", "Perfect Defense", "Dion", "Deireas Faileas" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void BahamutDragonSet(Monster monster)
    {
        var skillList = new List<string> { "Fire Wheel", "Thrash", "Ambidextrous", "Slash", "Claw" };
        var abilityList = new List<string> { "Megaflare", "Lava Armor", "Ember Strike", "Silent Siren" };
        var spellList = new List<string> { "Heavens Fall", "Liquid Hell", "Ao Sith Gar" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void ElementalSet(Monster monster)
    {
        var skillList = new List<string> { "Flame Thrower", "Water Cannon", "Tornado Vector", "Earth Shatter" };
        var abilityList = new List<string> { "Elemental Bane" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void FairySet(Monster monster)
    {
        var skillList = new List<string> { "Ambidextrous", "Divine Thrust", "Clobber x2" };
        var abilityList = new List<string> { "Earthly Delights", "Claw Fist", "Lullaby Strike" };
        var spellList = new List<string> { "Asgall", "Spectral Shield", "Deireas Faileas" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void FiendSet(Monster monster)
    {
        var skillList = new List<string> { "Punch", "Double Punch" };
        var abilityList = new List<string> { "Stab", "Stab Twice" };
        var spellList = new List<string> { "Blind" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void FungiSet(Monster monster)
    {
        var skillList = new List<string> { "Wallop", "Clobber" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade", "Vampiric Slash" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void GargoyleSet(Monster monster)
    {
        var skillList = new List<string> { "Slash" };
        var abilityList = new List<string> { "Palm Heel Strike" };
        var spellList = new List<string> { "Mor Dion" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void GiantSet(Monster monster)
    {
        var skillList = new List<string> { "Stomp", "Head Butt" };
        var abilityList = new List<string> { "Golden Lair", "Double-Edged Dance" };
        var spellList = new List<string> { "Silence", "Pramh" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void GoblinSet(Monster monster)
    {
        var skillList = new List<string> { "Assault", "Clobber", "Wallop" };
        var abilityList = new List<string> { "Wind Slice", "Wind Blade" };
        var spellList = new List<string> { "Beag Puinsein" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void GrimlokSet(Monster monster)
    {
        var skillList = new List<string> { "Wallop", "Clobber" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade" };
        var spellList = new List<string> { "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void HumanoidSet(Monster monster)
    {
        var skillList = new List<string> { "Thrust", "Thrash", "Wallop" };
        var abilityList = new List<string> { "Camouflage", "Adrenaline" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void ShapeShifter(Monster monster)
    {
        var skillList = new List<string> { "Thrust", "Thrash", "Wallop" };
        var spellList = new List<string> { "Spring Trap", "Snare Trap", "Blind", "Prahm" };
        MonsterLoader(skillList, [], spellList, monster);
    }

    internal void InsectSet(Monster monster)
    {
        var skillList = new List<string> { "Bite" };
        var abilityList = new List<string> { "Corrosive Touch" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void KoboldSet(Monster monster)
    {
        var skillList = new List<string> { "Clobber x2", "Assault" };
        var abilityList = new List<string> { "Ebb'n Flow", "Stab", "Stab'n Twist" };
        var spellList = new List<string> { "Blind" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void MagicalSet(Monster monster)
    {
        var spellList = new List<string> { "Aite", "Mor Fas Nadur", "Deireas Faileas" };
        MonsterLoader([], [], spellList, monster);
    }

    internal void MukulSet(Monster monster)
    {
        var skillList = new List<string> { "Clobber", "Mule Kick", "Onslaught" };
        var abilityList = new List<string> { "Krane Kick", "Wolf Fang Fist", "Flurry", "Desolate" };
        var spellList = new List<string> { "Perfect Defense" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void OozeSet(Monster monster)
    {
        var skillList = new List<string> { "Wallop", "Clobber", "Clobber x2" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade", "Vampiric Slash", "Retribution" };
        var spellList = new List<string> { "Asgall" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void OrcSet(Monster monster)
    {
        var skillList = new List<string> { "Clobber", "Thrash" };
        var abilityList = new List<string> { "Titan's Cleave", "Corrosive Touch" };
        var spellList = new List<string> { "Asgall" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void PlantSet(Monster monster)
    {
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Corrosive Touch" };
        var spellList = new List<string> { "Silence" };
        MonsterLoader(skillList, abilityList, spellList, monster);
    }

    internal void ReptileSet(Monster monster)
    {
        var skillList = new List<string> { "Tail Slap", "Head Butt" };
        var abilityList = new List<string> { "Pounce", "Death From Above" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void RoboticSet(Monster monster)
    {
        var spellList = new List<string> { "Mor Dion", "Perfect Defense" };
        MonsterLoader([], [], spellList, monster);
    }

    internal void ShadowSet(Monster monster)
    {
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Lullaby Strike", "Vampiric Slash" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void RodentSet(Monster monster)
    {
        var skillList = new List<string> { "Bite", "Assault" };
        var abilityList = new List<string> { "Rush" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    internal void UndeadSet(Monster monster)
    {
        var skillList = new List<string> { "Wallop" };
        var abilityList = new List<string> { "Corrosive Touch", "Retribution" };
        MonsterLoader(skillList, abilityList, [], monster);
    }

    private static void PickUniqueAndApply(IReadOnlyList<string> list, int pickCount, Action<string> apply)
    {
        if (pickCount <= 0 || list.Count == 0)
            return;

        if (pickCount > list.Count)
            pickCount = list.Count;

        Span<int> idx = list.Count <= 128 ? stackalloc int[list.Count] : new int[list.Count];

        for (int i = 0; i < idx.Length; i++)
            idx[i] = i;

        // Partial shuffle: only shuffle the first pickCount positions
        for (int i = 0; i < pickCount; i++)
        {
            int j = Random.Shared.Next(i, idx.Length);
            (idx[i], idx[j]) = (idx[j], idx[i]);

            apply(list[idx[i]]);
        }
    }

    private static bool ContainsSkill(List<SkillScript> scripts, string name)
    {
        for (int i = 0; i < scripts.Count; i++)
            if (scripts[i].Skill.Template.ScriptName == name)
                return true;
        return false;
    }

    private static bool ContainsAbility(List<SkillScript> scripts, string name)
    {
        for (int i = 0; i < scripts.Count; i++)
            if (scripts[i].Skill.Template.ScriptName == name)
                return true;
        return false;
    }

    private static bool ContainsSpell(List<SpellScript> scripts, string name)
    {
        for (int i = 0; i < scripts.Count; i++)
            if (scripts[i].Spell.Template.ScriptName == name)
                return true;
        return false;
    }

    private void MonsterLoader(List<string> skills, List<string> abilities, List<string> spells, Monster monster)
    {
        if (skills.Count != 0)
            foreach (var skill in skills)
                LoadSkillScript(skill, monster);

        if (abilities.Count != 0)
            foreach (var ability in abilities)
                LoadAbilityScript(ability, monster);

        if (spells.Count == 0) return;
        foreach (var spell in spells)
            LoadSpellScript(spell, monster);
    }
}