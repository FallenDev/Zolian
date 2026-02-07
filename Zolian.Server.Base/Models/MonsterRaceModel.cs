using System.Collections.Frozen;

using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Sprites.Entity;

namespace Darkages.Models;

public class MonsterRaceModel
{
    internal static readonly FrozenDictionary<MonsterRace, Action<CreateMonster, Monster>> _monsterRaceActions =
    new Dictionary<MonsterRace, Action<CreateMonster, Monster>>
    {
        [MonsterRace.Aberration] = static (self, m) => self.AberrationSet(m),
        [MonsterRace.Animal] = static (self, m) => self.AnimalSet(m),
        [MonsterRace.Aquatic] = static (self, m) => self.AquaticSet(m),
        [MonsterRace.Beast] = static (self, m) => self.BeastSet(m),
        [MonsterRace.Celestial] = static (self, m) => self.CelestialSet(m),
        [MonsterRace.Construct] = static (self, m) => self.ConstructSet(m),
        [MonsterRace.Demon] = static (self, m) => self.DemonSet(m),
        [MonsterRace.Dragon] = static (self, m) => self.DragonSet(m),
        [MonsterRace.Bahamut] = static (self, m) => self.BahamutDragonSet(m),
        [MonsterRace.Elemental] = static (self, m) => self.ElementalSet(m),
        [MonsterRace.Fairy] = static (self, m) => self.FairySet(m),
        [MonsterRace.Fiend] = static (self, m) => self.FiendSet(m),
        [MonsterRace.Fungi] = static (self, m) => self.FungiSet(m),
        [MonsterRace.Gargoyle] = static (self, m) => self.GargoyleSet(m),
        [MonsterRace.Giant] = static (self, m) => self.GiantSet(m),
        [MonsterRace.Goblin] = static (self, m) => self.GoblinSet(m),
        [MonsterRace.Grimlok] = static (self, m) => self.GrimlokSet(m),
        [MonsterRace.Humanoid] = static (self, m) => self.HumanoidSet(m),
        [MonsterRace.ShapeShifter] = static (self, m) => self.ShapeShifter(m),
        [MonsterRace.Insect] = static (self, m) => self.InsectSet(m),
        [MonsterRace.Kobold] = static (self, m) => self.KoboldSet(m),
        [MonsterRace.Magical] = static (self, m) => self.MagicalSet(m),
        [MonsterRace.Mukul] = static (self, m) => self.MukulSet(m),
        [MonsterRace.Ooze] = static (self, m) => self.OozeSet(m),
        [MonsterRace.Orc] = static (self, m) => self.OrcSet(m),
        [MonsterRace.Plant] = static (self, m) => self.PlantSet(m),
        [MonsterRace.Reptile] = static (self, m) => self.ReptileSet(m),
        [MonsterRace.Robotic] = static (self, m) => self.RoboticSet(m),
        [MonsterRace.Shadow] = static (self, m) => self.ShadowSet(m),
        [MonsterRace.Rodent] = static (self, m) => self.RodentSet(m),
        [MonsterRace.Undead] = static (self, m) => self.UndeadSet(m),
    }.ToFrozenDictionary();

    #region Static Kits & Tables

    // Assails: per level bracket
    internal static readonly string[] Assails_11 =
    [
        "Onslaught", "Assault", "Clobber", "Bite", "Claw"
    ];

    internal static readonly string[] Assails_50 =
    [
        "Double Punch", "Punch", "Clobber x2", "Onslaught", "Thrust",
        "Wallop", "Assault", "Clobber", "Bite", "Claw", "Stomp", "Tail Slap"
    ];

    internal static readonly string[] Assails_Any =
    [
        "Double Punch", "Punch", "Thrash", "Clobber x2", "Onslaught",
        "Thrust", "Wallop", "Assault", "Clobber", "Slash", "Bite", "Claw",
        "Head Butt", "Mule Kick", "Stomp", "Tail Slap"
    ];

    // Basic abilities: per level bracket
    internal static readonly string[] BasicAbilities_25 =
    [
        "Stab", "Dual Slice", "Wind Slice", "Wind Blade"
    ];

