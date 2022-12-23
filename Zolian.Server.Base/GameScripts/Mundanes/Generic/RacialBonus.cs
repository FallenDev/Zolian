using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic
{
    public static class RacialBonus
    {

        public static void HumanSkill(GameClient client, string skill)
        {
            client.Aisling._Str += 1;
            client.Aisling._Int += 1;
            client.Aisling._Wis += 1;
            client.Aisling._Con += 1;
            client.Aisling._Dex += 1;
            client.Aisling._Luck += 1;

            var raceBool = Skill.GiveTo(client.Aisling, skill, 1);
            if (raceBool) client.LoadSkillBook();

            RacialLegend(client);
        }

        public static void HumanSpell(GameClient client, string spell)
        {
            client.Aisling._Str += 1;
            client.Aisling._Int += 1;
            client.Aisling._Wis += 1;
            client.Aisling._Con += 1;
            client.Aisling._Dex += 1;
            client.Aisling._Luck += 1;

            var raceBool = Spell.GiveTo(client.Aisling, spell, 1);
            if (raceBool) client.LoadSpellBook();

            RacialLegend(client);
        }

        public static void Dwarf(GameClient client)
        {
            client.Aisling._Str += 2;
            client.Aisling._Con += 2;
            client.Aisling._ac += 2;
            client.Aisling.RaceSkill = null;
            client.Aisling.RaceSpell = "Stone Skin";

            var raceBool = Spell.GiveTo(client.Aisling, client.Aisling.RaceSpell, 1);
            if (raceBool) client.LoadSpellBook();

            RacialLegend(client);
        }

        public static void Halfling(GameClient client)
        {
            client.Aisling._Int += 2;
            client.Aisling._Dex += 4;
            client.Aisling._Luck += 3;
            client.Aisling.RaceSkill = "Appraise";
            client.Aisling.RaceSpell = null;

            var raceBool = Skill.GiveTo(client.Aisling, client.Aisling.RaceSkill, 1);
            if (raceBool) client.LoadSkillBook();

            RacialLegend(client);
        }

        public static void HighElf(GameClient client)
        {
            client.Aisling._Int += 8;
            client.Aisling._Wis += 3;
            client.Aisling._Dex += 2;

            var raceBool = Spell.GiveTo(client.Aisling, client.Aisling.RaceSpell, 1);
            if (raceBool) client.LoadSpellBook();

            RacialLegend(client);
        }

        public static void DarkElf(GameClient client)
        {
            client.Aisling._Con += 2;
            client.Aisling._Dex += 3;
            client.Aisling._Luck += 2;
            client.Aisling.RaceSkill = "Shadowfade";
            client.Aisling.RaceSpell = null;

            var raceBool = Skill.GiveTo(client.Aisling, client.Aisling.RaceSkill, 1);
            if (raceBool) client.LoadSkillBook();

            RacialLegend(client);
        }

        public static void WoodElf(GameClient client)
        {
            client.Aisling._Wis += 2;
            client.Aisling._Dex += 5;
            client.Aisling.RaceSkill = "Archery";
            client.Aisling.RaceSpell = null;

            Skill.GiveTo(client.Aisling, client.Aisling.RaceSkill, 1);
            var raceBool = Skill.GiveTo(client.Aisling, "Camouflage", 1);
            if (raceBool) client.LoadSkillBook();

            RacialLegend(client);
        }

        public static void HalfElfSkill(GameClient client, string skill)
        {
            client.Aisling._Str += 1;
            client.Aisling.StatPoints += 4;

            var raceBool = Skill.GiveTo(client.Aisling, skill, 1);
            if (raceBool) client.LoadSkillBook();

            RacialLegend(client);
        }

        public static void HalfElfSpell(GameClient client, string spell)
        {
            client.Aisling._Int += 1;
            client.Aisling.StatPoints += 4;

            var raceBool = Spell.GiveTo(client.Aisling, spell, 1);
            if (raceBool) client.LoadSpellBook();

            RacialLegend(client);
        }

        public static void Orc(GameClient client)
        {
            client.Aisling._Str += 3;
            client.Aisling._Int += 1;
            client.Aisling._Con += 3;
            client.Aisling.RaceSkill = "Pain Bane";
            client.Aisling.RaceSpell = null;

            var raceBool = Skill.GiveTo(client.Aisling, client.Aisling.RaceSkill, 1);
            if (raceBool) client.LoadSkillBook();

            RacialLegend(client);
        }

        public static void Dragonkin(GameClient client, SubClassDragonkin dragonkin)
        {
            switch (dragonkin)
            {
                case SubClassDragonkin.Red:
                    client.Aisling.FireImmunity = true;
                    client.Aisling.RaceSkill = "Fire Breath";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Blue:
                    client.Aisling.WaterImmunity = true;
                    client.Aisling.RaceSkill = "Bubble Burst";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Green:
                    client.Aisling.EarthImmunity = true;
                    client.Aisling.RaceSkill = "Earthly Delights";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Black:
                    client.Aisling.PoisonImmunity = true;
                    client.Aisling.RaceSkill = "Poison Talon";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.White:
                    client.Aisling.EnticeImmunity = true;
                    client.Aisling.RaceSkill = "Icy Blast";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Brass:
                    client.Aisling.FireImmunity = true;
                    client.Aisling.RaceSkill = "Silent Siren";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Bronze:
                    client.Aisling.WindImmunity = true;
                    client.Aisling.RaceSkill = "Toxic Breath";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Copper:
                    client.Aisling.EarthImmunity = true;
                    client.Aisling.RaceSkill = "Vicious Roar";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Gold:
                    client.Aisling.DarkImmunity = true;
                    client.Aisling.RaceSkill = "Golden Lair";
                    client.Aisling.RaceSpell = null;
                    break;
                case SubClassDragonkin.Silver:
                    client.Aisling.LightImmunity = true;
                    client.Aisling.RaceSkill = "Heavenly Gaze";
                    client.Aisling.RaceSpell = null;
                    break;
            }
            client.Aisling._Int += 2;
            client.Aisling._Regen += 5;
            var raceSkillBool = false;

            if (client.Aisling.RaceSkill != null)
                raceSkillBool = Skill.GiveTo(client.Aisling, client.Aisling.RaceSkill, 1);

            if (raceSkillBool) client.LoadSkillBook();

            DragonRacialLegend(client, ClassStrings.SubRaceDragonkinValue(dragonkin));
        }

        public static void HalfBeast(GameClient client)
        {
            client.Aisling.StatPoints += 30;
            client.Aisling.RaceSkill = null;
            client.Aisling.RaceSpell = null;
            RacialLegend(client);
        }

        public static void Merfolk(GameClient client)
        {
            client.Aisling.RaceSkill = "Splash";
            client.Aisling.RaceSpell = "Tail Flip";
            client.Aisling.WaterImmunity = true;
            client.Aisling._Str += 2;
            client.Aisling._Int += 2;
            client.Aisling._Wis += 2;
            client.Aisling._Con += 2;
            client.Aisling._Dex += 2;

            RacialLegend(client);

            var raceSkillBool = false;
            var raceSpellBool = false;

            if (client.Aisling.RaceSkill != null)
                raceSkillBool = Skill.GiveTo(client.Aisling, client.Aisling.RaceSkill, 1);
            if (client.Aisling.RaceSpell != null)
                raceSpellBool = Spell.GiveTo(client.Aisling, client.Aisling.RaceSpell, 1);

            if (raceSkillBool) client.LoadSkillBook();
            if (raceSpellBool) client.LoadSpellBook();
        }

        private static void RacialLegend(IGameClient client)
        {
            var race = ClassStrings.RaceValue(client.Aisling.Race);

            var item = new Legend.LegendItem
            {
                Category = "Race",
                Time = DateTime.Now,
                Color = LegendColor.Red,
                Icon = (byte)LegendIcon.Community,
                Value = $"Racial Origins: {race}"
            };

            client.Aisling.LegendBook.AddLegend(item, client.Aisling.Client);

            client.SendStats(StatusFlags.MultiStat);
        }

        private static void DragonRacialLegend(IGameClient client, string subRace)
        {
            var race = ClassStrings.RaceValue(client.Aisling.Race);

            var item = new Legend.LegendItem
            {
                Category = "Race",
                Time = DateTime.Now,
                Color = LegendColor.Red,
                Icon = (byte)LegendIcon.Community,
                Value = $"Racial Origins: {race} - {subRace}"
            };

            client.Aisling.LegendBook.AddLegend(item, client.Aisling.Client);

            client.SendStats(StatusFlags.MultiStat);
        }
    }
}
