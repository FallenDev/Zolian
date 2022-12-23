using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.Generic
{
    [Script("Guide")]
    public class Guide : MundaneScript
    {
        public Guide(GameServer server, Mundane mundane) : base(server, mundane) { }

        public override void OnClick(GameServer server, GameClient client)
        {
            TopMenu(client);
        }

        public override void TopMenu(IGameClient client)
        {
            var options = new List<OptionsDataItem>
            {
                new(0x01, "Basics"),
                new(0x02, "Mechanics"),
                new(0x03, "Lore"),
                new(0x29, "F1 - Advanced Stats"),
                new(0x04, "Keyboard Controls")
            };

            client.SendOptionsDialog(Mundane, "This documentation is meant to be a guide for terminology and mechanics within Zolian.", options.ToArray());
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            switch (responseID)
            {
                case 1:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x09, "Character Panels"),
                            new (0x11, "Movement"),
                            new (0x12, "Settings"),
                            new (0x28, "{=q<- Back")
                        };

                        client.SendOptionsDialog(Mundane, "Zolian Help Topics", options.ToArray());
                        break;
                    }
                case 2:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x19, "Regeneration"),
                            new (0x20, "Item Quality"),
                            new (0x21, "Weapon Variances"),
                            new (0x22, "Armor Variances"),
                            new (0x24, "Armor Class"),
                            new (0x25, "Bonus Stats"),
                            new (0x28, "{=q<- Back")
                        };

                        client.SendOptionsDialog(Mundane, "Zolian Mechanics", options.ToArray());
                        break;
                    }
                case 3:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x28, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendMessage(0x09,
                            "Long ago there was a great war among aislings. The war spread far and drew the attention of the gods. Ceannlaidir, Sgrios, and Fiosachd created a plan to infuse the aislings with more power. Their plan later backfired when Chadul instead drained the lesser gods and infused their essence into the aislings, this in turn, gave them powers never seen before. Chadul had hoped to gain their trust and admiration, but eventually grew bored. Dannan who witnessed what happened, did nothing to help the lessor gods. With them weakened, the temples became empty, worship was all but lost. This caused the collapse of Loures and eventually Temuair fell into chaos.");
                        client.SendOptionsDialog(Mundane, "Listen to a tale, not so far from distant past.", options.ToArray());
                        break;
                    }
                case 4:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x28, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendMessage(0x09, $"{{=qControls\n" +
                                                 $"{{=c~ {{=a: Remove equipped main hand & off hand\n" +
                                                 $"{{=c1 thru = {{=a: Can be used to quickly access skills/spells/items\n" +
                                                 $"{{=cTab {{=a: Opens up a grid map\n" +
                                                 $"{{=ca {{=a: Opens Inventory\n" +
                                                 $"{{=cs {{=a: Opens Skills Pane\n" +
                                                 $"{{=cd {{=a: Opens Spells Pane\n" +
                                                 $"{{=cf {{=a: Opens Chat Pane\n" +
                                                 $"{{=cg {{=a: Opens Basic Stats Pane\n" +
                                                 $"{{=ch {{=a: Opens Tools Pane\n" +
                                                 $"{{=cSpacebar {{=a: Use melee skills\n" +
                                                 $"{{=cz,x,c,v {{=a: Movement\n" +
                                                 $"{{=cArrow Keys {{=a: Movement\n" +
                                                 $"{{=cShift + a {{=a: Extended Inventory\n" +
                                                 $"{{=cShift + s {{=a: Extended Skills Pane\n" +
                                                 $"{{=cShift + d {{=a: Extended Spells Pane\n" +
                                                 $"{{=cShift + f {{=a: System/Important Message Pane\n" +
                                                 $"{{=cShift + g {{=a: Extended Basic Stats Pane\n" +
                                                 $"{{=cF1 {{=a: Advanced Stats Window\n" +
                                                 $"{{=cF3 {{=a: Macro Sentences\n" +
                                                 $"{{=cF4 {{=a: System Settings\n" +
                                                 $"{{=cF5 {{=a: Client Refresh (Map Fix)\n" +
                                                 $"{{=cF8 {{=a: Client Portrait Taken\n" +
                                                 $"{{=cF9 {{=a: Add player to Ignore List\n" +
                                                 $"{{=cF10 {{=a: Add player to Friends List\n" +
                                                 $"{{=cF12 {{=a: Take ScreenShot (Stored client folder)\n" +
                                                 $"{{=cShift + ' {{=a: Direct Message player\n" +
                                                 $"{{=cGroup Messaging can be done by typing {{=a!! {{=cfor the players name.");
                        client.SendOptionsDialog(Mundane, "General keyboard commands", options.ToArray());
                        break;
                    }
                case 5:
                    client.CloseDialog();
                    break;
                case 6:
                    //client.Aisling.BetaReset = false;
                    //client.CloseDialog();
                    break;
                case 8:
                    {
                        //client.ForgetSkills();
                        //client.ForgetSpells();
                        //await Skill.GiveTo(client.Aisling, "Assail", 1);
                        //client.Aisling.Path = Class.Peasant;
                        //client.Aisling.BetaReset = true;
                        //client.CloseDialog();
                        //client.TransitionToMap(720, new Position(14, 9));
                        //client.SendMessage(0x02, "Character has been reset. Please re-login prior to changing class.");
                        break;
                    }
                case 9:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x13, "Inventory"),
                            new (0x14, "Skills"),
                            new (0x15, "Spells"),
                            new (0x16, "Conversation"),
                            new (0x17, "Stats"),
                            new (0x18, "Tools"),
                            new (0x01, "{=q<- Back")
                        };

                        client.SendOptionsDialog(Mundane, "Character Panels", options.ToArray());
                        break;
                    }
                case 16:
                    {

                        break;
                    }
                case 17:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x01, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "You may move using your mouse to point & click, arrow keys, (home, end, page up, page down), or zxcv (similar to wasd).", options.ToArray());
                        break;
                    }
                case 18:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x01, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Press F4 to view settings, an example is NPC chat. You may turn that off to reduce NPC's from filling your conversation panel.", options.ToArray());
                        break;
                    }
                case 19:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x09, "{=q<- Back"),
                            new (0x01, "{=cTop Menu"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Inventory <press a> - This pane stores all of your equipment, quest items, restoratives and monster drops. Space is limited, thus when you're running near full. Bank what you can or sell what you don't need to an NPC.", options.ToArray());
                        break;
                    }
                case 20:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x09, "{=q<- Back"),
                            new (0x01, "{=cTop Menu"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Skills <press s or press shift + s> - These panes store all of your skills, the medenia pane <shift + s> stores your more advanced skills so you may combo more effectively using a macro keyboard or software.", options.ToArray());
                        break;
                    }
                case 21:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x09, "{=q<- Back"),
                            new (0x01, "{=cTop Menu"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Spells <press d or press shift + d> - These panes store all of your spells, the medenia pane <shift + d> stores your more advanced spells saving you space and allowing you to organize your casting.", options.ToArray());
                        break;
                    }
                case 22:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x09, "{=q<- Back"),
                            new (0x01, "{=cTop Menu"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Conversations <press f or press shift + f> - This pane is where you'll see all conversations between you, others and the server. Pressing shift + f will show you more vital conversations like whispers or server alerts, while pressing just f will show you regular chat.", options.ToArray());
                        break;
                    }
                case 23:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x09, "{=q<- Back"),
                            new (0x01, "{=cTop Menu"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Stats <press g or press shift + g> - Pressing g will show you the most basic stats allowing you to also improve upon your core attributes. Pressing shift + g will allow you to see some hidden stats like dmg and hit, but to see the more advanced stats it is recommended to press F1.", options.ToArray());
                        break;
                    }
                case 24:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x09, "{=q<- Back"),
                            new (0x01, "{=cTop Menu"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Tools <press h> - This pane is where utility skills and spells go. Skills such as Assail or spells like Locate.", options.ToArray());
                        break;
                    }
                case 25:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x02, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendMessage(0x08, "{=aHP & MP have softcaps and hardcaps based on current level, con, and wis.\n" +
                                                 "{=qHP Regeneration{=a: Con divided by 3 plus your level equals a seed based on your regen modifier. This is further modified by your Base HP.\n" +
                                                 "{=qMP Regeneration{=a: Wis divided by 3 plus your level equals a seed based on your regen modifier. This is further modified by your Base MP.\n" +
                                                 "{=qRegen is calculated in real-time.");
                        client.SendOptionsDialog(Mundane, "Health & Mana Regeneration", options.ToArray());
                        break;
                    }
                case 32:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x02, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendMessage(0x08, "{=jDamaged:{=i -2 AC " +
                                                          "\n\n{=gCommon:{=i No Enhancement" +
                                                          "\n\n{=qUncommon:{=i +1 Stats, +1 DMG, 100 HP " +
                                                          "\n\n{=cRare:{=i +1 Stats, 1 AC, 2 DMG, 1 Reflex, 1 Will, 500 HP, 100 MP  " +
                                                          "\n\n{=pEpic:{=i +2 Stats, 1 AC, 2 DMG, 2 Reflex, 1 Will, 5 Regen, 750 HP, 250 MP " +
                                                          "\n\n{=sLegendary:{=i +3 Stats, 2 AC, 15 DMG, 4 Reflex, 2 Will, 10 Regen, 1000 HP, 500 MP " +
                                                          "\n\n{=bForsaken:{=i +4 Stats, 3 AC, 20 DMG, 5 Reflex, 2 Will, 20 Regen, 1500 HP, 1000 MP " +
                                                          "\n\n{=fMythic:{=i +5 Stats, 5 AC, 25 DMG, 6 Reflex, 4 Will, 40 Regen, 2500 HP, 2000 MP ");
                        client.SendOptionsDialog(Mundane, "Quality Enhancements", options.ToArray());
                        break;
                    }
                case 33:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x02, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendMessage(0x08, "{=qAegis (2% or 4%){=a: casts magic reflect (Deireas Faileas). " +
                                                 "\n\n{=qVampirism (2% or 4%){=a: absorbs health; 7% DMG Dealt with 1 point, 14% DMG Dealt with 2 points. " +
                                                 "\n\n{=qHaste (2% or 4%){=a: decrease cooldown timers for assails, abilities, and spells; 25% with 1 point, 50% with 2 points. " +
                                                 "\n\n{=qBleeding (2% or 4%){=a: bleed the target 2% health over 7 seconds. " +
                                                 "\n\n{=qRending (2% or 4%){=a: remove 10 armor for 30 seconds. " +
                                                 "\n\n{=qReaping (1%){=a: has a chance to instantly kill monsters. *Monsters Only* " +
                                                 "\n\n{=qGust | Quake | Rain | Flame | Dusk | Dawn (3% or 6%){=a: Elemental weapon procs that increase based on level.");
                        client.SendOptionsDialog(Mundane, "Powers siphoned from the Gods by Chadul", options.ToArray());
                        break;
                    }
                case 34:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x02, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendMessage(0x08, "{=qEmbunement:{=a 1 Reflex " +
                                                 "\n{=qBlessing:{=a 2 DMG " +
                                                 "\n{=qMana:{=a 250 MP " +
                                                 "\n{=qGramail:{=a 2 Will " +
                                                 "\n{=qDeoch:{=a 10 Regen, -2 AC " +
                                                 "\n{=qCeannlaidir:{=a 1 STR " +
                                                 "\n{=qCail:{=a 1 CON " +
                                                 "\n{=qFiosachd:{=a 1 DEX " +
                                                 "\n{=qGlioca:{=a 1 WIS " +
                                                 "\n{=qLuathas:{=a 1 INT " +
                                                 "\n{=qSgrios:{=a 2 STR, 1 AC, -10 Regen " +
                                                 "\n{=qReinforcement:{=a 1 AC, 250 HP " +
                                                 "\n{=qSpikes:{=a attacks reflect a % of damage back at attacker 3% per piece of gear");
                        client.SendOptionsDialog(Mundane, "Powers siphoned from the Gods by Chadul", options.ToArray());
                        break;
                    }
                case 35:
                    {

                        break;
                    }
                case 36:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x02, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "AC or Armor Class is a metric that either increases damage dealt or reduces total damage taken. (0 is no mitigation, less than 0 is damage % increased, greater than 0 is damage % mitigated)", options.ToArray());
                        break;
                    }
                case 37:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x02, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendOptionsDialog(Mundane, "Bonus stats are viewed by pressing F1. They represent attributes that are granted by spells or equipment worn.", options.ToArray());
                        break;
                    }
                case 38:
                    {

                        break;
                    }
                case 40:
                    {
                        TopMenu(client);
                        break;
                    }
                case 41:
                {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x28, "{=q<- Back"),
                            new (0x05, "{=bExit")
                        };

                        client.SendMessage(0x09,
                            "{=qClient Latency{=g: {=aAnything below 100ms is considered instant, above 250ms you may experience lag.\n" +
                            "{=qMap#{=g: {=aThis correlates to your current .map file in your installation folder.\n" +
                            "{=qStats (Str, Int, Wis, Con, Dex){=g: {=aThese are stats given from gear, these are additional to your base.\n" +
                            "{=qDMG{=g: {=aBoost to dps output after Armor is calculated.\n" +
                            "{=qRegen{=g: {=aThe strength of your mana and health regeneration.\n" +
                            "{=qArmor{=g: {=aYour armor class, mitigation is 1% per point.\n" +
                            "{=qAmplified{=g: {=aYour elemental offenses and defenses are multiplied by this value.\n" +
                            "{=qSupport Offense{=g: {=aThis is your secondary elemental for offense.\n" +
                            "{=qSupport Defense{=g: {=aThis is your secondary elemental for defense.\n" +
                            "{=qFortitude{=g: {=aIs the percentage of hits you take that are mitigated by 33%. (20% of Con)\n" +
                            "{=qReflex{=g: {=aIs the percentage of physical attacks you completely dodge and receive no damage.\n" +
                            "{=qWill{=g: {=aIs the percentage of magical attacks you completely repel and receive no damage.\n" +
                            "{=qEnhancements{=g: {=aThese are various equipment enhancements which grant you bonuses or effects.");
                        client.SendOptionsDialog(Mundane, "Scroll through the window to see details for each attribute.", options.ToArray());
                        break;
                }
            }
        }
    }
}