    internal static readonly string[] BasicAbilities_60 =
    [
        "Claw Fist", "Cross Body Punch", "Knife Hand Strike", "Krane Kick", "Palm Heel Strike",
        "Wolf Fang Fist", "Stab", "Stab'n Twist", "Stab Twice", "Desolate", "Dual Slice", "Rush",
        "Wind Slice", "Beag Suain", "Wind Blade", "Double-Edged Dance", "Bite'n Shake", "Howl'n Call",
        "Death From Above", "Pounce", "Roll Over", "Corrosive Touch"
    ];

    internal static readonly string[] BasicAbilities_75 =
    [
        "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
        "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist", "Stab", "Stab'n Twist", "Stab Twice", "Desolate",
        "Dual Slice", "Lullaby Strike", "Rush", "Sever", "Wind Slice", "Beag Suain", "Charge", "Vampiric Slash",
        "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Bite'n Shake", "Howl'n Call", "Death From Above",
        "Pounce", "Roll Over", "Swallow Whole", "Tentacle", "Corrosive Touch"
    ];

    internal static readonly string[] BasicAbilities_120 =
    [
        "Ambush", "Claw Fist", "Cross Body Punch", "Hammer Twist", "Hurricane Kick", "Knife Hand Strike",
        "Krane Kick", "Palm Heel Strike", "Wolf Fang Fist", "Flurry", "Stab", "Stab'n Twist", "Stab Twice",
        "Titan's Cleave", "Desolate", "Dual Slice", "Lullaby Strike", "Rush", "Sever", "Wind Slice", "Beag Suain",
        "Charge", "Vampiric Slash", "Wind Blade", "Double-Edged Dance", "Ebb'n Flow", "Retribution",
        "Flame Thrower", "Bite'n Shake", "Howl'n Call", "Death From Above", "Pounce", "Roll Over", "Swallow Whole",
        "Tentacle", "Corrosive Touch", "Tantalizing Gaze"
    ];

    internal static readonly string[] BasicAbilities_Any = BasicAbilities_120;

    // Spell tiers
    internal static readonly string[] BeagSpellTable =
    [
        "Beag Srad", "Beag Sal", "Beag Athar", "Beag Creag", "Beag Dorcha", "Beag Eadrom", "Beag Puinsein", "Beag Cradh",
        "Ao Beag Cradh"
    ];

    internal static readonly string[] NormalSpellTable =
    [
        "Srad", "Sal", "Athar", "Creag", "Dorcha", "Eadrom", "Puinsein", "Cradh", "Ao Cradh"
    ];

    internal static readonly string[] MorSpellTable =
    [
        "Mor Srad", "Mor Sal", "Mor Athar", "Mor Creag", "Mor Dorcha", "Mor Eadrom", "Mor Puinsein", "Mor Cradh",
        "Fas Nadur", "Blind", "Pramh", "Ao Mor Cradh"
    ];

    internal static readonly string[] ArdSpellTable =
    [
        "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh",
        "Mor Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh"
    ];

    internal static readonly string[] MasterSpellTable =
    [
        "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein", "Ard Cradh",
        "Ard Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh", "Ao Puinsein", "Dark Chain", "Defensive Stance"
    ];

    // Job spells
    internal static readonly string[] JobSpellBase =
    [
        "Ard Srad", "Ard Sal", "Ard Athar", "Ard Creag", "Ard Dorcha", "Ard Eadrom", "Ard Puinsein",
        "Croich Beag Cradh", "Ard Fas Nadur", "Blind", "Pramh", "Silence", "Ao Ard Cradh", "Ao Puinsein",
        "Dark Chain", "Defensive Stance"
    ];

    internal static readonly string[] JobSpell500Plus =
    [
        "Uas Athar", "Uas Creag", "Uas Sal", "Uas Srad", "Uas Dorcha", "Uas Eadrom",
        "Croich Mor Cradh", "Penta Seal", "Decay"
    ];

    // Race kits
    internal static readonly string[] AberrationSkills = ["Thrust"];
    internal static readonly string[] AberrationAbilities = ["Lullaby Strike", "Vampiric Slash"];
    internal static readonly string[] AberrationSpells = ["Spectral Shield", "Silence"];

