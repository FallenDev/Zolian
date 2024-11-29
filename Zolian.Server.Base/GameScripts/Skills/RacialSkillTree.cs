﻿using Chaos.Networking.Entities.Server;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Skills;

// Drow
[Script("Shadowfade")]
public class Shadowfade(Skill skill) : SkillScript(skill)
{
    private bool _success;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to fade into the shadows.");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Damageable damageDealer) return;

        if (!sprite.Alive || sprite.IsInvisible)
        {
            OnFailed(sprite);
            return;
        }

        var buff = new buff_hide();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
        damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, damageDealer.Serial);

        if (damageDealer is Aisling aisling)
            GlobalSkillMethods.Train(aisling.Client, Skill);
    }

    public override void OnCleanup() { }
}

// Wood Elf
// Aisling Str * 2, Dex * 3, * Distance (Max 8)
[Script("Archery")]
public class Archery(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private AnimationArgs _animationArgs;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, null);

    protected override void OnSuccess(Sprite sprite)
    {
        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.LongBowShot,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(9);

            if (enemy.Count == 0)
            {
                OnFailed(sprite);
                return;
            }


            foreach (var i in enemy.Where(i => sprite.Serial != i.Serial))
            {
                var dmgCalc = DamageCalc(sprite, i);

                if (sprite is Aisling aisling)
                {
                    aisling.ActionUsed = "Archery";
                    var thrown = GlobalSkillMethods.Thrown(aisling.Client, Skill, _crit);

                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)thrown,
                        SourceId = aisling.Serial,
                        TargetAnimation = (ushort)thrown,
                        TargetId = i.Serial
                    };
                }
                else
                {
                    _animationArgs = new AnimationArgs
                    {
                        AnimationSpeed = 100,
                        SourceAnimation = (ushort)(_crit ? 10002 : 10000),
                        SourceId = sprite.Serial,
                        TargetAnimation = (ushort)(_crit ? 10002 : 10000),
                        TargetId = i.Serial
                    };
                }

                GlobalSkillMethods.OnSuccessWithoutAction(i, sprite, Skill, dmgCalc, _crit);
                damageDealer.SendAnimationNearby(_animationArgs.TargetAnimation, null, _animationArgs.TargetId ?? 0, _animationArgs.AnimationSpeed, _animationArgs.SourceAnimation, _animationArgs.SourceId ?? 0);
            }

            damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Bows" or "Guns"))
            {
                OnFailed(aisling);
                return;
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
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

    private long DamageCalc(Sprite sprite, Sprite target)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3 * damageDealingAisling.Position.DistanceFrom(target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Dex * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Merfolk
// Add 50% threat 
[Script("Splash")]
public class Splash(Skill skill) : SkillScript(skill)
{
    private bool _success;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{damageDealingAisling.Username} used Splash. It's super effective!");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Splash";
            GlobalSkillMethods.Train(aisling.Client, Skill);
            aisling.ThreatMeter += (uint)(aisling.Str + aisling.Dex  * aisling.Dmg * aisling.Luck * 100);
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (sprite is not Damageable damageDealer) return;
        damageDealer.SendAnimationNearby(Skill.Template.TargetAnimation, null, damageDealer.Serial, 75);
        damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Skill.Template.Sound, false));
        damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;

            if (aisling.CurrentMp - 250 > 0)
            {
                aisling.CurrentMp -= 250;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                return;
            }

            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

// Human
// Has a chance to hit twice, Aisling Str * 8, Con * 8, Dex * 8
[Script("Slash")]
public class Slash(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Slash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront().FirstOrDefault();

            if (enemy == null || enemy.Serial == sprite.Serial)
            {
                _target = enemy;
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            GlobalSkillMethods.OnSuccess(enemy, damageDealer, Skill, dmgCalc, _crit, action);

            // Second swing if crit
            if (_crit)
                GlobalSkillMethods.OnSuccessWithoutActionAnimation(enemy, damageDealer, Skill, dmgCalc, _crit);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + Skill.Level;
            dmg = client.Aisling.Str * 8 + client.Aisling.Con * 8 + client.Aisling.Dex * 8;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 8;
            dmg += damageMonster.Con * 8;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Human
// Increases Dex by 15 momentarily
[Script("Adrenaline")]
public class Adrenaline(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're out of stamina");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Adrenaline";
            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (sprite is not Damageable damageDealer) return;

        if (damageDealer.HasBuff("Adrenaline"))
        {
            OnFailed(sprite);
            return;
        }

        var buff = new buff_DexUp();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
        damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings,
            c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnCleanup() { }
}

// Half-Elf
// Add an elemental alignment to your weapon momentarily
[Script("Atlantean Weapon")]
public class Atlantean_Weapon(Skill skill) : SkillScript(skill)
{
    private bool _success;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to enhance offense");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            aisling.ActionUsed = "Atlantean Weapon";

            if (aisling.SecondaryOffensiveElement != ElementManager.Element.None && aisling.EquipmentManager.Shield != null)
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your off-hand already grants a secondary elemental boost.");
                return;
            }

            GlobalSkillMethods.Train(aisling.Client, Skill);
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (sprite is not Damageable damageDealer) return;

        if (damageDealer.HasBuff("Atlantean Weapon"))
        {
            OnFailed(sprite);
            return;
        }

        var buff = new buff_randWeaponElement();
        GlobalSkillMethods.ApplyPhysicalBuff(damageDealer, buff);
        damageDealer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

// Half-Elf
// Adds fortitude of 33% against all elements
[Script("Elemental Bane")]
public class Elemental_Bane(Skill skill) : SkillScript(skill)
{
    private bool _success;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to increase elemental fortitude");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Elemental Bane";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.HasBuff("Elemental Bane"))
        {
            OnFailed(aisling);
            return;
        }

        var buff = new buff_ElementalBane();
        {
            GlobalSkillMethods.ApplyPhysicalBuff(aisling, buff);
        }

        GlobalSkillMethods.Train(client, Skill);

        aisling.SendAnimationNearby(360, null, aisling.Serial);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling)
        {
            var buff = new buff_ElementalBane();
            {
                GlobalSkillMethods.ApplyPhysicalBuff(sprite, buff);
            }
            return;
        }

        _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
public class Appraise(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Hmm, can't seem to identify that.");
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        client.CloseDialog();

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
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

        aisling.SendAnimationNearby(Skill.Template.TargetAnimation, null, aisling.Serial);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
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
        client.SendServerMessage(ServerMessageType.ScrollWindow, $"{{=sWeapon{{=b:{{=c {name}\n" +
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
        client.SendServerMessage(ServerMessageType.ScrollWindow, $"{{=sArmor{{=b:{{=c {name}\n" +
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
        client.SendServerMessage(ServerMessageType.ScrollWindow, $"{{=sItem{{=b:{{=c {name}\n" +
                                                                  $"{{=uLevel{{=b:{{=c {itemToBeIdentified.Template.LevelRequired}  {{=uGender{{=b:{{=c {itemToBeIdentified.Template.Gender}\n" +
                                                                  $"{{=uWeight{{=b:{{=c {itemToBeIdentified.Template.CarryWeight}  {{=uWorth{{=b:{{=c {itemToBeIdentified.Template.Value}\n" +
                                                                  $"{{=uEnchantable{{=b:{{=c {enchanted} {{=uClass{{=b:{{=c {classString}\n" +
                                                                  $"{{=uDrop Rate{{=b:{{=c {itemToBeIdentified.Template.DropRate}");
    }
}

// Red Dragonkin
// Aisling Str * 4, Con * 3, Front & Sides, Fire Dmg
[Script("Fire Breath")]
public class Fire_Breath(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Fire Breath";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
            OnFailed(aisling);
            return;
        }

        try
        {
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                if (_target is not Damageable damageable) continue;

                if (_target.SpellReflect)
                {
                    aisling.SendAnimationNearby(184, null, _target.Serial);
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You repelled {Skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    aisling.SendAnimationNearby(64, null, _target.Serial);
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You negated {Skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Fire, Skill);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, 0, false, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) continue;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You repelled {Skill.Template.Name}.");

                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You negated {Skill.Template.Name}.");

                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, Skill);
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 4 + client.Aisling.Con * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 4;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Blue Dragonkin
// Aisling Str * 3, Con * 4, Front & Sides, Water Dmg
[Script("Bubble Burst")]
public class Bubble_Burst(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Bubble Burst";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
            OnFailed(aisling);
            return;
        }

        try
        {
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                if (_target is not Damageable damageable) continue;

                if (_target.SpellReflect)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(184, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You repelled {Skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(64, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You negated {Skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Water, Skill);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, 0, false, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) continue;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You repelled {Skill.Template.Name}.");

                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You negated {Skill.Template.Name}.");

                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Water, Skill);
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 4 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 4;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// White Dragonkin
// Aisling Str * 2, Wis * 4, Front & Sides, Water Dmg, Freezes targets
[Script("Icy Blast")]
public class Icy_Blast(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private DebuffFrozen _debuff;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Icy Blast";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
            OnFailed(aisling);
            return;
        }

        try
        {
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                if (_target is not Damageable damageable) continue;

                if (_target.SpellReflect)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(184, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You repelled {Skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(64, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You negated {Skill.Template.Name}.");

                    continue;
                }

                if (_target is Aisling target2Aisling &&
                    !target2Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;

                var dmgCalc = DamageCalc(sprite);
                _debuff = new DebuffFrozen();
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, Skill);
                aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, 0, false, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) continue;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You repelled {Skill.Template.Name}.");

                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You negated {Skill.Template.Name}.");

                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, Skill);
                    _debuff = new DebuffFrozen();

                    if (_target is Aisling affected)
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff);
                    }
                    else
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            _debuff.OnApplied(_target, _debuff);
                    }

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var distance = damageDealingAisling.Position.DistanceFrom(_target.Position);
            if (distance == 0) distance = 1;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Wis * 4 / distance;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Wis * 4;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Green Dragonkin
// Aisling Str * 3, Con * 3, Front & Sides, Restores HP
[Script("Earthly Delights")]
public class Earthly_Delights(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Earthly Delights";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        try
        {
            var enemy = client.Aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                var dmgCalc = DamageCalc(sprite);

                if (_target.MaximumHp * 0.30 < dmgCalc)
                    dmgCalc = (long)(_target.MaximumHp * 0.30);

                aisling.ThreatMeter += dmgCalc;
                _target.CurrentHp += dmgCalc;

                if (_target.CurrentHp > _target.MaximumHp)
                    _target.CurrentHp = _target.MaximumHp;

                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, 0, false, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable damageable) return;
                var enemy = damageable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var entity in enemy.Where(entity => entity is not null))
                {
                    _target = entity;
                    var dmgCalc = DamageCalc(sprite);

                    if (_target.MaximumHp * 0.30 < dmgCalc)
                        dmgCalc = (long)(_target.MaximumHp * 0.30);

                    _target.CurrentHp += dmgCalc;

                    if (_target.CurrentHp > _target.MaximumHp)
                        _target.CurrentHp = _target.MaximumHp;

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) continue;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                    _crit = false;
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Silver Dragonkin
// Aisling Str * 3, Con * 3, Front & Sides, Restores HP/MP
[Script("Heavenly Gaze")]
public class Heavenly_Gaze(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Heavenly Gaze";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        try
        {
            var enemy = client.Aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                var dmgCalc = DamageCalc(sprite);
                aisling.ThreatMeter += dmgCalc;

                if (_target.MaximumHp * 0.15 < dmgCalc)
                {
                    _target.CurrentHp += (long)(_target.MaximumHp * 0.15);
                }
                else
                {
                    _target.CurrentHp += dmgCalc;
                }

                if (_target.MaximumMp * 0.20 < dmgCalc)
                {
                    _target.CurrentMp += (long)(_target.MaximumMp * 0.20);
                }
                else
                {
                    _target.CurrentMp += dmgCalc;
                }

                if (_target.CurrentHp > _target.MaximumHp)
                    _target.CurrentHp = _target.MaximumHp;

                if (_target.CurrentMp > _target.MaximumMp)
                    _target.CurrentMp = _target.MaximumMp;

                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, 0, false, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable damageable) return;
                var enemy = damageable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var entity in enemy.Where(entity => entity is not null))
                {
                    _target = entity;
                    var dmgCalc = DamageCalc(sprite);

                    if (_target.MaximumHp * 0.15 < dmgCalc)
                    {
                        _target.CurrentHp += (long)(_target.MaximumHp * 0.15);
                    }
                    else
                    {
                        _target.CurrentHp += dmgCalc;
                    }

                    if (_target.MaximumMp * 0.20 < dmgCalc)
                    {
                        _target.CurrentMp += (long)(_target.MaximumMp * 0.20);
                    }
                    else
                    {
                        _target.CurrentMp += dmgCalc;
                    }

                    if (_target.CurrentHp > _target.MaximumHp)
                        _target.CurrentHp = _target.MaximumHp;

                    if (_target.CurrentMp > _target.MaximumMp)
                        _target.CurrentMp = _target.MaximumMp;

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) continue;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                    _crit = false;
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 3 + client.Aisling.Con * 3 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 3;
            dmg += damageMonster.Con * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Brass Dragonkin
// Aisling Str * 2, Con * 2, Front & Sides, Earth Dmg, Silences targets
[Script("Silent Siren")]
public class Silent_Siren(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private DebuffSilence _debuff;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Silent Siren";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        try
        {
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                if (_target is not Damageable damageable) continue;

                if (_target.SpellReflect)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(184, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");

                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You repelled {Skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(64, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You negated {Skill.Template.Name}.");

                    continue;
                }

                if (_target is Aisling target2Aisling &&
                    !target2Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;

                var dmgCalc = DamageCalc(sprite);
                _debuff = new DebuffSilence();
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, Skill);
                aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) continue;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You repelled {Skill.Template.Name}.");

                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You negated {Skill.Template.Name}.");

                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, Skill);
                    _debuff = new DebuffSilence();

                    if (_target is Aisling affected)
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff);
                    }
                    else
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            _debuff.OnApplied(_target, _debuff);
                    }

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 2 + client.Aisling.Con * 2 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Black Dragonkin
// Aisling Str * 5, Dex * 2, Front & Sides, Earth Dmg, Poisons targets
[Script("Poison Talon")]
public class Poison_Talon(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private DebuffPoison _debuff;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Poison Talon";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        try
        {
            var enemy = aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                if (_target is not Damageable damageable) continue;

                if (_target.SpellReflect)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(184, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");

                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You repelled {Skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(64, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You negated {Skill.Template.Name}.");

                    continue;
                }

                if (_target is Aisling target2Aisling &&
                    !target2Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;

                var dmgCalc = DamageCalc(sprite);
                _debuff = new DebuffPoison();
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, Skill);
                aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) continue;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You repelled {Skill.Template.Name}.");

                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You negated {Skill.Template.Name}.");

                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, Skill);
                    _debuff = new DebuffPoison();

                    if (_target is Aisling affected)
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff);
                    }
                    else
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            _debuff.OnApplied(_target, _debuff);
                    }

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 5 + client.Aisling.Dex * 2 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Dex * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Bronze Dragonkin
// Aisling Str * 5, Con * 2, Front & Sides, Wind Dmg, Poisons targets
[Script("Toxic Breath")]
public class Toxic_Breath(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private DebuffPoison _debuff;

    protected override void OnFailed(Sprite sprite) => GlobalSkillMethods.OnFailed(sprite, Skill, _target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Toxic Breath";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.CurrentMp - 300 > 0)
        {
            aisling.CurrentMp -= 300;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        try
        {
            var enemy = client.Aisling.GetInFrontToSide();

            if (enemy.Count == 0)
            {
                GlobalSkillMethods.FailedAttemptBodyAnimation(aisling);
                OnFailed(aisling);
                GlobalSkillMethods.Train(aisling.Client, Skill);
                return;
            }

            foreach (var entity in enemy.Where(entity => entity is not null))
            {
                _target = entity;
                if (_target is not Damageable damageable) continue;

                if (_target.SpellReflect)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(184, null, _target.Serial));
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");

                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You repelled {Skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(64, null, _target.Serial));
                    client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                            $"You negated {Skill.Template.Name}.");

                    continue;
                }

                if (_target is Aisling target2Aisling &&
                    !target2Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;

                var dmgCalc = DamageCalc(sprite);
                _debuff = new DebuffPoison();
                damageable.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, Skill);
                aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff);
                GlobalSkillMethods.OnSuccess(_target, sprite, Skill, dmgCalc, _crit, action);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
            try
            {
                if (sprite is not Damageable identifiable) return;
                var enemy = identifiable.GetInFrontToSide();

                var action = new BodyAnimationArgs
                {
                    AnimationSpeed = 30,
                    BodyAnimation = BodyAnimation.Assail,
                    Sound = null,
                    SourceId = sprite.Serial
                };

                if (enemy.Count == 0) return;

                foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial))
                {
                    _target = i;
                    if (_target is not Damageable damageable) continue;

                    if (_target.SpellReflect)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(184, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You repelled {Skill.Template.Name}.");

                        _target = Spell.SpellReflect(_target, sprite);
                    }

                    if (_target.SpellNegate)
                    {
                        damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                            c => c.SendAnimation(64, null, _target.Serial));
                        if (_target is Aisling targetAisling)
                            targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1,
                                $"You negated {Skill.Template.Name}.");

                        continue;
                    }

                    var dmgCalc = DamageCalc(sprite);
                    damageable.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, Skill);
                    _debuff = new DebuffPoison();

                    if (_target is Aisling affected)
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff);
                    }
                    else
                    {
                        if (!_target.HasDebuff(_debuff.Name))
                            _debuff.OnApplied(_target, _debuff);
                    }

                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                    if (!_crit) return;
                    damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(387, null, sprite.Serial));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + Skill.Level;
            dmg = client.Aisling.Str * 5 + client.Aisling.Con * 2 / damageDealingAisling.Position.DistanceFrom(_target.Position);
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 5;
            dmg += damageMonster.Con * 2;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

