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
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var skill = skillList[randomIndices[index]];
            var check = monster.SkillScripts.Any(script => script.Skill.Template.ScriptName == skill);

            if (!check)
                LoadSkillScript(skill, monster);

            randomIndices.RemoveAt(index);
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
            _ => new List<string>
            {
                "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
                "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice",
                "Titan's Cleave", "Desolate", "Dual Slice", "Lullaby Strike", "Rush", "Sever", "Wind Slice", "Beag Suain",
                "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Retribution",
                "Flame Thrower", "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole",
                "Tentacle", "Corrosive Touch", "Tantalizing Gaze"
            }
        };

        var skillCount = Math.Round(monster.Level / 30d) + 1;
        skillCount = Math.Min(skillCount, 5); // Max 5 abilities regardless of level
        var randomIndices = Enumerable.Range(0, skillList.Count).ToList();

        for (var i = 0; i < skillCount; i++)
        {
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var skill = skillList[randomIndices[index]];
            var check = monster.AbilityScripts.Any(script => script.Skill.Template.ScriptName == skill);

            if (!check)
                LoadAbilityScript(skill, monster);

            randomIndices.RemoveAt(index);
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
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index);
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
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index);
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
        spellCount = Math.Min(spellCount, 5);
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index);
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
        spellCount = Math.Min(spellCount, 5);
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index);
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
        spellCount = Math.Min(spellCount, 5);
        var randomIndices = Enumerable.Range(0, spellList.Length).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index);
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
            spellList.AddRange([
                "Uas Athar", "Uas Creag", "Uas Sal", "Uas Srad", "Uas Dorcha", "Uas Eadrom",
                "Croich Mor Cradh", "Penta Seal", "Decay"
            ]);
        }

        var spellCount = Math.Round(monster.Level / 200d) + 2;
        spellCount = Math.Min(spellCount, 5);
        var randomIndices = Enumerable.Range(0, spellList.Count).ToList();

        for (var i = 0; i < spellCount; i++)
        {
            if (randomIndices.Count == 0) break;

            var index = Random.Shared.Next(randomIndices.Count);
            var spell = spellList[randomIndices[index]];
            var check = monster.SpellScripts.Any(script => script.Spell.Template.ScriptName == spell);

            if (!check)
                LoadSpellScript(spell, monster);

            randomIndices.RemoveAt(index);
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
        var abilityList = new List<string> { "Titan's Cleave", "Sever", "Earthly Delights", "Entice" };
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
        var skillList = new List<string> { "Flame Thrower", "Water Cannon", "Tornado Vector", "Earth Shatter" };
        var abilityList = new List<string> { "Elemental Bane" };
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
                LoadSkillScript(skill, monster);

        if (abilities.Count != 0)
            foreach (var ability in abilities)
                LoadAbilityScript(ability, monster);

        if (spells.Count == 0) return;
        foreach (var spell in spells)
            LoadSpellScript(spell, monster);
    }
}