    internal static readonly string[] AnimalSkills = ["Bite", "Claw"];
    internal static readonly string[] AnimalAbilities = ["Howl'n Call", "Bite'n Shake"];
    internal static readonly string[] AnimalSpells = ["Defensive Stance"];

    internal static readonly string[] AquaticSkills = ["Bite", "Tail Slap"];
    internal static readonly string[] AquaticAbilities = ["Bubble Burst", "Swallow Whole"];
    internal static readonly string[] AquaticSpells = [];

    internal static readonly string[] BeastSkills = ["Bite", "Claw"];
    internal static readonly string[] BeastAbilities = ["Bite'n Shake", "Pounce", "Poison Talon"];
    internal static readonly string[] BeastSpells = ["Asgall"];

    internal static readonly string[] CelestialSkills = ["Thrash", "Divine Thrust", "Slash", "Wallop"];
    internal static readonly string[] CelestialAbilities = ["Titan's Cleave", "Shadow Step", "Entice", "Smite"];
    internal static readonly string[] CelestialSpells = ["Deireas Faileas", "Asgall", "Perfect Defense", "Dion", "Silence"];

    internal static readonly string[] ConstructSkills = ["Stomp"];
    internal static readonly string[] ConstructAbilities = ["Titan's Cleave", "Earthly Delights"];
    internal static readonly string[] ConstructSpells = ["Dion", "Defensive Stance"];

    internal static readonly string[] DemonSkills = ["Onslaught", "Two-Handed Attack", "Dual Wield", "Slash", "Thrash"];
    internal static readonly string[] DemonAbilities = ["Titan's Cleave", "Sever", "Earthly Delights", "Entice"];
    internal static readonly string[] DemonSpells = ["Asgall", "Perfect Defense", "Dion"];

    internal static readonly string[] DragonSkills = ["Thrash", "Ambidextrous", "Slash", "Claw", "Tail Slap"];
    internal static readonly string[] DragonAbilities = ["Titan's Cleave", "Sever", "Earthly Delights", "Hurricane Kick"];
    internal static readonly string[] DragonSpells = ["Asgall", "Perfect Defense", "Dion", "Deireas Faileas"];

    internal static readonly string[] BahamutSkills = ["Fire Wheel", "Thrash", "Ambidextrous", "Slash", "Claw"];
    internal static readonly string[] BahamutAbilities = ["Megaflare", "Lava Armor", "Ember Strike", "Silent Siren"];
    internal static readonly string[] BahamutSpells = ["Heavens Fall", "Liquid Hell", "Ao Sith Gar"];

    internal static readonly string[] ElementalSkills = [];
    internal static readonly string[] ElementalAbilities = ["Flame Thrower", "Water Cannon", "Tornado Vector", "Earth Shatter", "Elemental Bane"];
    internal static readonly string[] ElementalSpells = [];

    internal static readonly string[] FairySkills = ["Ambidextrous", "Divine Thrust", "Clobber x2"];
    internal static readonly string[] FairyAbilities = ["Earthly Delights", "Claw Fist", "Lullaby Strike"];
    internal static readonly string[] FairySpells = ["Asgall", "Spectral Shield", "Deireas Faileas"];

    internal static readonly string[] FiendSkills = ["Punch", "Double Punch"];
    internal static readonly string[] FiendAbilities = ["Stab", "Stab Twice"];
    internal static readonly string[] FiendSpells = ["Blind"];

    internal static readonly string[] FungiSkills = ["Wallop", "Clobber"];
    internal static readonly string[] FungiAbilities = ["Dual Slice", "Wind Blade", "Vampiric Slash"];
    internal static readonly string[] FungiSpells = [];

    internal static readonly string[] GargoyleSkills = ["Slash"];
    internal static readonly string[] GargoyleAbilities = ["Palm Heel Strike"];
    internal static readonly string[] GargoyleSpells = ["Mor Dion"];

