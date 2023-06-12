using Darkages.Sprites;

namespace Darkages.Enums;

public enum MonsterEnums
{
    Pure,
    Elemental,
    Physical
}

public enum MonsterType
{
    None,
    Physical,
    Magical,
    GodlyStr,
    GodlyInt,
    GodlyWis,
    GodlyCon,
    GodlyDex,
    Above99P,
    Above99M,
    Forsaken,
    Boss
}

[Flags]
public enum MonsterRace
{
    Aberration = 1,
    Animal = 1 << 1,
    Aquatic = 1 << 2,
    Beast = 1 << 3,
    Celestial = 1 << 4,
    Contruct = 1 << 5,
    Demon = 1 << 6,
    Dragon = 1 << 7,
    Dummy = 1 << 8,
    Elemental = 1 << 9,
    Fairy = 1 << 10,
    Fiend = 1 << 11,
    Fungi = 1 << 12,
    Gargoyle = 1 << 13,
    Giant = 1 << 14,
    Goblin = 1 << 15,
    Grimlok = 1 << 16,
    Humanoid = 1 << 17,
    Inanimate = 1 << 18,
    Insect = 1 << 19,
    Kobold = 1 << 20,
    Magical = 1 << 21,
    Mukul = 1 << 22,
    Ooze = 1 << 23,
    Orc = 1 << 24,
    Plant = 18 << 25,
    Reptile = 1 << 26,
    Robotic = 1 << 27,
    Shadow = 1 << 28,
    Rodent = 1 << 29,
    Undead = 1 << 30,

    LowerBeing = Animal | Insect | Plant | Rodent,
    HigherBeing = Aberration | Celestial | Demon | Dragon | Fairy | Shadow
}

[Flags]
public enum MoodQualifer
{
    Idle = 1,
    Aggressive = 1 << 1,
    Unpredicable = 1 << 2,
    Neutral = 1 << 3,
    VeryAggressive = 1 << 4
}

public static class MonsterExtensions
{
    public static bool MoodFlagIsSet(this MoodQualifer self, MoodQualifer flag) => (self & flag) == flag;
    public static bool MonsterRaceIsSet(this MonsterRace self, MonsterRace flag) => (self & flag) == flag;

