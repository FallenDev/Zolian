using System.Collections.Frozen;

using Darkages.Enums;
using Darkages.GameScripts.Creations;
using Darkages.Sprites.Entity;

namespace Darkages.Models;

public class MonsterRaceModel
{
    public static readonly FrozenDictionary<MonsterRace, Action<CreateMonster, Monster>> _monsterRaceActions =
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
}
