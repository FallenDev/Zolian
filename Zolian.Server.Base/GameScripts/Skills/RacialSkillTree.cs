﻿using System.Text;
using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Drow
[Script("Shadowfade")]
public class Shadowfade : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Shadowfade(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;
        client.SendMessage(0x02, "Failed to fade into the shadows.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        switch (sprite)
        {
            case Aisling aisling:
                {
                    var action = new ServerFormat1A
                    {
                        Serial = aisling.Serial,
                        Number = 0x06,
                        Speed = 30
                    };

                    if (aisling.Dead || aisling.Invisible)
                    {
                        _skillMethod.FailedAttempt(aisling, _skill, action);
                        OnFailed(aisling);
                        return;
                    }

                    var buff = new buff_hide();
                    buff.OnApplied(aisling, buff);
                    _skillMethod.Train(aisling.Client, _skill);
                    aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, aisling.Pos));
                    break;
                }
            case Monster monster:
                {
                    var client = monster.Client;
                    // ToDo: Add logic so monsters can hide here
                    break;
                }
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }
}

// Wood Elf
// Aisling Str * 2, Dex * 3, * Distance (Max 8)
[Script("Archery")]
public class Archery : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Archery(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Archery";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x8E,
            Speed = 30
        };

        var enemy = aisling.DamageableGetInFront(9);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var thrown = _skillMethod.Thrown(aisling.Client, _skill, _crit);

            var animation = new ServerFormat29
            {
                CasterSerial = (uint)aisling.Serial,
                TargetSerial = (uint)i.Serial,
                CasterEffect = (ushort)thrown,
                TargetEffect = (ushort)thrown,
                Speed = 100
            };

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, _skill, dmgCalc, _crit);
            aisling.Show(Scope.NearbyAislings, animation);
        }

        aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Bows"))
            {
                OnFailed(aisling);
                return;
            }

            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            var enemy = sprite.MonsterGetInFront(7).FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, _skill, action);
                OnFailed(sprite);
                return;
            }

            var animation = new ServerFormat29
            {
                CasterSerial = (uint)sprite.Serial,
                TargetSerial = (uint)_target.Serial,
                CasterEffect = (ushort)(_crit ? 10002 : 10000),
                TargetEffect = (ushort)(_crit ? 10002 : 10000),
                Speed = 100
            };

            sprite.Show(Scope.NearbyAislings, animation);
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3 * damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Dex * 5;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Merfolk
// Add 50% threat 
[Script("Splash")]
public class Splash : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Splash(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost focus.");
                    damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingMonster.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Splash";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, aisling.Pos, 75));
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(_skill.Template.Sound));

        _skillMethod.Train(aisling.Client, _skill);
        aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - 250 > 0)
            {
                aisling.CurrentMp -= 250;
            }
            else
            {
                client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                if (client.Aisling.Invisible && _skill.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.Invisible = false;
                    client.UpdateDisplay();
                }

                if (aisling.ThreatMeter > 0)
                    aisling.ThreatMeter += (uint)((aisling.ThreatMeter + 1) * .50);

                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }
}

// Human
// Has a chance to hit twice, Aisling Str * 3, Con * 2, Dex * 3
[Script("Slash")]
public class Slash : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Slash(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Slash";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x84,
            Speed = 20
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }
        
        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, aisling, _skill, dmgCalc, _crit, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, _skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + _skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Human
// Increases Dex by 15 momentarily
[Script("Adrenaline")]
public class Adrenaline : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Adrenaline(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "You're out of steam.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Adrenaline";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x06,
            Speed = 70
        };

        if (aisling.HasBuff("Adrenaline"))
        {
            OnFailed(aisling);
            return;
        }

        var buff = new buff_DexUp();
        {
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.Train(client, _skill);

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, _skill);

        if (_success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }
}

// Half-Elf
// Add an elemental alignment to your weapon momentarily
[Script("Atlantean Weapon")]
public class Atlantean_Weapon : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Atlantean_Weapon(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Failed to enhance offense.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Atlantean Weapon";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x06,
            Speed = 70
        };

        if (aisling.HasBuff("Atlantean Weapon"))
        {
            OnFailed(aisling);
            return;
        }

        if (aisling.SecondaryOffensiveElement != ElementManager.Element.None && aisling.EquipmentManager.Shield != null)
        {
            client.SendMessage(0x02, "Your off-hand already grants a secondary elemental boost.");
            return;
        }

        var buff = new buff_randWeaponElement();
        {
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.Train(client, _skill);

        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, _skill);

        if (_success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }
}

