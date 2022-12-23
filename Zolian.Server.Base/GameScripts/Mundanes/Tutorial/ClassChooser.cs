using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Tutorial
{
    [Script("Class Chooser")]
    public class ClassChooser : MundaneScript
    {
        private bool _skillSwitch;
        private bool _spellSwitch;

        public ClassChooser(GameServer server, Mundane mundane) : base(server, mundane) { }

        public override void OnClick(GameServer server, GameClient client)
        {
            TopMenu(client);
        }

        public override void TopMenu(IGameClient client)
        {
            if (client.Aisling.Path == Class.Peasant)
            {
                var options = new List<OptionsDataItem>
                {
                    new (0x21, "I'm ready to choose a path."),
                    new (0x22, "I'm not worthy of a path.")
                };
                client.SendOptionsDialog(Mundane,
                    "Hmm... You look weak, are you sure you're ready? Unless you've set a path for yourself, you won't survive this world. Now is the time, you must make a choice!",
                    options.ToArray());
            }
            else
            {
                client.SendOptionsDialog(Mundane, "Your path has been set. Only those who've mastered their path may look for anew.");

                Task.Delay(2000).ContinueWith(ct => { client.TransitionToMap(393, new Position(6, 4)); });
                client.SendMessage(0x02, "You don't belong here, begone!");
            }
        }

        public override async void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            if (client.Aisling.Map.ID != Mundane.Map.ID)
            {
                client.Dispose();
                return;
            }

            if (responseID is < 0x01 or > 0x06)
            {
                switch (responseID)
                {
                    case 33:
                    {
                        var options = new List<OptionsDataItem>
                        {
                            new (0x01, "Berserker"),
                            new (0x02, "Defender"),
                            new (0x03, "Assassin"),
                            new (0x04, "Cleric"),
                            new (0x05, "Arcanus"),
                            new (0x06, "Monk")
                        };

                        client.SendOptionsDialog(Mundane, "Which will you be?", options.ToArray());
                        break;
                    }
                    case 34:
                        client.SendOptionsDialog(Mundane, "Come back when you're ready to decide.");
                        break;
                }
            }
            else
            {
                var aislingEquipped = client.Aisling.EquipmentManager.Equipment;
                client.Aisling.Path = (Class)responseID;
                client.Aisling.Stage = ClassStage.Class;

                switch (client.Aisling.Path)
                {
                    case Class.Berserker:
                        {
                            Berzerker(client, aislingEquipped);
                            break;
                        }
                    case Class.Defender:
                        {
                            Defender(client, aislingEquipped);
                            break;
                        }
                    case Class.Monk:
                        {
                            Monk(client, aislingEquipped);
                            break;
                        }
                    case Class.Assassin:
                        {
                            Assassin(client, aislingEquipped);
                            break;
                        }
                    case Class.Cleric:
                        {
                            Cleric(client, aislingEquipped);
                            break;
                        }
                    case Class.Arcanus:
                        {
                            Arcanus(client, aislingEquipped);
                            break;
                        }
                    case Class.Peasant:
                        break;
                }

                var path = ClassStrings.ClassValue(client.Aisling.Path);
                var race = ClassStrings.RaceValue(client.Aisling.Race);

                if (path != "Peasant")
                {
                    ClassWrapUp(client, path);
                    await Task.Delay(350).ContinueWith(ct => { client.Aisling.Animate(303); });
                    await Task.Delay(350).ContinueWith(ct => { client.SendSound(97, Scope.AislingsOnSameMap); });
                    await Task.Delay(750).ContinueWith(ct => { client.Aisling.Animate(303); });
                }
                else
                {
                    client.SendMessage(0x03, "An error occurred, try again.");
                    TopMenu(client);
                }
            }
        }

        private async void ClassWrapUp(GameClient client, string path)
        {
            var legend = new Legend.LegendItem
            {
                Category = "Class",
                Time = DateTime.Now,
                Color = LegendColor.Blue,
                Icon = (byte)LegendIcon.Victory,
                Value = $"Devoted to the path of {path}"
            };

            client.Aisling.LegendBook.AddLegend(legend, client);

            var legendItem = new Legend.LegendItem
            {
                Category = "Alpha Aisling",
                Time = DateTime.Now,
                Color = LegendColor.Yellow,
                Icon = (byte)LegendIcon.Heart,
                Value = "Enduring Alpha Testing"
            };

            client.Aisling.LegendBook.AddLegend(legendItem, client);
            client.CloseDialog();
            client.Aisling.QuestManager.TutorialCompleted = true;
            client.Aisling.PastClass = client.Aisling.Path;

            if (_skillSwitch) client.LoadSkillBook();
            if (_spellSwitch) client.LoadSpellBook();
            client.LoadEquipment();
            client.SendStats(StatusFlags.StructA);
            client.UpdateDisplay();
            await Task.Delay(500).ContinueWith(ct => {
                client.SendMessage(0x02, "You wake from your slumber.. music begins to fill the air.");
            });
            client.TransitionToMap(137, new Position(1, 4));
        }

        private void Berzerker(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
        {
            _skillSwitch = Skill.GiveTo(client.Aisling, "Wind Slice", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Dual Slice", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Blitz", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Crasher", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Sever", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Titan's Cleave", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Retribution", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Rush", 1);

            _skillSwitch = Skill.GiveTo(client.Aisling, "Onslaught", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Clobber x2", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Wallop", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Thrust", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Asgall", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Perfect Defense", 1);

            var item = new Item();
            item = item.Create(client.Aisling,
                client.Aisling.Gender == Gender.Female
                    ? ServerSetup.Instance.GlobalItemTemplateCache["Leather Bliaut"]
                    : ServerSetup.Instance.GlobalItemTemplateCache["Leather Tunic"]);

            var item1 = new Item();
            item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Eppe"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

            var equip = new Item
            {
                Template = item.Template,
                ItemId = item.ItemId,
                Slot = 2,
                Image = item.Template.Image,
                DisplayImage = item.Template.DisplayImage,
                Durability = item.Durability,
                Owner = item.Serial,
                ItemQuality = Item.Quality.Common,
                OriginalQuality = Item.Quality.Common,
                ItemVariance = Item.Variance.None,
                WeapVariance = Item.WeaponVariance.None,
                Enchantable = item.Template.Enchantable
            };

            var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

            if (weightTest > client.Aisling.MaximumWeight)
            {
                client.SendMessage(0x03, "You can not hold anymore.");
                return;
            }

            equip.GetDisplayName();
            equip.NoColorGetDisplayName();
            client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
            item1.GiveTo(client.Aisling);
        }

        private void Defender(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
        {
            _skillSwitch = Skill.GiveTo(client.Aisling, "Wind Blade", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Beag Suain", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Charge", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Execute", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Titan's Cleave", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Beag Suain Ia Gar", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Sneak Attack", 1);

            _skillSwitch = Skill.GiveTo(client.Aisling, "Assault", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Clobber", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Wallop", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Asgall", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Perfect Defense", 1);

            var item = new Item();
            item = item.Create(client.Aisling, client.Aisling.Gender == Gender.Female
                ? ServerSetup.Instance.GlobalItemTemplateCache["Leather Donet"]
                : ServerSetup.Instance.GlobalItemTemplateCache["Leather Guard"]);
            
            var item1 = new Item();
            item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Eppe"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);
            
            var item2 = new Item();
            item2 = item2.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Wooden Shield"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

            var equip = new Item
            {
                Template = item.Template,
                ItemId = item.ItemId,
                Slot = 2,
                Image = item.Template.Image,
                DisplayImage = item.Template.DisplayImage,
                Durability = item.Durability,
                Owner = item.Serial,
                ItemQuality = Item.Quality.Common,
                OriginalQuality = Item.Quality.Common,
                ItemVariance = Item.Variance.None,
                WeapVariance = Item.WeaponVariance.None,
                Enchantable = item.Template.Enchantable
            };

            var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

            if (weightTest > client.Aisling.MaximumWeight)
            {
                client.SendMessage(0x03, "You can not hold anymore.");
                return;
            }

            equip.GetDisplayName();
            equip.NoColorGetDisplayName();
            client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
            item1.GiveTo(client.Aisling);
            item2.GiveTo(client.Aisling);
        }

        private void Assassin(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
        {
            _skillSwitch = Skill.GiveTo(client.Aisling, "Stab", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Stab Twice", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Stab'n Twist", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Sneak Attack", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Flurry", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Double-Edged Dance", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Ebb'n Flow", 1);

            _skillSwitch = Skill.GiveTo(client.Aisling, "Aim", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Throw", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Poison Tipped Trap", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Snare Trap", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Needle Trap", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Stiletto Trap", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Hiraishin", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Shunshin", 1);

            var item = new Item();
            item = item.Create(client.Aisling,
                client.Aisling.Gender == Gender.Female
                    ? ServerSetup.Instance.GlobalItemTemplateCache["Cotte"]
                    : ServerSetup.Instance.GlobalItemTemplateCache["Scout Leather"]);

            var item1 = new Item();
            item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Snow Dagger"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

            var equip = new Item
            {
                Template = item.Template,
                ItemId = item.ItemId,
                Slot = 2,
                Image = item.Template.Image,
                DisplayImage = item.Template.DisplayImage,
                Durability = item.Durability,
                Owner = item.Serial,
                ItemQuality = Item.Quality.Common,
                OriginalQuality = Item.Quality.Common,
                ItemVariance = Item.Variance.None,
                WeapVariance = Item.WeaponVariance.None,
                Enchantable = item.Template.Enchantable
            };

            var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

            if (weightTest > client.Aisling.MaximumWeight)
            {
                client.SendMessage(0x03, "You can not hold anymore.");
                return;
            }

            equip.GetDisplayName();
            equip.NoColorGetDisplayName();
            client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
            item1.GiveTo(client.Aisling);
        }

        private void Monk(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
        {
            _skillSwitch = Skill.GiveTo(client.Aisling, "Ambush", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Knife Hand Strike", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Palm Heel Strike", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Hammer Twist", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Cross Body Punch", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Wolf Fang Fist", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Hurricane Kick", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Kelberoth Strike", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Krane Kick", 1);

            _skillSwitch = Skill.GiveTo(client.Aisling, "Punch", 1);
            _skillSwitch = Skill.GiveTo(client.Aisling, "Double Punch", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Dion", 1);

            var item = new Item();
            item = item.Create(client.Aisling,
                client.Aisling.Gender == Gender.Female
                    ? ServerSetup.Instance.GlobalItemTemplateCache["Bodice"]
                    : ServerSetup.Instance.GlobalItemTemplateCache["Dobok"]);
            
            var item1 = new Item();
            item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Leather Bracer"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

            var equip = new Item
            {
                Template = item.Template,
                ItemId = item.ItemId,
                Slot = 2,
                Image = item.Template.Image,
                DisplayImage = item.Template.DisplayImage,
                Durability = item.Durability,
                Owner = item.Serial,
                ItemQuality = Item.Quality.Common,
                OriginalQuality = Item.Quality.Common,
                ItemVariance = Item.Variance.None,
                WeapVariance = Item.WeaponVariance.None,
                Enchantable = item.Template.Enchantable
            };

            var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

            if (weightTest > client.Aisling.MaximumWeight)
            {
                client.SendMessage(0x03, "You can not hold anymore.");
                return;
            }

            equip.GetDisplayName();
            equip.NoColorGetDisplayName();
            client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
            item1.GiveTo(client.Aisling);
        }

        private void Cleric(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
        {
            _spellSwitch = Spell.GiveTo(client.Aisling, "Spectral Shield", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Dark Chain", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Detect", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Healing Winds", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Forestall", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Raise Ally", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ao Puinsein", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Beag Cradh", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Cradh", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Cradh", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Cradh", 1);

            var item = new Item();
            item = item.Create(client.Aisling,
                client.Aisling.Gender == Gender.Female
                    ? ServerSetup.Instance.GlobalItemTemplateCache["Gorget Gown"]
                    : ServerSetup.Instance.GlobalItemTemplateCache["Cowl"]);

            var item1 = new Item();
            item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Stick"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

            var equip = new Item
            {
                Template = item.Template,
                ItemId = item.ItemId,
                Slot = 2,
                Image = item.Template.Image,
                DisplayImage = item.Template.DisplayImage,
                Durability = item.Durability,
                Owner = item.Serial,
                ItemQuality = Item.Quality.Common,
                OriginalQuality = Item.Quality.Common,
                ItemVariance = Item.Variance.None,
                WeapVariance = Item.WeaponVariance.None,
                Enchantable = item.Template.Enchantable
            };

            var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

            if (weightTest > client.Aisling.MaximumWeight)
            {
                client.SendMessage(0x03, "You can not hold anymore.");
                return;
            }

            equip.GetDisplayName();
            equip.NoColorGetDisplayName();
            client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
            item1.GiveTo(client.Aisling);
        }

        private void Arcanus(IGameClient client, IDictionary<int, EquipmentSlot> aislingEquipped)
        {
            _spellSwitch = Spell.GiveTo(client.Aisling, "Beag Athar", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Beag Creag", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Beag Sal", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Beag Srad", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Beag Dorcha", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Beag Eadrom", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Athar", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Creag", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Sal", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Srad", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Dorcha", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Eadrom", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Athar", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Creag", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Sal", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Srad", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Dorcha", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Eadrom", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Athar", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Creag", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Sal", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Srad", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Dorcha", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Eadrom", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Puinsein", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Puinsein", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Puinsein", 1);

            _spellSwitch = Spell.GiveTo(client.Aisling, "Fas Nadur", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Mor Fas Nadur", 1);
            _spellSwitch = Spell.GiveTo(client.Aisling, "Ard Fas Nadur", 1);

            // Vampiric Touch (Assail that takes hp)

            var item = new Item();
            item = item.Create(client.Aisling,
                client.Aisling.Gender == Gender.Female
                    ? ServerSetup.Instance.GlobalItemTemplateCache["Magi Skirt"]
                    : ServerSetup.Instance.GlobalItemTemplateCache["Gardcorp"]);

            var item1 = new Item();
            item1 = item1.Create(client.Aisling, ServerSetup.Instance.GlobalItemTemplateCache["Stick"], Item.Quality.Common, Item.Variance.None, Item.WeaponVariance.None);

            var equip = new Item
            {
                Template = item.Template,
                ItemId = item.ItemId,
                Slot = 2,
                Image = item.Template.Image,
                DisplayImage = item.Template.DisplayImage,
                Durability = item.Durability,
                Owner = item.Serial,
                ItemQuality = Item.Quality.Common,
                OriginalQuality = Item.Quality.Common,
                ItemVariance = Item.Variance.None,
                WeapVariance = Item.WeaponVariance.None,
                Enchantable = item.Template.Enchantable
            };

            var weightTest = item.Template.Weight + equip.Template.Weight + client.Aisling.CurrentWeight;

            if (weightTest > client.Aisling.MaximumWeight)
            {
                client.SendMessage(0x03, "You can not hold anymore.");
                return;
            }

            equip.GetDisplayName();
            equip.NoColorGetDisplayName();
            client.Aisling.EquipmentManager.Add(equip.Template.EquipmentSlot, equip);
            item1.GiveTo(client.Aisling);
        }
    }
}