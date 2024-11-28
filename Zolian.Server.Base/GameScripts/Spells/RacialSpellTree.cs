﻿using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
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

        _spellMethod.ElementalOnUse(sprite, target, Spell, 290);
    }
}

// Human
[Script("Caltrops")]
public class Caltrops(Spell spell) : SpellScript(spell)
{
    public override void OnActivated(Sprite sprite) { }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSelectionToggle(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnTriggeredBy(Sprite sprite, Sprite target)
    {
        var seed = Spell.Level / 100d;
        var damageImp = 15000 * seed;
        var dam = (int)(15000 + damageImp);
        if (target is not Damageable damageable) return;
        damageable.ApplyTrapDamage(sprite, dam, Spell.Template.Sound);

        if (target.CurrentHp > 1)
            damageable.SendAnimationNearby(Spell.Template.TargetAnimation, null, target.Serial);
        else
            damageable.SendAnimationNearby(Spell.Template.TargetAnimation, target.Position);
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
        GlobalSpellMethods.Train(aisling2.Client, Spell);
    }
}

// Half-Elf
// Removes Player's Threat, Target from hostile
[Script("Calming Voice")]
public class Calming_Voice(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (sprite is not Aisling aisling)
        {
            GlobalSpellMethods.SpellOnFailed(sprite, target, Spell);
            return;
        }

        if (target is not Aisling targetAisling)
        {
            GlobalSpellMethods.SpellOnFailed(sprite, target, Spell);
            return;
        }

        var client = aisling.Client;
        GlobalSpellMethods.Train(aisling.Client, Spell);

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var success = GlobalSpellMethods.Execute(client, Spell);

        if (success)
        {
            foreach (var monster in targetAisling.MonstersNearby())
            {
                if (monster.Target is null) continue;
                if (monster.Target == targetAisling)
                    monster.Target = null;
            }

            targetAisling.ThreatMeter = 0;

            GlobalSpellMethods.SpellOnSuccess(sprite, target, Spell);
        }
        else
        {
            GlobalSpellMethods.SpellOnFailed(aisling, target, Spell);
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

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (target.Immunity)
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        GlobalSpellMethods.EnhancementOnUse(sprite, sprite is Monster ? sprite : target, Spell, _buff);
    }
}

// High-Elf
// Pushes all monsters back 3 tiles within 4 tiles around you.
[Script("Destructive Force")]
public class DestructiveForce(Spell spell) : SpellScript(spell)
{
    private List<Monster> _enemyList;

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        damageDealingSprite.ActionUsed = "Destructive Force";
        if (target == null)
        {
            GlobalSpellMethods.SpellOnFailed(damageDealingSprite, null, Spell);
            return;
        }

        if (target.CurrentHp > 0)
        {
            damageDealingSprite.SendAnimationNearby(Spell.Template.TargetAnimation, null, target.Serial);
            damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            damageDealingSprite.SendAnimationNearby(Spell.Template.TargetAnimation, target.Position);
        }

        var mapCheck = damageDealingSprite.Map.ID;
        if (mapCheck != damageDealingSprite.Map.ID) return;

        ThrowBack(damageDealingSprite, target);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast) return;
        if (sprite is not Aisling aisling)
        {
            GlobalSpellMethods.SpellOnFailed(sprite, target, Spell);
            return;
        }

        var client = aisling.Client;
        GlobalSpellMethods.Train(aisling.Client, Spell);

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
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
            if (targetSprite.Position.DistanceFrom((ushort)damageDealingSprite.Pos.X, (ushort)damageDealingSprite.Pos.Y) >= 5) continue;
            if (targetSprite.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dummy)) continue;

            var success = GlobalSpellMethods.Execute(damageDealingSprite.Client, Spell);

            if (success)
            {
                OnSuccess(damageDealingSprite, targetSprite);
            }
            else
            {
                GlobalSpellMethods.SpellOnFailed(damageDealingSprite, targetSprite, Spell);
            }
        }
    }

    private static void ThrowBack(Aisling aisling, Sprite target)
    {
        if (target is not Monster monster) return;
        if (monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Dummy)) return;
        var targetPosition = monster.GetPendingThrowPosition(3, monster);
        var hasHitOffWall = monster.GetPendingThrowIsWall(3, monster);
        var readyTime = DateTime.UtcNow;

        if (hasHitOffWall)
        {
            var stunned = new DebuffBeagsuain();
            stunned.OnApplied(monster, stunned);
            aisling.SendAnimationNearby(208, null, monster.Serial);
        }

        monster.Pos = new Vector2(targetPosition.X, targetPosition.Y);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendCreatureWalk(monster.Serial, new Point(targetPosition.X, targetPosition.Y), (Direction)monster.Direction));
        monster.LastMovementChanged = readyTime;
        monster.LastPosition = new Position(targetPosition.X, targetPosition.Y);
        monster.ThrownBack = true;
        monster.UpdateAddAndRemove();
        Task.Delay(1500).ContinueWith(ct => monster.ThrownBack = false);
    }
}

// High-Elf
// Random elemental moderate damage spell
[Script("Elemental Bolt")]
public class Elemental_Bolt(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target is not Damageable damageable) return;
        var dmg = (long)aisling.GetBaseDamage(aisling, target, MonsterEnums.Elemental);
        dmg = GlobalSpellMethods.AislingSpellDamageCalc(sprite, dmg, Spell, 95);
        var randomEle = Generator.RandomEnumValue<ElementManager.Element>();

        if (target.CurrentHp > 0)
        {
            aisling.SendAnimationNearby(Spell.Template.TargetAnimation, null, target.Serial);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendAnimationNearby(Spell.Template.TargetAnimation, target.Position);
        }

        damageable.ApplyElementalSpellDamage(aisling, dmg, randomEle, Spell);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Elemental Bolt";

        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        GlobalSpellMethods.Train(client, Spell);

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            var success = GlobalSpellMethods.Execute(client, Spell);

            if (success)
            {
                OnSuccess(aisling, target);
            }
            else
            {
                GlobalSpellMethods.SpellOnFailed(aisling, target, Spell);
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
        GlobalSpellMethods.Train(playerAction.Client, Spell);

        var targetList = playerAction.MonstersNearby().ToList();
        var count = targetList.Count();

        for (var i = 0; i < 3; i++)
        {
            if (count == 0)
            {
                GlobalSpellMethods.SpellOnFailed(sprite, null, Spell);
                return;
            }
            var rand = Random.Shared.Next(0, count);
            var randTarget = targetList[rand];
            _spellMethod.ElementalNecklaceOnUse(sprite, randTarget, Spell, 90 + playerAction.ExpLevel);
        }
    }
}

// Halfling
// Withdraw from the bank remotely
[Script("Remote Bank")]
public class Remote_Bank(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }


    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        playerAction.SendAnimationNearby(Spell.Template.TargetAnimation, playerAction.Position);
        playerAction.ActionUsed = "Remote Bank";

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache)
        {
            if (npc.Value.Scripts is null) continue;
            if (!npc.Value.Scripts.TryGetValue("Banker", out var scriptObj)) continue;
            scriptObj.OnClick(playerAction.Client, npc.Value.Serial);
            break;
        }
    }
}

[Script("Recall")]
public class Recall(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;

        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"That is on cooldown: {Spell.CurrentCooldown}");
            return;
        }

        playerAction.ActionUsed = "Recall";
        playerAction.Client.TransitionToMap(playerAction.PlayerNation.AreaId, playerAction.PlayerNation.MapPosition);
    }
}