// Half-Elf
// Adds fortitude of 33% against all elements
[Script("Elemental Bane")]
public class Elemental_Bane : SkillScript
{
    private readonly Skill _skill;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Elemental_Bane(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendMessage(0x02, "Failed to increase elemental fortitude.");
        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, damageDealingAisling.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Elemental Bane";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x06,
            Speed = 50
        };

        if (aisling.HasBuff("Elemental Bane"))
        {
            OnFailed(aisling);
            return;
        }

        var buff = new buff_ElementalBane();
        {
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.Train(client, _skill);

        aisling.SendAnimation(360, aisling, aisling);
        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, _skill);

        if (_success)
        {
            OnSuccess(aisling);
        }
        else
        {
            OnFailed(aisling);
        }
    }
}

// Halfling
// Advanced item identification
[Script("Appraise")]
public class Appraise : SkillScript
{
    private readonly Skill _skill;

    public Appraise(Skill skill) : base(skill)
    {
        _skill = skill;
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        client.SendMessage(0x02, "Hmm, can't seem to identify that.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 50
        };

        try
        {
            var itemToBeIdentified = aisling.Inventory.Items[1];

            if (itemToBeIdentified == null)
            {
                OnFailed(aisling);
                return;
            }

            switch (itemToBeIdentified.Template.ScriptName)
            {
                case "Weapon":
                    WeaponAppraisal(aisling, itemToBeIdentified);
                    break;
                case "Armor":
                case "Helmet":
                    ArmorAppraisal(aisling, itemToBeIdentified);
                    break;
                default:
                    ItemAppraisal(aisling, itemToBeIdentified);
                    break;
            }
        }
        catch
        {
            OnFailed(aisling);
            return;
        }

        aisling.Client.Send(new ServerFormat2C(_skill.Slot, _skill.Icon, _skill.Name));
        aisling.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, aisling.Pos));
        client.Aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        OnSuccess(aisling);
    }

    private void WeaponAppraisal(Sprite sprite, Item itemToBeIdentified)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (itemToBeIdentified.Template.ScriptName != "Weapon")
        {
            OnFailed(aisling);
            return;
        }

        var mainHandOnly = !itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.DualWield) & !itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) & !itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff);
        var dualWield = itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.DualWield);
        var twoHander = itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHanded) | itemToBeIdentified.Template.Flags.FlagIsSet(ItemFlags.TwoHandedStaff);
        var answerMainHand = mainHandOnly switch
        {
            true => "Yes",
            false => "No"
        };
        var answerDualWield = dualWield switch
        {
            true => "Yes",
            false => "No"
        };
        var answerTwoHander = twoHander switch
        {
            true => "Yes",
            false => "No"
        };

        var classString = itemToBeIdentified.Template.Class != Class.Peasant ? ClassStrings.ClassValue(itemToBeIdentified.Template.Class) : "All";
        var name = itemToBeIdentified.DisplayName;
        client.SendMessage(0x08, $"{{=sWeapon{{=b:{{=c {name}\n" +
                                 $"{{=uMain hand only{{=b:{{=c {answerMainHand}\n" +
                                 $"{{=uCan Dual Wield{{=b:{{=c {answerDualWield}\n" +
                                 $"{{=uTwo-Handed{{=b:{{=c {answerTwoHander}\n" +
                                 $"{{=uDmg Min{{=b:{{=c {itemToBeIdentified.Template.DmgMin}  {{=uDmg Max{{=b:{{=c {itemToBeIdentified.Template.DmgMax}\n" +
                                 $"{{=uLevel{{=b:{{=c {itemToBeIdentified.Template.LevelRequired}  {{=uClass{{=b:{{=c {classString}\n" +
                                 "{=sStats Buff{=b:\n" +
                                 $"{{=uStr{{=b:{{=c {itemToBeIdentified.Template.StrModifer} {{=uInt{{=b:{{=c {itemToBeIdentified.Template.IntModifer} {{=uWis{{=b:{{=c {itemToBeIdentified.Template.WisModifer} {{=uCon{{=b:{{=c {itemToBeIdentified.Template.ConModifer} {{=uDex{{=b:{{=c {itemToBeIdentified.Template.DexModifer}\n" +
                                 $"{{=uHealth{{=b:{{=c {itemToBeIdentified.Template.HealthModifer} {{=uMana{{=b:{{=c {itemToBeIdentified.Template.ManaModifer}\n" +
                                 $"{{=uArmor{{=b:{{=c {itemToBeIdentified.Template.AcModifer} {{=uReflex{{=b:{{=c {itemToBeIdentified.Template.HitModifer} {{=uDamage{{=b:{{=c {itemToBeIdentified.Template.DmgModifer} {{=uRegen{{=b:{{=c {itemToBeIdentified.Template.RegenModifer}");
    }

    private void ArmorAppraisal(Sprite sprite, Item itemToBeIdentified)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (itemToBeIdentified.Template.ScriptName is not ("Armor" or "Helmet"))
        {
            OnFailed(aisling);
            return;
        }

        var classString = itemToBeIdentified.Template.Class != Class.Peasant ? ClassStrings.ClassValue(itemToBeIdentified.Template.Class) : "All";
        var name = itemToBeIdentified.DisplayName;
        client.SendMessage(0x08, $"{{=sArmor{{=b:{{=c {name}\n" +
                                 $"{{=uLevel{{=b:{{=c {itemToBeIdentified.Template.LevelRequired}  {{=uClass{{=b:{{=c {classString}\n" +
                                 "{=sStats Buff{=b:\n" +
                                 $"{{=uStr{{=b:{{=c {itemToBeIdentified.Template.StrModifer} {{=uInt{{=b:{{=c {itemToBeIdentified.Template.IntModifer} {{=uWis{{=b:{{=c {itemToBeIdentified.Template.WisModifer} {{=uCon{{=b:{{=c {itemToBeIdentified.Template.ConModifer} {{=uDex{{=b:{{=c {itemToBeIdentified.Template.DexModifer}\n" +
                                 $"{{=uHealth{{=b:{{=c {itemToBeIdentified.Template.HealthModifer} {{=uMana{{=b:{{=c {itemToBeIdentified.Template.ManaModifer}\n" +
                                 $"{{=uArmor{{=b:{{=c {itemToBeIdentified.Template.AcModifer} {{=uReflex{{=b:{{=c {itemToBeIdentified.Template.HitModifer} {{=uDamage{{=b:{{=c {itemToBeIdentified.Template.DmgModifer} {{=uRegen{{=b:{{=c {itemToBeIdentified.Template.RegenModifer}");
    }

    private static void ItemAppraisal(Sprite sprite, Item itemToBeIdentified)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        var canBeEnchanted = itemToBeIdentified.Template.Enchantable;
        var enchanted = canBeEnchanted switch
        {
            true => "Yes",
            false => "No"
        };

        var classString = itemToBeIdentified.Template.Class != Class.Peasant ? ClassStrings.ClassValue(itemToBeIdentified.Template.Class) : "All";
        var name = itemToBeIdentified.DisplayName;
        client.SendMessage(0x08, $"{{=sItem{{=b:{{=c {name}\n" +
                                 $"{{=uLevel{{=b:{{=c {itemToBeIdentified.Template.LevelRequired}  {{=uGender{{=b:{{=c {itemToBeIdentified.Template.Gender}\n" +
                                 $"{{=uWeight{{=b:{{=c {itemToBeIdentified.Template.CarryWeight}  {{=uWorth{{=b:{{=c {itemToBeIdentified.Template.Value}\n" +
                                 $"{{=uEnchantable{{=b:{{=c {enchanted} {{=uClass{{=b:{{=c {classString}\n" +
                                 $"{{=uDrop Rate{{=b:{{=c {itemToBeIdentified.Template.DropRate}");
    }
}

