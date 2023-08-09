using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

[Script("Remote Bank")]
public class Remote_Bank : SpellScript
{
    public Remote_Bank(Spell spell) : base(spell)
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

[Script("Needle Trap")]
public class Needle_Trap : SpellScript
{
    public Needle_Trap(Spell spell) : base(spell)
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
        target.MagicApplyDamage(sprite, 250, Spell);

        if (sprite.PlayerNearby?.Client != null)
        {
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, sprite.Serial));
        }
        else
        {
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, sprite.Serial));
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        Trap.Set(sprite, 3000, 1, OnTriggeredBy);

        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You laid a {Spell.Template.Name}");
    }
}

[Script("Stiletto Trap")]
public class Stiletto_Trap : SpellScript
{
    public Stiletto_Trap(Spell spell) : base(spell)
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
        target.MagicApplyDamage(sprite, 750, Spell);

        if (sprite.PlayerNearby?.Client != null)
        {
            sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, sprite.Serial));
        }
        else
        {
            target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, sprite.Serial));
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        Trap.Set(sprite, 3000, 1, OnTriggeredBy);

        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You laid a {Spell.Template.Name}");
    }
}

[Script("Poison Tipped Trap")]
public class Poison_Trap : SpellScript
{
    public Poison_Trap(Spell spell) : base(spell)
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
        var debuff = new debuff_Poison();

        if (target.HasBuff(debuff.Name)) return;
        debuff.OnApplied(target, debuff);
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

    public override void OnUse(Sprite sprite, Sprite target)
    {
        Trap.Set(sprite, 3000, 1, OnTriggeredBy);

        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You laid a {Spell.Template.Name}");
    }
}

[Script("Snare Trap")]
public class Snare_Trap : SpellScript
{
    public Snare_Trap(Spell spell) : base(spell)
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
        var debuff = new debuff_sleep();

        if (target.HasBuff(debuff.Name)) return;
        debuff.OnApplied(target, debuff);
        {
            if (sprite.PlayerNearby?.Client != null)
            {
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(95, null, sprite.Serial));
            }
            else
            {
                target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(95, null, sprite.Serial));
            }
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        Trap.Set(sprite, 3000, 1, OnTriggeredBy);

        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You laid a {Spell.Template.Name}");
    }
}

[Script("Flash Trap")]
public class Flash_Trap : SpellScript
{
    public Flash_Trap(Spell spell) : base(spell)
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
        var debuff = new debuff_blind();

        if (target.HasBuff(debuff.Name)) return;
        debuff.OnApplied(target, debuff);
        {
            if (sprite.PlayerNearby?.Client != null)
            {
                sprite.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(18, null, sprite.Serial));
            }
            else
            {
                target.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(18, null, sprite.Serial));
            }
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        Trap.Set(sprite, 3000, 3, OnTriggeredBy);

        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You laid a {Spell.Template.Name}");
    }
}

[Script("Hiraishin")]
public class Hiraishin : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Hiraishin(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"No suitable targets nearby.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Hiraishin";

        if (target == null)
        {
            OnFailed(damageDealingSprite, null);
            return;
        }

        if (target.Serial == damageDealingSprite.Serial) return;

        var targetPos = damageDealingSprite.GetFromAllSidesEmpty(damageDealingSprite, target);
        if (targetPos == null || targetPos == target.Position) return;
        _spellMethod.Step(damageDealingSprite, targetPos.X, targetPos.Y);
        damageDealingSprite.Facing(target.X, target.Y, out var direction);
        damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(76, null, damageDealingSprite.Serial));
        damageDealingSprite.Direction = (byte)direction;
        damageDealingSprite.Turn();
        client.SendBodyAnimation(client.Aisling.Serial, (BodyAnimation)0x82, 20, _spell.Template.Sound);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
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

            if (aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
            {
                var sprites = aisling.SpritesNearby();
                var nearestSpriteList = new SortedDictionary<int, Sprite>();

                foreach (var targetSprite in sprites)
                {
                    if (targetSprite.Serial == aisling.Serial) continue;
                    var nearestDist = aisling.Position.DistanceFrom(targetSprite.Position);
                    if (nearestSpriteList.ContainsKey(nearestDist)) continue;
                    nearestSpriteList.TryAdd(nearestDist, targetSprite);
                }

                if (nearestSpriteList.Count == 0)
                {
                    OnFailed(aisling, null);
                    return;
                }

                var nearestSpriteKey = nearestSpriteList.Keys.First();
                target = nearestSpriteList[nearestSpriteKey];

                if (target == null)
                {
                    OnFailed(aisling, null);
                    return;
                }

                OnSuccess(aisling, target);
                return;
            }

            var monsters = aisling.MonstersNearby();
            var nearestMonsterList = new SortedDictionary<int, Monster>();

            foreach (var monster in monsters)
            {
                if (monster == null) continue;
                var nearestMonsterDist = aisling.Position.DistanceFrom(monster.Position);
                if (nearestMonsterList.ContainsKey(nearestMonsterDist)) continue;
                nearestMonsterList.TryAdd(nearestMonsterDist, monster);
            }

            if (nearestMonsterList.Count == 0)
            {
                OnFailed(aisling, null);
                return;
            }

            var nearestKey = nearestMonsterList.Keys.First();
            target = nearestMonsterList[nearestKey];

            if (target == null)
            {
                OnFailed(aisling, null);
                return;
            }

            OnSuccess(aisling, target);
        }
    }
}

[Script("Shunshin")]
public class Shunshin : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Shunshin(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.PlayerNearby?.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"No suitable targets nearby.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Shunshin";

        if (target == null)
        {
            OnFailed(damageDealingSprite, null);
            return;
        }

        if (target.Serial == damageDealingSprite.Serial) return;

        var targetPos = damageDealingSprite.GetFromAllSidesEmpty(damageDealingSprite, target);
        if (targetPos == null || targetPos == target.Position)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Something got in the way.");
            return;
        }

        client.SendServerMessage(ServerMessageType.OrangeBar1, "You've blended into the shadows.");
        client.UpdateDisplay();
        var oldPos = damageDealingSprite.Pos;

        _spellMethod.Step(damageDealingSprite, targetPos.X, targetPos.Y);
        damageDealingSprite.Facing(target.X, target.Y, out var direction);
        damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(63, null, damageDealingSprite.Serial));
        damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(76, null, damageDealingSprite.Serial));

        damageDealingSprite.Direction = (byte)direction;
        damageDealingSprite.Turn();
        client.SendBodyAnimation(client.Aisling.Serial, (BodyAnimation)0x82, 20, _spell.Template.Sound);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!_spell.CanUse()) return;

        if (sprite is Aisling aisling)
        {
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

            if (aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
            {
                if (target == null)
                {
                    OnFailed(aisling, null);
                    return;
                }

                if (target.Serial == aisling.Serial)
                {
                    OnFailed(aisling, null);
                    return;
                }

                OnSuccess(aisling, target);
                return;
            }


            if (target == null)
            {
                OnFailed(aisling, null);
                return;
            }

            OnSuccess(aisling, target);
        }
    }
}