﻿using System.Numerics;
using Chaos.Common.Definitions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

// Merfolk
// High-dmg Water damage type attack
[Script("Tail Flip")]
public class Tail_Flip : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Tail_Flip(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Tail Flip";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 105);
    }
}

// Human
// Trap does moderate damage based on Int & Dex
[Script("Caltrops")]
public class Caltrops : SpellScript
{
    public Caltrops(Spell spell) : base(spell)
    {
    }

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
        target.MagicApplyDamage(sprite, 2500, Spell);
        target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Serial));
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        Trap.Set(sprite, 3000, 1, OnTriggeredBy);

        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You threw down {Spell.Template.Name}");
    }
}

// Half-Elf
// Removes Player's Threat, Target from hostile
[Script("Calming Voice")]
public class Calming_Voice : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Calming_Voice(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (sprite is not Aisling aisling)
        {
            _spellMethod.SpellOnFailed(sprite, target, _spell);
            return;
        }

        if (target is not Aisling targetAisling)
        {
            _spellMethod.SpellOnFailed(sprite, target, _spell);
            return;
        }

        var client = aisling.Client;
        _spellMethod.Train(aisling.Client, _spell);

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= _spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var success = _spellMethod.Execute(client, _spell);

        if (success)
        {
            if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                if (client.Aisling.Buffs.TryRemove("Hide", out var hide))
                {
                    hide.OnEnded(client.Aisling, hide);
                }

                if (client.Aisling.Buffs.TryRemove("Shadowfade", out var shadowFade))
                {
                    shadowFade.OnEnded(client.Aisling, shadowFade);
                }

                client.UpdateDisplay();
            }

            foreach (var monster in targetAisling.MonstersNearby())
            {
                if (monster.Target is null) continue;
                if (monster.Target == targetAisling)
                    monster.Target = null;
            }

            targetAisling.ThreatMeter = 0;

            _spellMethod.SpellOnSuccess(sprite, target, _spell);
        }
        else
        {
            _spellMethod.SpellOnFailed(aisling, target, _spell);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

// Dwarf
// Immortal for a set time
[Script("Stone Skin")]
public class Stone_Skin : SpellScript
{
    private readonly Spell _spell;
    private readonly Buff _buff = new buff_StoneSkin();
    private readonly GlobalSpellMethods _spellMethod;

    public Stone_Skin(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (target.Immunity)
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.EnhancementOnUse(sprite, target, _spell, _buff);
    }
}

// High-Elf
// Pushes all monsters back 2 tiles within 1 tiles around you.
[Script("Destructive Force")]
public class DestructiveForce : SpellScript
{
    private readonly Spell _spell;
    private Sprite _target;
    private IEnumerable<Sprite> _enemyList;
    private readonly GlobalSpellMethods _spellMethod;

    public DestructiveForce(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        damageDealingSprite.ActionUsed = "Destructive Force";
        damageDealingSprite.CastAnimation(_spell, target);
        damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendSound(_spell.Template.Sound, false));

        if (target == null)
        {
            _spellMethod.SpellOnFailed(damageDealingSprite, null, _spell);
            return;
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
            _spellMethod.SpellOnFailed(sprite, target, _spell);
            return;
        }

        var client = aisling.Client;
        _spellMethod.Train(aisling.Client, _spell);

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= _spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        if (client.Aisling.IsInvisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
        {
            if (client.Aisling.Buffs.TryRemove("Hide", out var hide))
            {
                hide.OnEnded(client.Aisling, hide);
            }

            if (client.Aisling.Buffs.TryRemove("Shadowfade", out var shadowFade))
            {
                shadowFade.OnEnded(client.Aisling, shadowFade);
            }

            client.UpdateDisplay();
        }

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
            
            var success = _spellMethod.Execute(damageDealingSprite.Client, _spell);
            
            if (success)
            {
                OnSuccess(damageDealingSprite, _target);
            }
            else
            {
                _spellMethod.SpellOnFailed(damageDealingSprite, _target, _spell);
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
            var stunned = new debuff_beagsuain();
            stunned.OnApplied(_target, stunned);
            _target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(208, _target.Serial));
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
public class Elemental_Bolt : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Elemental_Bolt(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target == null) return;
        var dmg = (long)aisling.GetBaseDamage(aisling, target, MonsterEnums.Elemental);
        dmg = _spellMethod.AislingSpellDamageCalc(sprite, dmg, _spell, 95);
        var randomEle = Generator.RandomEnumValue<ElementManager.Element>();

        aisling.CastAnimation(_spell, target);
        target.ApplyElementalSpellDamage(aisling, dmg, randomEle, _spell);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Elemental Bolt";

        if (!_spell.CanUse())
        {
            if (sprite is Aisling)
                sprite.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(184, target.Serial));
            if (sprite is Aisling)
                sprite.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            if (target is Aisling)
                target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {_spell.Template.Name}.");

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        _spellMethod.Train(client, _spell);

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= _spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (target.SpellNegate)
        {
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(64, target.Serial));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            if (target is Aisling)
                target.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {_spell.Template.Name}.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            var success = _spellMethod.Execute(client, _spell);

            if (success)
            {
                if (client.Aisling.IsInvisible &&
                    _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    if (client.Aisling.Buffs.TryRemove("Hide", out var hide))
                    {
                        hide.OnEnded(client.Aisling, hide);
                    }

                    if (client.Aisling.Buffs.TryRemove("Shadowfade", out var shadowFade))
                    {
                        shadowFade.OnEnded(client.Aisling, shadowFade);
                    }

                    client.UpdateDisplay();
                }

                OnSuccess(aisling, target);
            }
            else
            {
                _spellMethod.ElementalOnFailed(aisling, target, _spell);
            }
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }
}

// High-Elf
// Shoot three holy elemental bolts which hit random targets
[Script("Magic Missile")]
public class Magic_Missile : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Magic_Missile(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        playerAction.ActionUsed = "Magic Missile";
        _spellMethod.Train(playerAction.Client, _spell);

        var targetList = playerAction.MonstersNearby().ToList();
        var count = targetList.Count();

        for (var i = 0; i < 3; i++)
        {
            if (count == 0)
            {
                _spellMethod.SpellOnFailed(sprite, null, _spell);
                return;
            }
            var rand= Random.Shared.Next(0, count);
            var randTarget = targetList[rand];
            _spellMethod.ElementalOnUse(sprite, randTarget, _spell, 90);
        }
    }
}

