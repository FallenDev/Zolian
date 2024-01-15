using Chaos.Common.Definitions;

using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

public static class RacialBonus
{

    public static void HumanSkill(WorldClient client, string skill)
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

    public static void HumanSpell(WorldClient client, string spell)
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

    public static void Dwarf(WorldClient client)
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

    public static void Halfling(WorldClient client)
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

    public static void HighElf(WorldClient client)
    {
        client.Aisling._Int += 8;
        client.Aisling._Wis += 3;
        client.Aisling._Dex += 2;

        var raceBool = Spell.GiveTo(client.Aisling, client.Aisling.RaceSpell, 1);
        if (raceBool) client.LoadSpellBook();

        RacialLegend(client);
    }

    public static void DarkElf(WorldClient client)
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

    public static void WoodElf(WorldClient client)
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

    public static void HalfElfSkill(WorldClient client, string skill)
    {
        client.Aisling._Str += 1;
        client.Aisling.StatPoints += 4;

        var raceBool = Skill.GiveTo(client.Aisling, skill, 1);
        if (raceBool) client.LoadSkillBook();

        RacialLegend(client);
    }

    public static void HalfElfSpell(WorldClient client, string spell)
    {
        client.Aisling._Int += 1;
        client.Aisling.StatPoints += 4;

        var raceBool = Spell.GiveTo(client.Aisling, spell, 1);
        if (raceBool) client.LoadSpellBook();

        RacialLegend(client);
    }

    public static void Orc(WorldClient client)
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

    public static void Dragonkin(WorldClient client, SubClassDragonkin dragonkin)
    {
        switch (dragonkin)
        {
            case SubClassDragonkin.Red:
                client.Aisling.BodyColor = 8;
                client.Aisling.FireImmunity = true;
                client.Aisling.RaceSkill = "Fire Breath";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Blue:
                client.Aisling.BodyColor = 7;
                client.Aisling.WaterImmunity = true;
                client.Aisling.RaceSkill = "Bubble Burst";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Green:
                client.Aisling.BodyColor = 3;
                client.Aisling.EarthImmunity = true;
                client.Aisling.RaceSkill = "Earthly Delights";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Black:
                client.Aisling.BodyColor = 9;
                client.Aisling.PoisonImmunity = true;
                client.Aisling.RaceSkill = "Poison Talon";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.White:
                client.Aisling.BodyColor = 1;
                client.Aisling.EnticeImmunity = true;
                client.Aisling.RaceSkill = "Icy Blast";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Brass:
                client.Aisling.BodyColor = 4;
                client.Aisling.FireImmunity = true;
                client.Aisling.RaceSkill = "Silent Siren";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Bronze:
                client.Aisling.BodyColor = 5;
                client.Aisling.WindImmunity = true;
                client.Aisling.RaceSkill = "Toxic Breath";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Copper:
                client.Aisling.BodyColor = 2;
                client.Aisling.EarthImmunity = true;
                client.Aisling.RaceSkill = "Vicious Roar";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Gold:
                client.Aisling.BodyColor = 4;
                client.Aisling.DarkImmunity = true;
                client.Aisling.RaceSkill = "Golden Lair";
                client.Aisling.RaceSpell = null;
                break;
            case SubClassDragonkin.Silver:
                client.Aisling.BodyColor = 1;
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

    public static void HalfBeast(WorldClient client)
    {
        client.Aisling.StatPoints += 30;
        client.Aisling.RaceSkill = null;
        client.Aisling.RaceSpell = null;
        RacialLegend(client);
    }

    public static void Merfolk(WorldClient client)
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

    private static void RacialLegend(WorldClient client)
    {
        var race = ClassStrings.RaceValue(client.Aisling.Race);

        var item = new Legend.LegendItem
        {
            Key = "Race",
            Time = DateTime.UtcNow,
            Color = LegendColor.Red,
            Icon = (byte)LegendIcon.Community,
            Text = $"Racial Origins: {race}"
        };

        client.Aisling.LegendBook.AddLegend(item, client.Aisling.Client);
        client.SendAttributes(StatUpdateType.Full);
    }

    private static void DragonRacialLegend(WorldClient client, string subRace)
    {
        var race = ClassStrings.RaceValue(client.Aisling.Race);

        var item = new Legend.LegendItem
        {
            Key = "Race",
            Time = DateTime.UtcNow,
            Color = LegendColor.Red,
            Icon = (byte)LegendIcon.Community,
            Text = $"Racial Origins: {race} - {subRace}"
        };

        client.Aisling.LegendBook.AddLegend(item, client.Aisling.Client);
        client.SendAttributes(StatUpdateType.Full);
    }
}