// Gold Dragonkin
// Hastens Party
[Script("Golden Lair")]
public class Golden_Lair(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private buff_Hasten _buff;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've lost focus.");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    protected override void OnSuccess(Sprite sprite)
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
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        try
        {
            var party = client.Aisling.GroupParty?.PartyMembers;
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.HandsUp,
                Sound = null,
                SourceId = sprite.Serial
            };

            GlobalSkillMethods.Train(aisling.Client, Skill);
            _buff = new buff_Hasten();

            if (party == null || party.IsEmpty)
            {
                aisling.Client.EnqueueBuffAppliedEvent(aisling, _buff);
                return;
            }

            foreach (var entity in party.Values.Where(entity => entity is not null))
            {
                if (entity.Map.ID != aisling.Map.ID) continue;
                _target = entity;
                aisling.Client.EnqueueBuffAppliedEvent(_target, _buff);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(Skill.Template.TargetAnimation, null, entity.Serial, 170));
            }

            Skill.LastUsedSkill = DateTime.UtcNow;
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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
public class Vicious_Roar(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private buff_clawfist _buff;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've lost focus.");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        _buff = new buff_clawfist();

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
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        _target = aisling;
        aisling.Client.EnqueueBuffAppliedEvent(_target, _buff);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial, 170));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        Skill.LastUsedSkill = DateTime.UtcNow;
        GlobalSkillMethods.Train(client, Skill);
    }

    public override void OnCleanup()
    {
        _target = null;
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = GlobalSkillMethods.OnUse(aisling, Skill);

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

[Script("Dash")]
public class Dash(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private List<Sprite> _enemyList;

    protected override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        damageDealingAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "*Stumbled*");
        GlobalSkillMethods.OnFailed(sprite, Skill, _target);
    }

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Dash";

        if (_target == null)
        {
            OnFailed(aisling);
            return;
        }

        try
        {
            var dmgCalc = DamageCalc(aisling);
            var position = _target.Position;
            var mapCheck = aisling.Map.ID;
            var wallPosition = aisling.GetPendingChargePosition(3, aisling);
            var targetPos = GlobalSkillMethods.DistanceTo(aisling.Position, position);
            var wallPos = GlobalSkillMethods.DistanceTo(aisling.Position, wallPosition);

            if (mapCheck != aisling.Map.ID) return;

            if (targetPos <= wallPos)
            {
                switch (aisling.Direction)
                {
                    case 0:
                        position.Y++;
                        break;
                    case 1:
                        position.X--;
                        break;
                    case 2:
                        position.Y--;
                        break;
                    case 3:
                        position.X++;
                        break;
                }

                if (aisling.Position != position)
                {
                    GlobalSkillMethods.Step(aisling, position.X, position.Y);
                }

                if (_target is not Damageable damageable) return;
                damageable.ApplyDamage(aisling, dmgCalc, Skill);
                GlobalSkillMethods.Train(client, Skill);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(Skill.Template.TargetAnimation, null, _target.Serial));

                if (!_crit) return;
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(387, null, sprite.Serial));
            }
            else
            {
                GlobalSkillMethods.Step(aisling, wallPosition.X, wallPosition.Y);

                var stunned = new DebuffBeagsuain();
                aisling.Client.EnqueueDebuffAppliedEvent(aisling, stunned);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                    c => c.SendAnimation(208, null, aisling.Serial));
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnCleanup()
    {
        _target = null;
        _enemyList?.Clear();
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;

        if (client.Aisling.CantAttack)
        {
            OnFailed(aisling);
            return;
        }

        Target(aisling);
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = Skill.Level * 2;
            dmg = client.Aisling.Str * 2 + client.Aisling.Dex * 3;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Str * 2 + damageMonster.Dex * 3;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        try
        {
            _enemyList = client.Aisling.DamageableGetInFront(3);
            _target = _enemyList.FirstOrDefault();
            _target = Skill.Reflect(_target, sprite, Skill);

            if (_target == null)
            {
                var mapCheck = aisling.Map.ID;
                var wallPosition = aisling.GetPendingChargePositionNoTarget(3, aisling);
                var wallPos = GlobalSkillMethods.DistanceTo(aisling.Position, wallPosition);

                if (mapCheck != aisling.Map.ID) return;
                if (!(wallPos > 0)) OnFailed(aisling);

                if (aisling.Position != wallPosition)
                {
                    GlobalSkillMethods.Step(aisling, wallPosition.X, wallPosition.Y);
                }

                if (wallPos <= 2)
                {
                    var stunned = new DebuffBeagsuain();
                    aisling.Client.EnqueueDebuffAppliedEvent(aisling, stunned);
                    aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings,
                        c => c.SendAnimation(208, null, aisling.Serial));
                }

                aisling.UsedSkill(Skill);
            }
            else
            {
                OnSuccess(aisling);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within Target called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}