// Red Dragonkin
// Aisling Str * 4, Con * 3, Front & Sides, Fire Dmg
[Script("Fire Breath")]
public class Fire_Breath : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Fire_Breath(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Fire Breath";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                _target.Animate(184);
                sprite.Client.SendMessage(0x02, "Your breath has been repelled");

                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                _target.Animate(64);
                aisling.Client.SendMessage(0x02, "Your breath has been negated");
                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Fire, _skill);
            _skillMethod.OnSuccess(_target, sprite, _skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                if (_target.SpellReflect)
                {
                    _target.Animate(184);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.Animate(64);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 4 + client.Aisling.Con * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Blue Dragonkin
// Aisling Str * 3, Con * 4, Front & Sides, Water Dmg
[Script("Bubble Burst")]
public class Bubble_Burst : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Bubble_Burst(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Bubble Burst";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                _target.Animate(184);
                sprite.Client.SendMessage(0x02, "Your breath has been repelled");

                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                _target.Animate(64);
                aisling.Client.SendMessage(0x02, "Your breath has been negated");
                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Water, _skill);
            _skillMethod.OnSuccess(_target, sprite, _skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                if (_target.SpellReflect)
                {
                    _target.Animate(184);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.Animate(64);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Water, _skill);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 4 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 4;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// White Dragonkin
// Aisling Str * 2, Wis * 4, Front & Sides, Water Dmg, Freezes targets
[Script("Icy Blast")]
public class Icy_Blast : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new debuff_frozen();
    private readonly GlobalSkillMethods _skillMethod;

    public Icy_Blast(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Icy Blast";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                _target.Animate(184);
                sprite.Client.SendMessage(0x02, "Your breath has been repelled");

                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                _target.Animate(64);
                aisling.Client.SendMessage(0x02, "Your breath has been negated");
                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, _skill);
            _skillMethod.OnSuccess(_target, sprite, _skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                if (_target.SpellReflect)
                {
                    _target.Animate(184);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.Animate(64);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, _skill);
                _debuff.OnApplied(_target, _debuff);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Wis * 4 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Wis * 4;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Green Dragonkin
// Aisling Str * 3, Con * 3, Front & Sides, Restores HP
[Script("Earthly Delights")]
public class Earthly_Delights : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Earthly_Delights(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Earthly Delights";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;
            var dmgCalc = DamageCalc(sprite);

            if (_target.MaximumHp * 0.30 < dmgCalc)
                dmgCalc = (int)(_target.MaximumHp * 0.30);

            _target.CurrentHp += dmgCalc;

            if (_target.CurrentHp > _target.MaximumHp)
                _target.CurrentHp = _target.MaximumHp;

            _skillMethod.OnSuccess(_target, sprite, _skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                var dmgCalc = DamageCalc(sprite);

                if (_target.MaximumHp * 0.30 < dmgCalc)
                    dmgCalc = (int)(_target.MaximumHp * 0.30);

                _target.CurrentHp += dmgCalc;

                if (_target.CurrentHp > _target.MaximumHp)
                    _target.CurrentHp = _target.MaximumHp;

                sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));
                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) continue;
                sprite.Animate(387);
                _crit = false;
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Silver Dragonkin
// Aisling Str * 3, Con * 3, Front & Sides, Restores HP/MP
[Script("Heavenly Gaze")]
public class Heavenly_Gaze : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod;

    public Heavenly_Gaze(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Heavenly Gaze";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;
            var dmgCalc = DamageCalc(sprite);

            if (_target.MaximumHp * 0.15 < dmgCalc)
            {
                _target.CurrentHp += (int)(_target.MaximumHp * 0.15);
            }
            else
            {
                _target.CurrentHp += dmgCalc;
            }

            if (_target.MaximumMp * 0.20 < dmgCalc)
            {
                _target.CurrentMp += (int)(_target.MaximumMp * 0.20);
            }
            else
            {
                _target.CurrentMp += dmgCalc;
            }

            if (_target.CurrentHp > _target.MaximumHp)
                _target.CurrentHp = _target.MaximumHp;

            if (_target.CurrentMp > _target.MaximumMp)
                _target.CurrentMp = _target.MaximumMp;

            _skillMethod.OnSuccess(_target, sprite, _skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                var dmgCalc = DamageCalc(sprite);

                if (_target.MaximumHp * 0.15 < dmgCalc)
                {
                    _target.CurrentHp += (int)(_target.MaximumHp * 0.15);
                }
                else
                {
                    _target.CurrentHp += dmgCalc;
                }

                if (_target.MaximumMp * 0.20 < dmgCalc)
                {
                    _target.CurrentMp += (int)(_target.MaximumMp * 0.20);
                }
                else
                {
                    _target.CurrentMp += dmgCalc;
                }

                if (_target.CurrentHp > _target.MaximumHp)
                    _target.CurrentHp = _target.MaximumHp;

                if (_target.CurrentMp > _target.MaximumMp)
                    _target.CurrentMp = _target.MaximumMp;

                sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));
                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) continue;
                sprite.Animate(387);
                _crit = false;
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Brass Dragonkin
// Aisling Str * 2, Con * 2, Front & Sides, Earth Dmg, Silences targets
[Script("Silent Siren")]
public class Silent_Siren : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new debuff_Silence();
    private readonly GlobalSkillMethods _skillMethod;

    public Silent_Siren(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Silent Siren";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                _target.Animate(184);
                sprite.Client.SendMessage(0x02, "Your breath has been repelled");

                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                _target.Animate(64);
                aisling.Client.SendMessage(0x02, "Your breath has been negated");
                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, _skill);
            _debuff.OnApplied(_target, _debuff);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                if (_target.SpellReflect)
                {
                    _target.Animate(184);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.Animate(64);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, _skill);
                _debuff.OnApplied(_target, _debuff);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Con * 2 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Black Dragonkin
// Aisling Str * 5, Dex * 2, Front & Sides, Earth Dmg, Poisons targets
[Script("Poison Talon")]
public class Poison_Talon : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new debuff_Poison();
    private readonly GlobalSkillMethods _skillMethod;

    public Poison_Talon(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Poison Talon";

        var action = new ServerFormat1A
        {
            Serial = aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                _target.Animate(184);
                sprite.Client.SendMessage(0x02, "Your breath has been repelled");

                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                _target.Animate(64);
                aisling.Client.SendMessage(0x02, "Your breath has been negated");
                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, _skill);
            _debuff.OnApplied(_target, _debuff);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                if (_target.SpellReflect)
                {
                    _target.Animate(184);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.Animate(64);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, _skill);
                _debuff.OnApplied(_target, _debuff);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 5 + client.Aisling.Dex * 2 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Bronze Dragonkin
// Aisling Str * 5, Con * 2, Front & Sides, Wind Dmg, Poisons targets
[Script("Toxic Breath")]
public class Toxic_Breath : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new debuff_Poison();
    private readonly GlobalSkillMethods _skillMethod;

    public Toxic_Breath(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Toxic Breath";

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, _skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                _target.Animate(184);
                sprite.Client.SendMessage(0x02, "Your breath has been repelled");

                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                _target.Animate(64);
                client.SendMessage(0x02, "Your breath has been negated");
                if (_target is Aisling)
                    _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, _skill);
            _debuff.OnApplied(_target, _debuff);
            _skillMethod.OnSuccess(_target, sprite, _skill, dmgCalc, _crit, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            var enemy = sprite.GetInFrontToSide();

            var action = new ServerFormat1A
            {
                Serial = sprite.Serial,
                Number = 0x01,
                Speed = 30
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                if (_target.SpellReflect)
                {
                    _target.Animate(184);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You repelled {_skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.Animate(64);
                    if (_target is Aisling)
                        _target.Client.SendMessage(0x02, $"You negated {_skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, _skill);
                _debuff.OnApplied(_target, _debuff);

                if (_skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));

                sprite.Show(Scope.NearbyAislings, action);
                if (!_crit) return;
                sprite.Animate(387);
            }
        }
    }

    private int DamageCalc(Sprite sprite)
    {
        _crit = false;
        int dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + _skill.Level;
            dmg = client.Aisling.Str * 5 + client.Aisling.Con * 2 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = _skillMethod.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Gold Dragonkin
// Hastens Party
[Script("Golden Lair")]
public class Golden_Lair : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _success;
    private readonly Buff _buff = new buff_Hasten();
    private readonly GlobalSkillMethods _skillMethod;

    public Golden_Lair(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost focus.");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Golden Lair";

        if (aisling.CurrentMp - 1800 > 0)
        {
            aisling.CurrentMp -= 1800;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var party = client.Aisling.PartyMembers;
        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        if (party == null || party.Count == 0)
        {
            _buff.OnApplied(aisling, _buff);
            return;
        }

        foreach (var entity in party.Where(entity => entity is not null))
        {
            if (entity.Map.ID != aisling.Map.ID) continue;
            _target = entity;
            _buff.OnApplied(_target, _buff);
            aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));
            _skill.LastUsedSkill = DateTime.Now;
            _skillMethod.Train(client, _skill);
            aisling.Show(Scope.NearbyAislings, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }
}

// Copper Dragonkin
// Increases Attack Power moderately
[Script("Vicious Roar")]
public class Vicious_Roar : SkillScript
{
    private readonly Skill _skill;
    private Sprite _target;
    private bool _success;
    private readonly Buff _buff = new buff_clawfist();
    private readonly GlobalSkillMethods _skillMethod;

    public Vicious_Roar(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendMessage(0x02, "You've lost focus.");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
            case Monster damageDealingMonster:
                {
                    var client = damageDealingMonster.Client;

                    client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.MissAnimation, _target.Pos));

                    break;
                }
        }
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling)
        {
            _buff.OnApplied(sprite, _buff);
            return;
        }

        var client = aisling.Client;
        aisling.ActionUsed = "Vicious Roar";

        if (aisling.CurrentMp - 1800 > 0)
        {
            aisling.CurrentMp -= 1800;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var action = new ServerFormat1A
        {
            Serial = client.Aisling.Serial,
            Number = 0x91,
            Speed = 70
        };

        _target = aisling;
        _buff.OnApplied(_target, _buff);
        aisling.Show(Scope.NearbyAislings, new ServerFormat29(_skill.Template.TargetAnimation, _target.Pos, 170));
        _skill.LastUsedSkill = DateTime.Now;
        _skillMethod.Train(client, _skill);
        aisling.Show(Scope.NearbyAislings, action);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, _skill);

            if (_success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }
}