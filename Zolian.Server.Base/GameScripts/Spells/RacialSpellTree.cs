using Chaos.Common.Definitions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Numerics;

namespace Darkages.GameScripts.Spells;

// Merfolk
// High-dmg Water damage type attack
[Script("Tail Flip")]
public class Tail_Flip(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Tail Flip";

        _spellMethod.ElementalOnUse(sprite, target, spell, 290);
    }
}

// Human
// Trap does moderate damage based on Int & Dex
[Script("Caltrops")]
public class Caltrops(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnActivated(Sprite sprite) { }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSelectionToggle(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnTriggeredBy(Sprite sprite, Sprite target)
    {
        var seed = Spell.Level / 100d;
        var damageImp = 15000 * seed;
        var dam = (int)(15000 + damageImp);
        target.ApplyTrapDamage(sprite, dam, Spell.Template.Sound);
        if (target.CurrentHp > 1)
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
        else
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (Spell.Template.ManaCost > sprite.CurrentMp)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Not enough mana to infuse into this trap");
            return;
        }

        sprite.CurrentMp -= Spell.Template.ManaCost;
        Trap.Set(sprite, 2270, 300, 1, OnTriggeredBy);

        if (sprite is not Aisling aisling2) return;
        aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You laid a {Spell.Template.Name}");
        _spellMethod.Train(aisling2.Client, Spell);
    }
}

// Half-Elf
// Removes Player's Threat, Target from hostile
[Script("Calming Voice")]
public class Calming_Voice(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (sprite is not Aisling aisling)
        {
            _spellMethod.SpellOnFailed(sprite, target, spell);
            return;
        }

        if (target is not Aisling targetAisling)
        {
            _spellMethod.SpellOnFailed(sprite, target, spell);
            return;
        }

        var client = aisling.Client;
        _spellMethod.Train(aisling.Client, spell);

        if (aisling.CurrentMp - spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var success = _spellMethod.Execute(client, spell);

        if (success)
        {
            foreach (var monster in targetAisling.MonstersNearby())
            {
                if (monster.Target is null) continue;
                if (monster.Target == targetAisling)
                    monster.Target = null;
            }

            targetAisling.ThreatMeter = 0;

            _spellMethod.SpellOnSuccess(sprite, target, spell);
        }
        else
        {
            _spellMethod.SpellOnFailed(aisling, target, spell);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

// Dwarf
// Immortal for a set time
[Script("Stone Skin")]
public class Stone_Skin(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_StoneSkin();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (target.Immunity)
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, spell);
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, spell, _buff);
    }
}

// High-Elf
// Pushes all monsters back 2 tiles within 1 tiles around you.
[Script("Destructive Force")]
public class DestructiveForce(Spell spell) : SpellScript(spell)
{
    private Sprite _target;
    private IEnumerable<Sprite> _enemyList;
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        damageDealingSprite.ActionUsed = "Destructive Force";
        if (target == null)
        {
            _spellMethod.SpellOnFailed(damageDealingSprite, null, spell);
            return;
        }

