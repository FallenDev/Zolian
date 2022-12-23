using Darkages.Enums;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Tutorial
{
    [Script("Journey Start")]
    public class RaceChooser : MundaneScript
    {
        private Race _chosenRace = Race.UnDecided;
        private SubClassDragonkin _dragonkin = SubClassDragonkin.Red;
        public RaceChooser(GameServer server, Mundane mundane) : base(server, mundane) { }

        public override void OnClick(GameServer server, GameClient client)
        {
            TopMenu(client);
        }

        public override void TopMenu(IGameClient client)
        {
            if (client.Aisling.Path == Class.Peasant && client.Aisling.Race == Race.UnDecided)
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x01, $"{{=qYes"),
                    new (0x02, $"{{=bNot yet")
                };

                client.SendOptionsDialog(Mundane, "Ah, starting your journey?", options.ToArray());
            }
            else
            {
                client.SendOptionsDialog(Mundane, "What are you doing here? *raises hand towards you*");
                Task.Delay(2000).ContinueWith(ct => { client.TransitionToMap(720, new Position(14, 15)); });
                client.CloseDialog();
            }
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            if (client.Aisling.Map.ID != Mundane.Map.ID)
            {
                client.Dispose();
                return;
            }

            switch (responseID)
            {
                case 1:
                    client.Aisling.RaceSkill = null;
                    client.Aisling.RaceSpell = null;
                    client.Aisling.Race = Race.UnDecided;
                    client.SendOptionsDialog(Mundane, "{=aBefore proceeding, let's pick a race.\n" +
                                                      "Racial bonuses will be displayed on the next pane. {=qHowever be advised that once chosen, your race is {=bpermanent{=a.",
                        new OptionsDataItem(0x03, "{=qTop"),
                        new OptionsDataItem(0x04, "Human"),
                        new OptionsDataItem(0x05, "Dwarf"),
                        new OptionsDataItem(0x06, "Halfling"),
                        new OptionsDataItem(0x07, "High Elf"),
                        new OptionsDataItem(0x08, "Drow"),
                        new OptionsDataItem(0x09, "Wood Elf"),
                        new OptionsDataItem(0x0A, "Half-Elf"),
                        new OptionsDataItem(0x0B, "Orc"),
                        new OptionsDataItem(0x0C, "Dragonkin"),
                        new OptionsDataItem(0x0D, "Half-Beast"),
                        new OptionsDataItem(0x1F, "Merfolk"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 2:
                    client.Aisling.RaceSkill = null;
                    client.Aisling.RaceSpell = null;
                    client.Aisling.Race = Race.UnDecided;
                    client.CloseDialog();
                    break;
                case 3:
                    TopMenu(client);
                    break;
                case 4:
                    _chosenRace = Race.Human;
                    client.SendOptionsDialog(Mundane, "{=aHumans are known for their tenacity, creativity, and endless capacity to learn.\n" +
                                                      "Racial bonuses: {=qSTR +1, INT +1, WIS +1, CON +1, DEX +1, Luck +1, {=aPick an {=cability",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x0E, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 5:
                    _chosenRace = Race.Dwarf;
                    client.SendOptionsDialog(Mundane, "{=aDwarfs are steadfast, hardy, and brave. They value their culture, family and fine craftsmanship.\n" +
                                                      "Racial bonuses: {=qSTR + 2, CON +2, AC +2, {=aSpell: {=cStone Skin",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x10, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 6:
                    _chosenRace = Race.Halfling;
                    client.SendOptionsDialog(Mundane, "{=aHalflings are the peace keepers, highly sociable, and nimble.\n" +
                                                      "Racial bonuses: {=qINT +2, DEX +4, Luck +4, {=aSkill: {=cAppraise",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x10, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 7:
                    _chosenRace = Race.HighElf;
                    client.SendOptionsDialog(Mundane, "{=aHigh Elves respect those who know how to handle magic, they look down on those who don't.\n" +
                                                      "Racial bonuses: {=qINT +8, WIS +3, DEX +2, {=aPick a {=cspell",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x2A, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 8:
                    _chosenRace = Race.DarkElf;
                    client.SendOptionsDialog(Mundane, "{=aDark Elves were once corrupted and driven deep within the underworld. They're charismatic and cunning.\n" +
                                                      "Racial bonuses: {=qCON +2, DEX +3, Luck +2, {=aSkill: {=cShadowfade",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x10, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 9:
                    _chosenRace = Race.WoodElf;
                    client.SendOptionsDialog(Mundane, "{=aWood Elves are reclusive. They've spent their entire youth training in archery and have a natural camouflage.\n" +
                                                      "Racial bonuses: {=qWIS +2, DEX +5, {=aSkill: {=cArchery{=a, Passive: {=cCamouflage",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x10, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 0x0A:
                    _chosenRace = Race.HalfElf;
                    client.SendOptionsDialog(Mundane, "{=aA touch of elven blood remains. Half-Elves have strength from both humans and elves in their bloodlines.\n" +
                                                      "Racial bonuses: {=q+1 ability based stat, +4 Stat Points, {=aPick an {=cability",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x0F, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 0x0B:
                    _chosenRace = Race.Orc;
                    client.SendOptionsDialog(Mundane, "{=aOrcs are strong warriors who diverted from the goblins and kobolds in the deep woodlands.\n" +
                                                      "Racial bonuses: {=qSTR +3, INT +1, CON +3, {=aSkill: {=cPain Bane",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x10, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 0x0C:
                    client.SendOptionsDialog(Mundane, "{=aDragonkin, are born from various dragons and humans mixing. Their {=bDNA {=atwisted with the serpent blood renders them immune to various afflictions.\n" +
                                                      "Racial bonuses: {=qINT +2, Regen +5, {=cVarious Immunities",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x1E, "{=qDragonkin Race"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 0x0D:
                    _chosenRace = Race.HalfBeast;
                    client.SendOptionsDialog(Mundane, "{=aHalf-Beasts are a mixed race. They have blood and dna from various races and thus have various proficiencies.\n" +
                                                      "Racial bonuses: {=q+30 Stat Points",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x10, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 0x0E:
                    client.SendOptionsDialog(Mundane, $"{{=aAh {{=cHuman{{=a, which ability will you choose then?\n" +
                                                      $"{{=qSlash{{=a: An assail that sometimes hits targets twice\n" +
                                                      $"{{=qAdrenaline{{=a: Increase your dexterity momentarily\n" +
                                                      $"{{=qCaltrops{{=a: Lay down a trap that does moderate damage based on your intelligence and dexterity",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x12, "{=qSlash"),
                        new OptionsDataItem(0x13, "{=qAdrenaline"),
                        new OptionsDataItem(0x14, "{=qCaltrops"));
                    break;
                case 0x0F:
                    client.SendOptionsDialog(Mundane, $"{{=aAh {{=cHalf-Elf{{=a, which ability will you choose then?\n" +
                                                      $"{{=qCalming Voice{{=a: Removes target's threat. Makes monsters less likely to attack you\n" +
                                                      $"{{=qAtlantean Weapon{{=a: Embune your weapon with a random element\n" +
                                                      $"{{=qElement Bane{{=a: Reduces damage by 33%",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x15, "{=qCalming Voice"),
                        new OptionsDataItem(0x16, "{=qAtlantean Weapon"),
                        new OptionsDataItem(0x17, "{=qElement Bane"));
                    break;
                case 0x10:
                    var playerChosenRace = ClassStrings.RaceValue(_chosenRace);
                    client.SendOptionsDialog(Mundane, $"{{=aVery well {{=q{playerChosenRace}{{=a, do you wish to be teleported to the Path of Souls now?\n" +
                                                      $"{{=aThere you will pick a class.",
                        new OptionsDataItem(0x11, "{=qI accept"),
                        new OptionsDataItem(0x01, "{=cI've changed my mind"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 0x11:
                    {
                        client.Aisling.Race = _chosenRace;

                        if (client.Aisling.Race != Race.UnDecided)
                        {
                            switch (client.Aisling.Race)
                            {
                                case Race.Human:
                                    if (client.Aisling.RaceSkill is not null)
                                        RacialBonus.HumanSkill(client, client.Aisling.RaceSkill);
                                    if (client.Aisling.RaceSpell is not null)
                                        RacialBonus.HumanSpell(client, client.Aisling.RaceSpell);
                                    break;
                                case Race.HalfElf:
                                    if (client.Aisling.RaceSkill is not null)
                                        RacialBonus.HalfElfSkill(client, client.Aisling.RaceSkill);
                                    if (client.Aisling.RaceSpell is not null)
                                        RacialBonus.HalfElfSpell(client, client.Aisling.RaceSpell);
                                    break;
                                case Race.HighElf:
                                    RacialBonus.HighElf(client);
                                    break;
                                case Race.DarkElf:
                                    RacialBonus.DarkElf(client);
                                    break;
                                case Race.WoodElf:
                                    RacialBonus.WoodElf(client);
                                    break;
                                case Race.Orc:
                                    RacialBonus.Orc(client);
                                    break;
                                case Race.Dwarf:
                                    RacialBonus.Dwarf(client);
                                    break;
                                case Race.Halfling:
                                    RacialBonus.Halfling(client);
                                    break;
                                case Race.Dragonkin:
                                    RacialBonus.Dragonkin(client, _dragonkin);
                                    break;
                                case Race.HalfBeast:
                                    RacialBonus.HalfBeast(client);
                                    break;
                                case Race.Merfolk:
                                    RacialBonus.Merfolk(client);
                                    break;
                            }
                        }

                        client.SendOptionsDialog(Mundane, "*raises a hand towards you*");
                        Task.Delay(1000).ContinueWith(ct => { client.TransitionToMap(720, new Position(14, 15)); });
                        Task.Delay(1300).ContinueWith(ct => { client.Aisling.Animate(303); });
                        Task.Delay(1300).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                        Task.Delay(1500).ContinueWith(ct => { client.Aisling.Animate(303); });
                        client.CloseDialog();
                        client.SendMessage(0x03, "Was that a dream?");
                        Task.Delay(2000).ContinueWith(ct => { client.SendMessage(0x03, $"{{=cYou start to feel newly found powers surge within you"); });
                    }
                    return;
                case 0x12:
                    client.Aisling.RaceSkill = "Slash";
                    client.Aisling.RaceSpell = null;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x13:
                    client.Aisling.RaceSkill = "Adrenaline";
                    client.Aisling.RaceSpell = null;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x14:
                    client.Aisling.RaceSkill = null;
                    client.Aisling.RaceSpell = "Caltrops";
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x15:
                    client.Aisling.RaceSkill = null;
                    client.Aisling.RaceSpell = "Calming Voice";
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x16:
                    client.Aisling.RaceSkill = "Atlantean Weapon";
                    client.Aisling.RaceSpell = null;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x17:
                    client.Aisling.RaceSkill = "Elemental Bane";
                    client.Aisling.RaceSpell = null;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x1E:
                    _chosenRace = Race.Dragonkin;
                    client.SendOptionsDialog(Mundane, $"{{=aScroll between the various abilities in the popup and chose one that matches the type of dragonkin blood that flows through you.",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x20, "{=bRed"),
                        new OptionsDataItem(0x21, "{=fBlue"),
                        new OptionsDataItem(0x22, "{=dGreen"),
                        new OptionsDataItem(0x23, "{=kBlack"),
                        new OptionsDataItem(0x24, "{=uWhite"),
                        new OptionsDataItem(0x25, "{=sBrass"),
                        new OptionsDataItem(0x26, "{=sBronze"),
                        new OptionsDataItem(0x27, "{=sCopper"),
                        new OptionsDataItem(0x28, "{=cGold"),
                        new OptionsDataItem(0x29, "{=gSilver"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    client.SendMessage(0x09,
                        $"{{=aScroll between the various abilities below and chose one that matches the type of dragonkin blood that flows through you.\n" +
                        $"{{=bRed{{=a: Fire Breath, and immunity to fire dmg.\n" + 
                        $"{{=fBlue{{=a: Bubble Burst, and immunity to water dmg.\n" + 
                        $"{{=dGreen{{=a: Earthly Delights, and immunity to earth dmg.\n" + 
                        $"{{=mBlack{{=a: Poison Talon, and immunity to poison.\n" + 
                        $"{{=uWhite{{=a: Icy Blast, and immunity to entice.\n" + 
                        $"{{=sBrass{{=a: Silent Siren, and immunity to fire dmg.\n" + 
                        $"{{=sBronze{{=a: Toxic Breath, and immunity to wind dmg.\n" +
                        $"{{=sCopper{{=a: Vicious Roar, and immunity to earth dmg.\n" +
                        $"{{=cGold{{=a: Golden Lair, and immunity to void dmg.\n" +
                        $"{{=gSilver{{=a: Heavenly Gaze, and immunity to holy dmg.\n");
                    break;
                case 0x1F:
                    _chosenRace = Race.Merfolk;
                    client.SendOptionsDialog(Mundane, $"{{=aMerfolk, legendary race of the sea. They have a natural defense to water-based spells.\n" +
                                                      $"Racial: {{=q+2 all stats, Splash, Tail Flip, Water Immunity",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x10, "{=qLet's go"),
                        new OptionsDataItem(0x02, "{=bEnd"));
                    break;
                case 0x20:
                    _dragonkin = SubClassDragonkin.Red;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x21:
                    _dragonkin = SubClassDragonkin.Blue;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x22:
                    _dragonkin = SubClassDragonkin.Green;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x23:
                    _dragonkin = SubClassDragonkin.Black;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x24:
                    _dragonkin = SubClassDragonkin.White;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x25:
                    _dragonkin = SubClassDragonkin.Brass;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x26:
                    _dragonkin = SubClassDragonkin.Bronze;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x27:
                    _dragonkin = SubClassDragonkin.Copper;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x28:
                    _dragonkin = SubClassDragonkin.Gold;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x29:
                    _dragonkin = SubClassDragonkin.Silver;
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x2A:
                    client.SendOptionsDialog(Mundane, $"{{=aAh {{=cHigh-Elf{{=a, wielder of magic, which spell will you choose then?\n" +
                                                      $"{{=qDestructive Force{{=a: Push all monsters within 5 tiles, 2 tiles away from you\n" +
                                                      $"{{=qElemental Bolt{{=a: Send fourth a random elemental attack\n" +
                                                      $"{{=qMagic Missile{{=a: Sends fourth 3 magical bolts to random enemies",
                        new OptionsDataItem(0x01, "{=cBack"),
                        new OptionsDataItem(0x2B, "{=qDestructive Force"),
                        new OptionsDataItem(0x2C, "{=qElemental Bolt"),
                        new OptionsDataItem(0x2D, "{=qMagic Missile"));
                    break;
                case 0x2B:
                    client.Aisling.RaceSkill = null;
                    client.Aisling.RaceSpell = "Destructive Force";
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x2C:
                    client.Aisling.RaceSkill = null;
                    client.Aisling.RaceSpell = "Elemental Bolt";
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
                case 0x2D:
                    client.Aisling.RaceSkill = null;
                    client.Aisling.RaceSpell = "Magic Missile";
                    OnResponse(server, client, 0x10, string.Empty);
                    break;
            }
        }
    }
}
