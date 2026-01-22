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

    private void LoadSkills(string[] skills, Monster monster)
    {
        for (int i = 0; i < skills.Length; i++)
            LoadSkillScript(skills[i], monster);
    }

    private void LoadAbilities(string[] abilities, Monster monster)
    {
        for (int i = 0; i < abilities.Length; i++)
            LoadAbilityScript(abilities[i], monster);
    }

    private void LoadSpells(string[] spells, Monster monster)
    {
        for (int i = 0; i < spells.Length; i++)
            LoadSpellScript(spells[i], monster);
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
            <= 11 => Assails_11,
            <= 50 => Assails_50,
            _ => Assails_Any
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
            <= 25 => BasicAbilities_25,
            <= 60 => BasicAbilities_60,
            <= 75 => BasicAbilities_75,
            <= 120 => BasicAbilities_120,
            _ => BasicAbilities_Any
        };

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(skillList, count, ability =>
        {
            if (!ContainsAbility(monster.AbilityScripts, ability))
                LoadAbilityScript(ability, monster);
        });
    }

    #region Level-Based Spells

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

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(BeagSpellTable, count, spell =>
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

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(NormalSpellTable, count, spell =>
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

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(MorSpellTable, count, spell =>
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

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(ArdSpellTable, count, spell =>
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

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        PickUniqueAndApply(MasterSpellTable, count, spell =>
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

        var count = (int)(Math.Round(monster.Level / 30d) + 1);
        count = Math.Min(count, 5);

        if (monster.Template.Level < 500)
        {
            PickUniqueAndApply(JobSpellBase, count, spell =>
            {
                if (!ContainsSpell(monster.SpellScripts, spell))
                    LoadSpellScript(spell, monster);
            });
            return;
        }

        PickUniqueAndApplyConcat(JobSpellBase, JobSpell500Plus, count, spell =>
        {
            if (!ContainsSpell(monster.SpellScripts, spell))
                LoadSpellScript(spell, monster);
        });
    }

    #endregion

    #region Racial Abilities and Racial Spells

    internal void AberrationSet(Monster monster)
    {
        LoadSkills(AberrationSkills, monster);
        LoadAbilities(AberrationAbilities, monster);
        LoadSpells(AberrationSpells, monster);
    }

    internal void AnimalSet(Monster monster)
    {
        LoadSkills(AnimalSkills, monster);
        LoadAbilities(AnimalAbilities, monster);
        LoadSpells(AnimalSpells, monster);
    }

    internal void AquaticSet(Monster monster)
    {
        LoadSkills(AquaticSkills, monster);
        LoadAbilities(AquaticAbilities, monster);
        LoadSpells(AquaticSpells, monster);
    }

    internal void BeastSet(Monster monster)
    {
        LoadSkills(BeastSkills, monster);
        LoadAbilities(BeastAbilities, monster);
        LoadSpells(BeastSpells, monster);
    }

    internal void CelestialSet(Monster monster)
    {
        LoadSkills(CelestialSkills, monster);
        LoadAbilities(CelestialAbilities, monster);
        LoadSpells(CelestialSpells, monster);
    }

    internal void ConstructSet(Monster monster)
    {
        LoadSkills(ConstructSkills, monster);
        LoadAbilities(ConstructAbilities, monster);
        LoadSpells(ConstructSpells, monster);
    }

    internal void DemonSet(Monster monster)
    {
        LoadSkills(DemonSkills, monster);
        LoadAbilities(DemonAbilities, monster);
        LoadSpells(DemonSpells, monster);
    }

    internal void DragonSet(Monster monster)
    {
        LoadSkills(DragonSkills, monster);
        LoadAbilities(DragonAbilities, monster);
        LoadSpells(DragonSpells, monster);
    }

    internal void BahamutDragonSet(Monster monster)
    {
        LoadSkills(BahamutSkills, monster);
        LoadAbilities(BahamutAbilities, monster);
        LoadSpells(BahamutSpells, monster);
    }

    internal void ElementalSet(Monster monster)
    {
        LoadSkills(ElementalSkills, monster);
        LoadAbilities(ElementalAbilities, monster);
        LoadSpells(ElementalSpells, monster);
    }

    internal void FairySet(Monster monster)
    {
        LoadSkills(FairySkills, monster);
        LoadAbilities(FairyAbilities, monster);
        LoadSpells(FairySpells, monster);
    }

    internal void FiendSet(Monster monster)
    {
        LoadSkills(FiendSkills, monster);
        LoadAbilities(FiendAbilities, monster);
        LoadSpells(FiendSpells, monster);
    }

    internal void FungiSet(Monster monster)
    {
        LoadSkills(FungiSkills, monster);
        LoadAbilities(FungiAbilities, monster);
        LoadSpells(FungiSpells, monster);
    }

    internal void GargoyleSet(Monster monster)
    {
        LoadSkills(GargoyleSkills, monster);
        LoadAbilities(GargoyleAbilities, monster);
        LoadSpells(GargoyleSpells, monster);
    }

    internal void GiantSet(Monster monster)
    {
        LoadSkills(GiantSkills, monster);
        LoadAbilities(GiantAbilities, monster);
        LoadSpells(GiantSpells, monster);
    }

    internal void GoblinSet(Monster monster)
    {
        LoadSkills(GoblinSkills, monster);
        LoadAbilities(GoblinAbilities, monster);
        LoadSpells(GoblinSpells, monster);
    }

    internal void GrimlokSet(Monster monster)
    {
        LoadSkills(GrimlokSkills, monster);
        LoadAbilities(GrimlokAbilities, monster);
        LoadSpells(GrimlokSpells, monster);
    }

    internal void HumanoidSet(Monster monster)
    {
        LoadSkills(HumanoidSkills, monster);
        LoadAbilities(HumanoidAbilities, monster);
        LoadSpells(HumanoidSpells, monster);
    }

    internal void ShapeShifter(Monster monster)
    {
        LoadSkills(ShapeShifterSkills, monster);
        LoadAbilities(ShapeShifterAbilities, monster);
        LoadSpells(ShapeShifterSpells, monster);
    }

    internal void InsectSet(Monster monster)
    {
        LoadSkills(InsectSkills, monster);
        LoadAbilities(InsectAbilities, monster);
        LoadSpells(InsectSpells, monster);
    }

    internal void KoboldSet(Monster monster)
    {
        LoadSkills(KoboldSkills, monster);
        LoadAbilities(KoboldAbilities, monster);
        LoadSpells(KoboldSpells, monster);
    }

    internal void MagicalSet(Monster monster)
    {
        LoadSkills(MagicalSkills, monster);
        LoadAbilities(MagicalAbilities, monster);
        LoadSpells(MagicalSpells, monster);
    }

    internal void MukulSet(Monster monster)
    {
        LoadSkills(MukulSkills, monster);
        LoadAbilities(MukulAbilities, monster);
        LoadSpells(MukulSpells, monster);
    }

    internal void OozeSet(Monster monster)
    {
        LoadSkills(OozeSkills, monster);
        LoadAbilities(OozeAbilities, monster);
        LoadSpells(OozeSpells, monster);
    }

    internal void OrcSet(Monster monster)
    {
        LoadSkills(OrcSkills, monster);
        LoadAbilities(OrcAbilities, monster);
        LoadSpells(OrcSpells, monster);
    }

    internal void PlantSet(Monster monster)
    {
        LoadSkills(PlantSkills, monster);
        LoadAbilities(PlantAbilities, monster);
        LoadSpells(PlantSpells, monster);
    }

    internal void ReptileSet(Monster monster)
    {
        LoadSkills(ReptileSkills, monster);
        LoadAbilities(ReptileAbilities, monster);
        LoadSpells(ReptileSpells, monster);
    }

    internal void RoboticSet(Monster monster)
    {
        LoadSkills(RoboticSkills, monster);
        LoadAbilities(RoboticAbilities, monster);
        LoadSpells(RoboticSpells, monster);
    }

    internal void ShadowSet(Monster monster)
    {
        LoadSkills(ShadowSkills, monster);
        LoadAbilities(ShadowAbilities, monster);
        LoadSpells(ShadowSpells, monster);
    }

    internal void RodentSet(Monster monster)
    {
        LoadSkills(RodentSkills, monster);
        LoadAbilities(RodentAbilities, monster);
        LoadSpells(RodentSpells, monster);
    }

    internal void UndeadSet(Monster monster)
    {
        LoadSkills(UndeadSkills, monster);
        LoadAbilities(UndeadAbilities, monster);
        LoadSpells(UndeadSpells, monster);
    }

    #endregion

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

    private static void PickUniqueAndApplyConcat(IReadOnlyList<string> a, IReadOnlyList<string> b, int pickCount, Action<string> apply)
    {
        int total = a.Count + b.Count;
        if (pickCount <= 0 || total == 0)
            return;

        if (pickCount > total)
            pickCount = total;

        Span<int> idx = total <= 128 ? stackalloc int[total] : new int[total];

        for (int i = 0; i < total; i++)
            idx[i] = i;

        for (int i = 0; i < pickCount; i++)
        {
            int j = Random.Shared.Next(i, total);
            (idx[i], idx[j]) = (idx[j], idx[i]);

            int k = idx[i];
            apply(k < a.Count ? a[k] : b[k - a.Count]);
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
}