    /// <summary>
    /// Give monsters random assails depending on their level
    /// </summary>
    public static void Assails(Monster monster)
    {
        var skillList = monster.Template.Level switch
        {
            <= 11 => new List<string>
            {
                "Assail", "Onslaught", "Assault", "Clobber", "Bite", "Claw"
            },
            > 11 and <= 50 => new List<string>
            {
                "Assail", "Double Punch", "Punch", "Clobber x2", "Onslaught", "Thrust",
                "Wallop", "Assault", "Clobber", "Bite", "Claw", "Stomp", "Tail Slap"
            },
            _ => new List<string>
            {
                "Assail", "Double Punch", "Punch", "Thrash", "Clobber x2", "Onslaught",
                "Thrust", "Wallop", "Assault", "Clobber", "Slash", "Bite", "Claw",
                "Head Butt", "Mule Kick", "Stomp", "Tail Slap"
            }
        };

        var skillCount = Math.Round(monster.Level / 20d) + 2;
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

            if (!monster.Template.SkillScripts.Contains(skill))
            {
                monster.Template.SkillScripts.Add(skill);
            }

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same skill again
        }
    }

    /// <summary>
    /// Give monsters additional random abilities depending on their level
    /// This is additional to the monster racial abilities
    /// </summary>
    public static void BasicAbilities(Monster monster)
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

            if (!monster.Template.AbilityScripts.Contains(skill))
            {
                monster.Template.AbilityScripts.Add(skill);
            }

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same ability again
        }
    }

    /// <summary>
    /// Give beag spells randomly depending on their level
    /// </summary>
    public static void BeagSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case < 11:
            case > 35:
                return;
        }

        var spellList = new[]
        {
            "Beag Srad", "Beag Sal", "Beag Athar", "Beag Creag", "Beag Dorcha", "Beag Eadrom", "Beag Puinsein", "Beag Cradh"
        };

        var spellCount = Math.Round(monster.Level / 10d) + 2;
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

            if (!monster.Template.SpellScripts.Contains(spell))
            {
                monster.Template.SpellScripts.Add(spell);
            }

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give normal spells randomly depending on their level
    /// </summary>
    public static void NormalSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case <= 35:
            case > 65:
                return;
        }

        var spellList = new[]
        {
            "Srad", "Sal", "Athar", "Creag", "Dorcha", "Eadrom", "Puinsein", "Cradh"
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

            if (!monster.Template.SpellScripts.Contains(spell))
            {
                monster.Template.SpellScripts.Add(spell);
            }

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give mor spells randomly depending on their level
    /// </summary>
    public static void MorSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case <= 65:
            case > 95:
                return;
        }

        var spellList = new[]
        {
            "Mor Srad", "Mor Sal", "Mor Athar", "Mor Creag", "Mor Dorcha", "Mor Eadrom", "Mor Puinsein", "Mor Cradh", "Fas Nadur", "Blind", "Pramh"
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

            if (!monster.Template.SpellScripts.Contains(spell))
            {
                monster.Template.SpellScripts.Add(spell);
            }

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give ard spells randomly depending on their level
    /// </summary>
    public static void ArdSpells(Monster monster)
    {
        switch (monster.Template.Level)
        {
            case <= 95:
            case > 120:
                return;
        }

        var spellList = new[]
        {
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh", "Mor Fas Nadur", "Blind", "Pramh", "Silence"
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

            if (!monster.Template.SpellScripts.Contains(spell))
            {
                monster.Template.SpellScripts.Add(spell);
            }

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    /// <summary>
    /// Give master spells randomly depending on their level
    /// </summary>
    public static void MasterSpells(Monster monster)
    {
        if (monster.Template.Level <= 120) return;

        var spellList = new[]
        {
            "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh", "Ard Fas Nadur", "Blind", "Pramh", "Silence", "Dark Chain", "Defensive Stance"
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

            if (!monster.Template.SpellScripts.Contains(spell))
            {
                monster.Template.SpellScripts.Add(spell);
            }

            randomIndices.RemoveAt(index); // Remove the index to avoid assigning the same spell again
        }
    }

    public static void AberrationSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Aberration)) return;
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Lullaby Strike", "Vampiric Slash" };
        var spellList = new List<string> { "Spectral Shield", "Silence" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void AnimalSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Animal)) return;
        var skillList = new List<string> { "Bite", "Claw" };
        var abilityList = new List<string> { "Howl'n Call", "Bite'n Shake" };
        var spellList = new List<string> { "Defensive Stance" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void AquaticSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Aquatic)) return;
        var skillList = new List<string> { "Bite", "Tail Slap" };
        var abilityList = new List<string> { "Bubble Burst", "Swallow Whole" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void BeastSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Beast)) return;
        var skillList = new List<string> { "Bite", "Claw" };
        var abilityList = new List<string> { "Bite'n Shake", "Pounce", "Poison Talon" };
        var spellList = new List<string> { "Asgall" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void CelestialSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Celestial)) return;
        var skillList = new List<string> { "Thrash", "Smite", "Divine Thrust", "Slash" };
        var abilityList = new List<string> { "Titan's Cleave", "Shadow Step", "Entice" };
        var spellList = new List<string> { "Spectral Shield", "Asgall", "Perfect Defense", "Dion", "Silence" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void ContructSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Contruct)) return;
        var skillList = new List<string> { "Stomp" };
        var abilityList = new List<string> { "Titan's Cleave", "Earthly Delights" };
        var spellList = new List<string> { "Dion", "Defensive Stance" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void DemonSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Demon)) return;
        var skillList = new List<string> { "Onslaught", "Two-Handed Attack", "Dual Wield", "Slash", "Thrash" };
        var abilityList = new List<string> { "Titan's Cleave", "Sever", "Earthly Delights", "Entice", "Atlantean Weapon" };
        var spellList = new List<string> { "Asgall", "Perfect Defense", "Dion" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void DragonSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dragon)) return;
        var skillList = new List<string> { "Thrash", "Ambidextrous", "Slash", "Claw", "Tail Slap" };
        var abilityList = new List<string> { "Titan's Cleave", "Sever", "Earthly Delights", "Hurricane Kick" };
        var spellList = new List<string> { "Asgall", "Perfect Defense", "Dion", };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void ElementalSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Elemental)) return;
        var skillList = new List<string> { "Onslaught", "Assault" };
        var abilityList = new List<string> { "Atlantean Weapon", "Elemental Bane" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void FairySet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Fairy)) return;
        var skillList = new List<string> { "Ambidextrous", "Divine Thrust", "Clobber x2" };
        var abilityList = new List<string> { "Earthly Delights", "Claw Fist", "Lullaby Strike" };
        var spellList = new List<string> { "Asgall", "Spectral Shield" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void FiendSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Fiend)) return;
        var skillList = new List<string> { "Punch", "Double Punch" };
        var abilityList = new List<string> { "Stab", "Stab Twice" };
        var spellList = new List<string> { "Blind" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void FungiSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Fungi)) return;
        var skillList = new List<string> { "Wallop", "Clobber" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade", "Vampiric Slash" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void GargoyleSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Gargoyle)) return;
        var skillList = new List<string> { "Slash" };
        var abilityList = new List<string> { "Kelberoth Strike", "Palm Heel Strike" };
        var spellList = new List<string> { "Mor Dion" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void GiantSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Giant)) return;
        var skillList = new List<string> { "Stomp", "Head Butt" };
        var abilityList = new List<string> { "Golden Lair", "Double-Edged Dance" };
        var spellList = new List<string> { "Silence", "Pramh" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void GoblinSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Goblin)) return;
        var skillList = new List<string> { "Assault", "Clobber", "Wallop" };
        var abilityList = new List<string> { "Wind Slice", "Wind Blade" };
        var spellList = new List<string> { "Beag Puinsein" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void GrimlokSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Grimlok)) return;
        var skillList = new List<string> { "Wallop", "Clobber" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade" };
        var spellList = new List<string> { "Silence" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void HumanoidSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Humanoid)) return;
        var skillList = new List<string> { "Thrust", "Thrash", "Wallop" };
        var abilityList = new List<string> { "Camouflage", "Adrenaline" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void InsectSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Insect)) return;
        var skillList = new List<string> { "Bite" };
        var abilityList = new List<string> { "Corrosive Touch" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void KoboldSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Kobold)) return;
        var skillList = new List<string> { "Clobber x2", "Assault" };
        var abilityList = new List<string> { "Ebb'n Flow", "Stab", "Stab'n Twist" };
        var spellList = new List<string> { "Blind" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void MagicalSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Magical)) return;
        var spellList = new List<string> { "Aite", "Mor Fas Nadur", "Deireas Faileas" };
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void MukulSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Mukul)) return;
        var skillList = new List<string> { "Clobber", "Mule Kick", "Onslaught" };
        var abilityList = new List<string> { "Krane Kick", "Wolf Fang Fist", "Flurry", "Desolate" };
        var spellList = new List<string> { "Perfect Defense" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void OozeSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Ooze)) return;
        var skillList = new List<string> { "Wallop", "Clobber", "Clobber x2" };
        var abilityList = new List<string> { "Dual Slice", "Wind Blade", "Vampiric Slash", "Retribution" };
        var spellList = new List<string> { "Asgall" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void OrcSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Orc)) return;
        var skillList = new List<string> { "Clobber", "Thrash" };
        var abilityList = new List<string> { "Titan's Cleave", "Corrosive Touch" };
        var spellList = new List<string> { "Asgall" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void PlantSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Plant)) return;
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Corrosive Touch" };
        var spellList = new List<string> { "Silence" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void ReptileSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Reptile)) return;
        var skillList = new List<string> { "Tail Slap", "Head Butt" };
        var abilityList = new List<string> { "Pounce", "Death From Above" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void RoboticSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Robotic)) return;
        var spellList = new List<string> { "Mor Dion", "Perfect Defense" };
        monster.Template.SpellScripts.AddRange(spellList);
    }

    public static void ShadowSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Shadow)) return;
        var skillList = new List<string> { "Thrust" };
        var abilityList = new List<string> { "Lullaby Strike", "Vampiric Slash" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void RodentSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Rodent)) return;
        var skillList = new List<string> { "Bite", "Assault" };
        var abilityList = new List<string> { "Rush" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }

    public static void UndeadSet(Monster monster)
    {
        if (!monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead)) return;
        var skillList = new List<string> { "Wallop" };
        var abilityList = new List<string> { "Corrosive Touch", "Retribution" };
        monster.Template.SkillScripts.AddRange(skillList);
        monster.Template.AbilityScripts.AddRange(abilityList);
    }
}