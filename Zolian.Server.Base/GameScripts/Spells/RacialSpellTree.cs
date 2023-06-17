using System.Numerics;
using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
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

        target.Show(Scope.NearbyAislings,
            new ServerFormat29((uint)target.Serial, (uint)target.Serial,
                Spell.Template.TargetAnimation, 0, 100));
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        Trap.Set(sprite, 3000, 1, OnTriggeredBy);

        if (sprite is Aisling aisling)
            aisling.Client.SendMessage(0x02, $"You threw down {Spell.Template.Name}");
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
        if (!sprite.CanCast) return;
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

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= _spell.Template.ManaCost;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var success = _spellMethod.Execute(client, _spell);

        if (success)
        {
            if (client.Aisling.Invisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
            {
                client.Aisling.Invisible = false;
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

        client.SendStats(StatusFlags.StructB);
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
        if (!sprite.CanCast) return;
        if (target.Immunity)
        {
            target.Client.SendMessage(0x02, "Another spell of similar nature is already applied.");
            _spellMethod.SpellOnFailed(sprite, target, _spell);
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
        damageDealingSprite.Cast(_spell, target);
        damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat19(_spell.Template.Sound));
        
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
        if (!sprite.CanCast) return;
        if (sprite is not Aisling aisling)
        {
            _spellMethod.SpellOnFailed(sprite, target, _spell);
            return;
        }

        var client = aisling.Client;

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= _spell.Template.ManaCost;
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        if (client.Aisling.Invisible && _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
        {
            client.Aisling.Invisible = false;
            client.UpdateDisplay();
        }

        Target(aisling);
        client.SendStats(StatusFlags.StructB);
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
        var readyTime = DateTime.Now;
        var response = new ServerFormat0C
        {
            Direction = _target.Direction,
            Serial = _target.Serial,
            X = (short)targetPosition.X,
            Y = (short)targetPosition.Y
        };

        if (hasHitOffWall)
        {
            var stunned = new debuff_beagsuain();
            stunned.OnApplied(_target, stunned);
            _target.Animate(208);
        }

        _target.Pos = new Vector2(targetPosition.X, targetPosition.Y);

        _target.Show(Scope.NearbyAislings, response);
        {
            _target.LastMovementChanged = readyTime;
            _target.LastPosition = new Position(targetPosition.X, targetPosition.Y);
        }

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
        var client = aisling.Client;
        var dmg = (long)aisling.GetBaseDamage(aisling, target, MonsterEnums.Elemental);
        dmg = _spellMethod.AislingSpellDamageCalc(sprite, dmg, _spell, 95);
        var randomEle = Generator.RandomEnumValue<ElementManager.Element>();

        aisling.Cast(_spell, target);
        target.ApplyElementalSpellDamage(aisling, dmg, randomEle, _spell);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Elemental Bolt";

        if (!_spell.CanUse())
        {
            if (sprite is Aisling)
                sprite.Client.SendMessage(0x02, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            target.Animate(184);
            if (sprite is Aisling)
                sprite.Client.SendMessage(0x02, "Your spell has been reflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, $"You reflected {_spell.Template.Name}.");

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
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (target.SpellNegate)
        {
            target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, $"You deflected {_spell.Template.Name}.");

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
                if (client.Aisling.Invisible &&
                    _spell.Template.PostQualifiers is PostQualifier.BreakInvisible or PostQualifier.Both)
                {
                    client.Aisling.Invisible = false;
                    client.UpdateDisplay();
                }

                OnSuccess(aisling, target);
            }
            else
            {
                _spellMethod.ElementalOnFailed(aisling, target, _spell);
            }
        }

        client.SendStats(StatusFlags.StructB);
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

