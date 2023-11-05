using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Drow
[Script("Shadowfade")]
public class Shadowfade(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to fade into the shadows.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        switch (sprite)
        {
            case Aisling aisling:
                {
                    var action = new BodyAnimationArgs
                    {
                        AnimationSpeed = 30,
                        BodyAnimation = BodyAnimation.HandsUp,
                        Sound = null,
                        SourceId = sprite.Serial
                    };

                    if (aisling.Dead || aisling.IsInvisible)
                    {
                        _skillMethod.FailedAttempt(aisling, skill, action);
                        OnFailed(aisling);
                        return;
                    }

                    var buff = new buff_hide();
                    aisling.Client.EnqueueBuffAppliedEvent(aisling, buff, TimeSpan.FromSeconds(buff.Length));
                    _skillMethod.Train(aisling.Client, skill);
                    aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, aisling.Serial));
                    break;
                }
            case Monster monster:
                {
                    // ToDo: Add logic so monsters can hide here
                    break;
                }
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
public class Archery(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.LongBowShot,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront(9);

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        aisling.ActionUsed = "Archery";

        foreach (var i in enemy.Where(i => aisling.Serial != i.Serial).Where(i => i.Attackable))
        {
            _target = i;
            var thrown = _skillMethod.Thrown(aisling.Client, skill, _crit);

            var animation = new AnimationArgs
            {
                AnimationSpeed = 100,
                SourceAnimation = (ushort)thrown,
                SourceId = aisling.Serial,
                TargetAnimation = (ushort)thrown,
                TargetId = i.Serial
            };

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccessWithoutAction(_target, aisling, skill, dmgCalc, _crit);
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
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

            _success = _skillMethod.OnUse(aisling, skill);

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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront(7).FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var animation = new AnimationArgs
            {
                AnimationSpeed = 100,
                SourceAnimation = (ushort)(_crit
                    ? 10002
                    : 10000),
                SourceId = sprite.Serial,
                TargetAnimation = (ushort)(_crit
                    ? 10002
                    : 10000),
                TargetId = _target.Serial
            };

            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(animation.TargetAnimation, null, animation.TargetId ?? 0, animation.AnimationSpeed, animation.SourceAnimation, animation.SourceId ?? 0));
            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 10 + skill.Level;
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
public class Splash(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;
        client.SendServerMessage(ServerMessageType.OrangeBar1, $"{damageDealingAisling.Username} used Splash. It's super effective!");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        aisling.ActionUsed = "Splash";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, aisling.Serial, 75));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(skill.Template.Sound, false));
        _skillMethod.Train(aisling.Client, skill);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

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

            if (aisling.CurrentMp < 0)
                aisling.CurrentMp = 0;

            _success = _skillMethod.OnUse(aisling, skill);

            if (_success)
            {
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
public class Slash(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        var enemy = aisling.DamageableGetInFront().FirstOrDefault();
        _target = enemy;

        if (_target == null || _target.Serial == aisling.Serial || !_target.Attackable)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        aisling.ActionUsed = "Slash";
        var dmgCalc = DamageCalc(sprite);
        _skillMethod.OnSuccess(_target, aisling, skill, dmgCalc, false, action);

        // Second swing if crit
        if (_crit)
            _skillMethod.OnSuccessWithoutActionAnimation(_target, aisling, skill, dmgCalc, _crit);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            var enemy = sprite.MonsterGetInFront().FirstOrDefault();
            _target = enemy;

            if (_target == null || _target.Serial == sprite.Serial || !_target.Attackable)
            {
                _skillMethod.FailedAttempt(sprite, skill, action);
                OnFailed(sprite);
                return;
            }

            var dmgCalc = DamageCalc(sprite);
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    private long DamageCalc(Sprite sprite)
    {
        _crit = false;
        long dmg;
        if (sprite is Aisling damageDealingAisling)
        {
            var client = damageDealingAisling.Client;
            var imp = 50 + skill.Level;
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
public class Adrenaline(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "You're out of stamina");
        client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, damageDealingAisling.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Adrenaline";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
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

        _skillMethod.Train(client, skill);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, skill);

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
public class Atlantean_Weapon(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to enhance offense");
        client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, damageDealingAisling.Serial));
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Atlantean Weapon";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (aisling.HasBuff("Atlantean Weapon"))
        {
            OnFailed(aisling);
            return;
        }

        if (aisling.SecondaryOffensiveElement != ElementManager.Element.None && aisling.EquipmentManager.Shield != null)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your off-hand already grants a secondary elemental boost.");
            return;
        }

        var buff = new buff_randWeaponElement();
        {
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.Train(client, skill);

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, skill);

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
public class Elemental_Bane(Skill skill) : SkillScript(skill)
{
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Failed to increase elemental fortitude");
        client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, damageDealingAisling.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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
            _skillMethod.ApplyPhysicalBuff(aisling, buff);
        }

        _skillMethod.Train(client, skill);

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(360, null, aisling.Serial));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        _success = _skillMethod.OnUse(aisling, skill);

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
    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "Hmm, can't seem to identify that.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

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

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, aisling.Serial));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUse()) return;
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
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Fire, skill);
            _skillMethod.OnSuccess(_target, sprite, skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Fire, skill);

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = 10 + skill.Level;
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
public class Bubble_Burst(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Water, skill);
            _skillMethod.OnSuccess(_target, sprite, skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Water, skill);

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = 10 + skill.Level;
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
public class Icy_Blast(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new DebuffFrozen();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, skill);
            aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff, TimeSpan.FromSeconds(_debuff.Length));
            _skillMethod.OnSuccess(_target, sprite, skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, skill);

                if (_target is Aisling affected)
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff, TimeSpan.FromSeconds(_debuff.Length));
                }
                else
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        _debuff.OnApplied(_target, _debuff);
                }

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = 10 + skill.Level;
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
public class Earthly_Delights(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;
            var dmgCalc = DamageCalc(sprite);

            if (_target.MaximumHp * 0.30 < dmgCalc)
                dmgCalc = (int)(_target.MaximumHp * 0.30);

            aisling.ThreatMeter += dmgCalc;
            _target.CurrentHp += (int)dmgCalc;

            if (_target.CurrentHp > _target.MaximumHp)
                _target.CurrentHp = _target.MaximumHp;

            _skillMethod.OnSuccess(_target, sprite, skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                    dmgCalc = (int)(_target.MaximumHp * 0.30);

                _target.CurrentHp += (int)dmgCalc;

                if (_target.CurrentHp > _target.MaximumHp)
                    _target.CurrentHp = _target.MaximumHp;

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) continue;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
                _crit = false;
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
            var imp = 10 + skill.Level;
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
public class Heavenly_Gaze(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;
            var dmgCalc = DamageCalc(sprite);
            aisling.ThreatMeter += dmgCalc;

            if (_target.MaximumHp * 0.15 < dmgCalc)
            {
                _target.CurrentHp += (int)(_target.MaximumHp * 0.15);
            }
            else
            {
                _target.CurrentHp += (int)dmgCalc;
            }

            if (_target.MaximumMp * 0.20 < dmgCalc)
            {
                _target.CurrentMp += (int)(_target.MaximumMp * 0.20);
            }
            else
            {
                _target.CurrentMp += (int)dmgCalc;
            }

            if (_target.CurrentHp > _target.MaximumHp)
                _target.CurrentHp = _target.MaximumHp;

            if (_target.CurrentMp > _target.MaximumMp)
                _target.CurrentMp = _target.MaximumMp;

            _skillMethod.OnSuccess(_target, sprite, skill, 0, false, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
                    _target.CurrentHp += (int)(_target.MaximumHp * 0.15);
                }
                else
                {
                    _target.CurrentHp += (int)dmgCalc;
                }

                if (_target.MaximumMp * 0.20 < dmgCalc)
                {
                    _target.CurrentMp += (int)(_target.MaximumMp * 0.20);
                }
                else
                {
                    _target.CurrentMp += (int)dmgCalc;
                }

                if (_target.CurrentHp > _target.MaximumHp)
                    _target.CurrentHp = _target.MaximumHp;

                if (_target.CurrentMp > _target.MaximumMp)
                    _target.CurrentMp = _target.MaximumMp;

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) continue;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
                _crit = false;
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
            var imp = 10 + skill.Level;
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
public class Silent_Siren(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new DebuffSilence();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");

                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, skill);
            aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff, TimeSpan.FromSeconds(_debuff.Length));
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, skill);

                if (_target is Aisling affected)
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff, TimeSpan.FromSeconds(_debuff.Length));
                }
                else
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        _debuff.OnApplied(_target, _debuff);
                }

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = 10 + skill.Level;
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
public class Poison_Talon(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new DebuffPoison();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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

        var enemy = aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");

                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Earth, skill);
            aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff, TimeSpan.FromSeconds(_debuff.Length));
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Earth, skill);

                if (_target is Aisling affected)
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff, TimeSpan.FromSeconds(_debuff.Length));
                }
                else
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        _debuff.OnApplied(_target, _debuff);
                }

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = 10 + skill.Level;
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
public class Toxic_Breath(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly Debuff _debuff = new DebuffPoison();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        if (sprite.NextTo(_target.Position.X, _target.Position.Y) && sprite.Facing(_target.Position.X, _target.Position.Y, out _))
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));
    }

    public override void OnSuccess(Sprite sprite)
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

        var enemy = client.Aisling.GetInFrontToSide();

        if (enemy.Count == 0)
        {
            _skillMethod.FailedAttempt(aisling, skill, action);
            OnFailed(aisling);
            return;
        }

        foreach (var entity in enemy.Where(entity => entity is not null))
        {
            _target = entity;

            if (_target.SpellReflect)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been repelled");

                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                _target = Spell.SpellReflect(_target, sprite);
            }

            if (_target.SpellNegate)
            {
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your breath has been negated");
                if (_target is Aisling targetAisling)
                    targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                continue;
            }

            var dmgCalc = DamageCalc(sprite);
            _target.ApplyElementalSkillDamage(aisling, dmgCalc, ElementManager.Element.Wind, skill);
            aisling.Client.EnqueueDebuffAppliedEvent(_target, _debuff, TimeSpan.FromSeconds(_debuff.Length));
            _skillMethod.OnSuccess(_target, sprite, skill, dmgCalc, _crit, action);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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

            var action = new BodyAnimationArgs
            {
                AnimationSpeed = 30,
                BodyAnimation = BodyAnimation.Assail,
                Sound = null,
                SourceId = sprite.Serial
            };

            if (enemy.Count == 0) return;

            foreach (var i in enemy.Where(i => i != null && sprite.Serial != i.Serial && i.Attackable))
            {
                _target = i;

                if (_target.SpellReflect)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You repelled {skill.Template.Name}.");

                    _target = Spell.SpellReflect(_target, sprite);
                }

                if (_target.SpellNegate)
                {
                    _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, _target.Serial));
                    if (_target is Aisling targetAisling)
                        targetAisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You negated {skill.Template.Name}.");

                    continue;
                }

                var dmgCalc = DamageCalc(sprite);
                _target.ApplyElementalSkillDamage(sprite, dmgCalc, ElementManager.Element.Wind, skill);

                if (_target is Aisling affected)
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        affected.Client.EnqueueDebuffAppliedEvent(affected, _debuff, TimeSpan.FromSeconds(_debuff.Length));
                }
                else
                {
                    if (!_target.HasDebuff(_debuff.Name))
                        _debuff.OnApplied(_target, _debuff);
                }

                if (skill.Template.TargetAnimation > 0)
                    if (_target is Monster or Mundane or Aisling)
                        sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));

                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

                if (!_crit) return;
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(387, null, sprite.Serial));
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
            var imp = 10 + skill.Level;
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
public class Golden_Lair(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _success;
    private readonly Buff _buff = new buff_Hasten();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You've lost focus.");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));

                    break;
                }
            case Monster:
                {
                    sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial));
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
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var party = client.Aisling.PartyMembers;
        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.HandsUp,
            Sound = null,
            SourceId = sprite.Serial
        };

        if (party == null || party.Count == 0)
        {
            aisling.Client.EnqueueBuffAppliedEvent(aisling, _buff, TimeSpan.FromSeconds(_buff.Length));
            return;
        }

        foreach (var entity in party.Where(entity => entity is not null))
        {
            if (entity.Map.ID != aisling.Map.ID) continue;
            _target = entity;
            aisling.Client.EnqueueBuffAppliedEvent(_target, _buff, TimeSpan.FromSeconds(_buff.Length));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, entity.Serial, 170));
        }

        skill.LastUsedSkill = DateTime.UtcNow;
        _skillMethod.Train(client, skill);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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
    private readonly Buff _buff = new buff_clawfist();
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (_target is not { Alive: true }) return;
        switch (sprite)
        {
            case Aisling damageDealingAisling:
                {
                    var client = damageDealingAisling.Client;

                    client.SendServerMessage(ServerMessageType.OrangeBar1, "You've lost focus.");
                    if (_target == null) return;
                    if (_target.NextTo((int)damageDealingAisling.Pos.X, (int)damageDealingAisling.Pos.Y) && damageDealingAisling.Facing((int)_target.Pos.X, (int)_target.Pos.Y, out var direction))
                        damageDealingAisling.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.MissAnimation, null, _target.Serial));

                    break;
                }
            case Monster:
                {
                    sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial));
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
        aisling.Client.EnqueueBuffAppliedEvent(_target, _buff, TimeSpan.FromSeconds(_buff.Length));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(skill.Template.TargetAnimation, null, _target.Serial, 170));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));
        skill.LastUsedSkill = DateTime.UtcNow;
        _skillMethod.Train(client, skill);
    }

    public override void OnUse(Sprite sprite)
    {
        if (!skill.CanUseZeroLineAbility) return;

        if (sprite is Aisling aisling)
        {
            _success = _skillMethod.OnUse(aisling, skill);

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