        if (target.CurrentHp > 0)
        {
            damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
            damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
        else
        {
            damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Position));
        }

        var mapCheck = damageDealingSprite.Map.ID;
        if (mapCheck != damageDealingSprite.Map.ID) return;

        ThrowBack();

        if (_target is Monster monster)
            Task.Delay(1500).ContinueWith(ct =>
                monster.ThrownBack = false
            );
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (sprite is not Aisling aisling)
        {
            _spellMethod.SpellOnFailed(sprite, target, spell);
            return;
        }

        var client = aisling.Client;
        _spellMethod.Train(aisling.Client, spell);

        if (aisling.CurrentMp - spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        Target(aisling);
        client.SendAttributes(StatUpdateType.Vitality);
    }

    private void Target(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        _enemyList = client.Aisling.MonstersNearby();
        var enemyList = _enemyList.ToList();

        foreach (var targetSprite in enemyList.Where(targetSprite => targetSprite is not null))
        {
            _target = targetSprite;
            if (_target.Position.DistanceFrom((ushort)damageDealingSprite.Pos.X, (ushort)damageDealingSprite.Pos.Y) >= 5) continue;
            _target = Spell.SpellReflect(_target, sprite);
            if (_target is Monster monster)
            {
                if (monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dummy)) continue;
                monster.ThrownBack = true;
            }

            var success = _spellMethod.Execute(damageDealingSprite.Client, spell);

            if (success)
            {
                OnSuccess(damageDealingSprite, _target);
            }
            else
            {
                _spellMethod.SpellOnFailed(damageDealingSprite, _target, spell);
            }
        }
    }

    private void ThrowBack()
    {
        var targetPosition = _target.GetPendingThrowPosition(2, _target);
        var hasHitOffWall = _target.GetPendingThrowIsWall(2, _target);
        var readyTime = DateTime.UtcNow;

        if (hasHitOffWall)
        {
            var stunned = new DebuffBeagsuain();
            if (_target is Aisling targetAisling)
            {
                targetAisling.Client.EnqueueDebuffAppliedEvent(_target, stunned, TimeSpan.FromSeconds(stunned.Length));
                targetAisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(208, null, _target.Serial));
            }
            else
            {
                stunned.OnApplied(_target, stunned);
                _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(208, null, _target.Serial));
            }
        }

        _target.Pos = new Vector2(targetPosition.X, targetPosition.Y);
        _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendCreatureWalk(_target.Serial, new Point(targetPosition.X, targetPosition.Y), (Direction)_target.Direction));
        _target.LastMovementChanged = readyTime;
        _target.LastPosition = new Position(targetPosition.X, targetPosition.Y);
        _target.UpdateAddAndRemove();
    }
}

// High-Elf
// Random elemental moderate damage spell
[Script("Elemental Bolt")]
public class Elemental_Bolt(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target == null) return;
        var dmg = (long)aisling.GetBaseDamage(aisling, target, MonsterEnums.Elemental);
        dmg = _spellMethod.AislingSpellDamageCalc(sprite, dmg, spell, 95);
        var randomEle = Generator.RandomEnumValue<ElementManager.Element>();

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Position));
        }

        target.ApplyElementalSpellDamage(aisling, dmg, randomEle, spell);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Elemental Bolt";

        if (!spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
            if (sprite is Aisling)
                sprite.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            if (target is Aisling)
                target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {spell.Template.Name}.");

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        _spellMethod.Train(client, spell);

        if (aisling.CurrentMp - spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (target.SpellNegate)
        {
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            if (target is Aisling)
                target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {spell.Template.Name}.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            var success = _spellMethod.Execute(client, spell);

            if (success)
            {
                OnSuccess(aisling, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(aisling, target, spell);
            }
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

// High-Elf
// Shoot three holy elemental bolts which hit random targets
[Script("Magic Missile")]
public class Magic_Missile(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        playerAction.ActionUsed = "Magic Missile";
        _spellMethod.Train(playerAction.Client, spell);

        var targetList = playerAction.MonstersNearby().ToList();
        var count = targetList.Count();

        for (var i = 0; i < 3; i++)
        {
            if (count == 0)
            {
                _spellMethod.SpellOnFailed(sprite, null, spell);
                return;
            }
            var rand = Random.Shared.Next(0, count);
            var randTarget = targetList[rand];
            _spellMethod.ElementalNecklaceOnUse(sprite, randTarget, spell, 90 + playerAction.ExpLevel);
        }
    }
}

// Halfling
// Withdraw from the bank remotely
[Script("Remote Bank")]
public class Remote_Bank(Spell spell) : SpellScript(spell)
{
    public override void OnActivated(Sprite sprite)
    {
    }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
    }

    public override void OnSelectionToggle(Sprite sprite)
    {
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
    }

    public override void OnTriggeredBy(Sprite sprite, Sprite target)
    {

    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.PlayerNearby?.Client != null)
        {
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, sprite.Serial));
        }
        else
        {
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, sprite.Serial));
        }
    }
}
