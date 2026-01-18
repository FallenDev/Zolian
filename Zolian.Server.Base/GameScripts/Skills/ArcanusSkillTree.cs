using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Spells;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

[Script("Flame Thrower")]
public class Flame_Thrower(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private int _strength;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Flame Thrower";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            _ = SendAnimations(damageDealer, enemy);
            Task.Delay(200).Wait();
            var target = enemy.FirstOrDefault();

            if (target is null || target.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            if (target is not Damageable damageable) return;

            if (target.SpellReflect)
            {
                damageDealer.SendAnimationNearby(184, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been reflected!");
                target = Spell.SpellReflect(target, damageDealer);
            }

            if (target.SpellNegate)
            {
                damageDealer.SendAnimationNearby(64, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(damageDealer, target);
            dmgCalc += GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, _strength);
            damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Fire, Skill);
            GlobalSkillMethods.OnSuccess(target, damageDealer, Skill, 0, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            var cost = ManaCostOnStrength(aisling);

            if (sprite.CurrentMp - cost > 0)
            {
                sprite.CurrentMp -= cost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                OnFailed(sprite, null);
                return;
            }

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands" or "Staves" or "Swords"))
            {
                if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not "Sources")
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "I'm unable to channel with this equipment!");
                    OnFailed(aisling, null);
                    return;
                }
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            if (sprite.CurrentMp - 500 > 0)
            {
                sprite.CurrentMp -= 500;
            }

            OnSuccess(sprite);
        }
    }

    private int ManaCostOnStrength(Aisling aisling)
    {
        int manacost;

        if (aisling.SpellBook.HasSpell("Uas Srad"))
        {
            manacost = 2500;
            _strength = 8;
            return manacost;
        }
        
        if (aisling.SpellBook.HasSpell("Ard Srad"))
        {
            manacost = 1000;
            _strength = 5;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Mor Srad"))
        {
            manacost = 750;
            _strength = 3;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Srad"))
        {
            manacost = 500;
            _strength = 2;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Beag Srad"))
        {
            manacost = 250;
            _strength = 1;
            return manacost;
        }

        _strength = 1;
        return 500;
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendAnimationNearby(147, vector);
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Water Cannon")]
public class Water_Cannon(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private int _strength;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Water Cannon";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            _ = SendAnimations(damageDealer, enemy);
            Task.Delay(200).Wait();
            var target = enemy.FirstOrDefault();

            if (target is null || target.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            if (target is not Damageable damageable) return;

            if (target.SpellReflect)
            {
                damageDealer.SendAnimationNearby(184, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been reflected!");
                target = Spell.SpellReflect(target, damageDealer);
            }

            if (target.SpellNegate)
            {
                damageDealer.SendAnimationNearby(64, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(damageDealer, target);
            dmgCalc += GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, _strength);
            damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Water, Skill);
            GlobalSkillMethods.OnSuccess(target, damageDealer, Skill, 0, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            var cost = ManaCostOnStrength(aisling);

            if (sprite.CurrentMp - cost > 0)
            {
                sprite.CurrentMp -= cost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                OnFailed(sprite, null);
                return;
            }

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands" or "Staves" or "Swords"))
            {
                if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not "Sources")
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "I'm unable to channel with this equipment!");
                    OnFailed(aisling, null);
                    return;
                }
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            if (sprite.CurrentMp - 500 > 0)
            {
                sprite.CurrentMp -= 500;
            }

            OnSuccess(sprite);
        }
    }

    private int ManaCostOnStrength(Aisling aisling)
    {
        int manacost;

        if (aisling.SpellBook.HasSpell("Uas Sal"))
        {
            manacost = 2500;
            _strength = 8;
            return manacost;
        }
        
        if (aisling.SpellBook.HasSpell("Ard Sal"))
        {
            manacost = 1000;
            _strength = 5;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Mor Sal"))
        {
            manacost = 750;
            _strength = 3;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Sal"))
        {
            manacost = 500;
            _strength = 2;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Beag Sal"))
        {
            manacost = 250;
            _strength = 1;
            return manacost;
        }

        _strength = 1;
        return 500;
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendAnimationNearby(150, vector);
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Tornado Vector")]
public class Tornado_Vector(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private int _strength;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Tornado Vector";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            _ = SendAnimations(damageDealer, enemy);
            Task.Delay(200).Wait();
            var target = enemy.FirstOrDefault();

            if (target is null || target.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            if (target is not Damageable damageable) return;

            if (target.SpellReflect)
            {
                damageDealer.SendAnimationNearby(184, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been reflected!");
                target = Spell.SpellReflect(target, damageDealer);
            }

            if (target.SpellNegate)
            {
                damageDealer.SendAnimationNearby(64, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(damageDealer, target);
            dmgCalc += GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, _strength);
            damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Wind, Skill);
            GlobalSkillMethods.OnSuccess(target, damageDealer, Skill, 0, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            var cost = ManaCostOnStrength(aisling);

            if (sprite.CurrentMp - cost > 0)
            {
                sprite.CurrentMp -= cost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                OnFailed(sprite, null);
                return;
            }

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands" or "Staves" or "Swords"))
            {
                if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not "Sources")
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "I'm unable to channel with this equipment!");
                    OnFailed(aisling, null);
                    return;
                }
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            if (sprite.CurrentMp - 500 > 0)
            {
                sprite.CurrentMp -= 500;
            }

            OnSuccess(sprite);
        }
    }

    private int ManaCostOnStrength(Aisling aisling)
    {
        int manacost;

        if (aisling.SpellBook.HasSpell("Uas Athar"))
        {
            manacost = 2500;
            _strength = 8;
            return manacost;
        }
        
        if (aisling.SpellBook.HasSpell("Ard Athar"))
        {
            manacost = 1000;
            _strength = 5;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Mor Athar"))
        {
            manacost = 750;
            _strength = 3;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Athar"))
        {
            manacost = 500;
            _strength = 2;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Beag Athar"))
        {
            manacost = 250;
            _strength = 1;
            return manacost;
        }

        _strength = 1;
        return 500;
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendAnimationNearby(197, vector);
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}

[Script("Earth Shatter")]
public class Earth_Shatter(Skill skill) : SkillScript(skill)
{
    private bool _crit;
    private int _strength;

    protected override void OnFailed(Sprite sprite, Sprite target) => GlobalSkillMethods.OnFailed(sprite, Skill, target);

    protected override async void OnSuccess(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.ActionUsed = "Earth Shatter";

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 40,
            BodyAnimation = (BodyAnimation)0x06,
            Sound = null,
            SourceId = sprite.Serial
        };

        try
        {
            if (sprite is not Damageable damageDealer) return;
            var enemy = damageDealer.DamageableGetInFront(6);

            if (enemy.Count == 0)
            {
                OnFailed(sprite, null);
                return;
            }

            _ = SendAnimations(damageDealer, enemy);
            Task.Delay(200).Wait();
            var target = enemy.FirstOrDefault();

            if (target is null || target.Serial == sprite.Serial)
            {
                OnFailed(sprite, null);
                return;
            }

            if (target is not Damageable damageable) return;

            if (target.SpellReflect)
            {
                damageDealer.SendAnimationNearby(184, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been reflected!");
                target = Spell.SpellReflect(target, damageDealer);
            }

            if (target.SpellNegate)
            {
                damageDealer.SendAnimationNearby(64, null, target.Serial);
                if (sprite is Aisling aislingReflect)
                    aislingReflect.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your elemental ability has been deflected!");
                return;
            }

            var dmgCalc = DamageCalc(damageDealer, target);
            dmgCalc += GlobalSpellMethods.WeaponDamageElementalProc(damageDealer, _strength);
            damageable.ApplyElementalSkillDamage(damageDealer, dmgCalc, ElementManager.Element.Earth, Skill);
            GlobalSkillMethods.OnSuccess(target, damageDealer, Skill, 0, _crit, action);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {Skill.Name} within OnSuccess called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    public override void OnUse(Sprite sprite)
    {
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            var cost = ManaCostOnStrength(aisling);

            if (sprite.CurrentMp - cost > 0)
            {
                sprite.CurrentMp -= cost;
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
                OnFailed(sprite, null);
                return;
            }

            if (client.Aisling.EquipmentManager.Equipment[1]?.Item?.Template.Group is not ("Wands" or "Staves" or "Swords"))
            {
                if (client.Aisling.EquipmentManager.Equipment[3]?.Item?.Template.Group is not "Sources")
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "I'm unable to channel with this equipment!");
                    OnFailed(aisling, null);
                    return;
                }
            }

            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling, null);
            }
        }
        else
        {
            if (sprite.CurrentMp - 500 > 0)
            {
                sprite.CurrentMp -= 500;
            }

            OnSuccess(sprite);
        }
    }

    private int ManaCostOnStrength(Aisling aisling)
    {
        int manacost;

        if (aisling.SpellBook.HasSpell("Uas Creag"))
        {
            manacost = 2500;
            _strength = 8;
            return manacost;
        }
        
        if (aisling.SpellBook.HasSpell("Ard Creag"))
        {
            manacost = 1000;
            _strength = 5;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Mor Creag"))
        {
            manacost = 750;
            _strength = 3;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Creag"))
        {
            manacost = 500;
            _strength = 2;
            return manacost;
        }

        if (aisling.SpellBook.HasSpell("Beag Creag"))
        {
            manacost = 250;
            _strength = 1;
            return manacost;
        }

        _strength = 1;
        return 500;
    }

    private static async Task SendAnimations(Sprite damageDealingSprite, IEnumerable<Sprite> enemy)
    {
        if (damageDealingSprite is not Damageable damageable) return;

        foreach (var position in damageable.GetTilesInFront(damageDealingSprite.Position.DistanceFrom(enemy.FirstOrDefault()?.Position)))
        {
            var vector = new Position(position.X, position.Y);
            await Task.Delay(200).ContinueWith(ct =>
            {
                damageable.SendAnimationNearby(60, vector);
            });
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
            var dist = damageDealingAisling.Position.DistanceFrom(target.Position) == 0 ? 1 : damageDealingAisling.Position.DistanceFrom(target.Position);
            dmg = client.Aisling.Int * 3 + client.Aisling.Wis * 3 / dist;
            dmg += dmg * imp / 100;
        }
        else
        {
            if (sprite is not Monster damageMonster) return 0;
            dmg = damageMonster.Int * 3;
            dmg += damageMonster.Wis * 5;
        }

        var critCheck = GlobalSkillMethods.OnCrit(dmg);
        _crit = critCheck.Item1;
        return critCheck.Item2;
    }
}