    internal static readonly string[] GiantSkills = ["Stomp", "Head Butt"];
    internal static readonly string[] GiantAbilities = ["Golden Lair", "Double-Edged Dance"];
    internal static readonly string[] GiantSpells = ["Silence", "Pramh"];

    internal static readonly string[] GoblinSkills = ["Assault", "Clobber", "Wallop"];
    internal static readonly string[] GoblinAbilities = ["Wind Slice", "Wind Blade"];
    internal static readonly string[] GoblinSpells = ["Beag Puinsein"];

    internal static readonly string[] GrimlokSkills = ["Wallop", "Clobber"];
    internal static readonly string[] GrimlokAbilities = ["Dual Slice", "Wind Blade"];
    internal static readonly string[] GrimlokSpells = ["Silence"];

    internal static readonly string[] HumanoidSkills = ["Thrust", "Thrash", "Wallop"];
    internal static readonly string[] HumanoidAbilities = ["Camouflage", "Adrenaline"];
    internal static readonly string[] HumanoidSpells = [];

    internal static readonly string[] ShapeShifterSkills = ["Thrust", "Thrash", "Wallop"];
    internal static readonly string[] ShapeShifterAbilities = [];
    internal static readonly string[] ShapeShifterSpells = ["Spring Trap", "Snare Trap", "Blind", "Prahm"];

    internal static readonly string[] InsectSkills = ["Bite"];
    internal static readonly string[] InsectAbilities = ["Corrosive Touch"];
    internal static readonly string[] InsectSpells = [];

    internal static readonly string[] KoboldSkills = ["Clobber x2", "Assault"];
    internal static readonly string[] KoboldAbilities = ["Ebb'n Flow", "Stab", "Stab'n Twist"];
    internal static readonly string[] KoboldSpells = ["Blind"];

    internal static readonly string[] MagicalSkills = [];
    internal static readonly string[] MagicalAbilities = [];
    internal static readonly string[] MagicalSpells = ["Aite", "Mor Fas Nadur", "Deireas Faileas"];

    internal static readonly string[] MukulSkills = ["Clobber", "Mule Kick", "Onslaught"];
    internal static readonly string[] MukulAbilities = ["Krane Kick", "Wolf Fang Fist", "Flurry", "Desolate"];
    internal static readonly string[] MukulSpells = ["Perfect Defense"];

    internal static readonly string[] OozeSkills = ["Wallop", "Clobber", "Clobber x2"];
    internal static readonly string[] OozeAbilities = ["Dual Slice", "Wind Blade", "Vampiric Slash", "Retribution"];
    internal static readonly string[] OozeSpells = ["Asgall"];

    internal static readonly string[] OrcSkills = ["Clobber", "Thrash"];
    internal static readonly string[] OrcAbilities = ["Titan's Cleave", "Corrosive Touch"];
    internal static readonly string[] OrcSpells = ["Asgall"];

    internal static readonly string[] PlantSkills = ["Thrust"];
    internal static readonly string[] PlantAbilities = ["Corrosive Touch"];
    internal static readonly string[] PlantSpells = ["Silence"];

    internal static readonly string[] ReptileSkills = ["Tail Slap", "Head Butt"];
    internal static readonly string[] ReptileAbilities = ["Pounce", "Death From Above"];
    internal static readonly string[] ReptileSpells = [];

    internal static readonly string[] RoboticSkills = [];
    internal static readonly string[] RoboticAbilities = [];
    internal static readonly string[] RoboticSpells = ["Mor Dion", "Perfect Defense"];

    internal static readonly string[] ShadowSkills = ["Thrust"];
    internal static readonly string[] ShadowAbilities = ["Lullaby Strike", "Vampiric Slash"];
    internal static readonly string[] ShadowSpells = [];

    internal static readonly string[] RodentSkills = ["Bite", "Assault"];
    internal static readonly string[] RodentAbilities = ["Rush"];
    internal static readonly string[] RodentSpells = [];

    internal static readonly string[] UndeadSkills = ["Wallop"];
    internal static readonly string[] UndeadAbilities = ["Corrosive Touch", "Retribution"];
    internal static readonly string[] UndeadSpells = [];

    #endregion
}
