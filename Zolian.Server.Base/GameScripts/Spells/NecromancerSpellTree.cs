using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

// Ice DPS spell, causes "Slow" 
[Script("Chill Touch")]
public class Chill_Touch(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        playerAction.ActionUsed = "Chill Touch";
        if (target == null) return;

        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {Spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
            targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Spell.Template.Name}");
            return;
        }

        if (playerAction.CurrentMp - Spell.Template.ManaCost > 0)
        {
            playerAction.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(playerAction.Client, Spell);
        }
        else
        {
            playerAction.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var targets = GetObjects(playerAction.Map, i => i != null && i.WithinRangeOf(target, 4), Get.AislingDamage).ToList();
        foreach (var enemy in targets.Where(enemy => enemy != null && enemy.Serial != playerAction.Serial && enemy.Attackable))
        {
            if (enemy is Aisling aisling && !aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;
            var dmgCalc = DamageCalc(playerAction);
            enemy.ApplyElementalSpellDamage(sprite, dmgCalc, ElementManager.Element.Water, Spell);
            enemy.ApplyElementalSpellDamage(sprite, dmgCalc, ElementManager.Element.Wind, Spell);
            var debuff = new DebuffAdvFrozen();
            debuff.OnApplied(enemy, debuff);
            playerAction.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(377, enemy.Position));
        }
    }

    private static long DamageCalc(Aisling summoner)
    {
        return (long)(summoner.ExpLevel * summoner.AbpLevel * 0.01) * summoner.Int;
    }
}

// Multiple status afflictions
[Script("Ray of Sickness")]
public class Ray_of_Sickness(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target == null) return;
        
        if (!Spell.CanUse())
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {Spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
            targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Spell.Template.Name}");
            return;
        }

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(aisling.Client, Spell);
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (target is Aisling targetAisling && !targetAisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can't seem to get the spell off.");
            return;
        }
        
        aisling.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));

        if (!target.HasDebuff("Deadly Poison"))
        {
            var debuff = new DebuffDeadlyPoison();
            debuff.OnApplied(target, debuff);
        }

        if (!target.HasDebuff("Silence"))
        {
            var debuffTwo = new DebuffSilence();
            debuffTwo.OnApplied(target, debuffTwo);
        }

        if (target.HasDebuff("Dark Chain")) return;
        var debuffThree = new DebuffDarkChain();
        debuffThree.OnApplied(target, debuffThree);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Ray of Sickness";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);
        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(playerAction, target, Spell);
            }
        }
        else
        {
            playerAction.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
        }
    }
}

// Death ray - dps - you get it...
[Script("Finger of Death")]
public class Finger_of_Death(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        playerAction.ActionUsed = "Finger of Death";
        if (target == null) return;

        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (target.SpellReflect)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been reflected!");
            }

            if (target is Aisling targetPlayer)
            {
                targetPlayer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(184, null, target.Serial));
                targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You reflected {Spell.Template.Name}");
            }

            sprite = Spell.SpellReflect(target, sprite);
        }

        if (target.SpellNegate)
        {
            if (sprite is Aisling caster)
            {
                caster.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
                caster.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            }

            if (target is not Aisling targetPlayer) return;
            targetPlayer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, target.Serial));
            targetPlayer.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Spell.Template.Name}");
            return;
        }

        if (playerAction.CurrentMp - Spell.Template.ManaCost > 0)
        {
            playerAction.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(playerAction.Client, Spell);
        }
        else
        {
            playerAction.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var enemies = target.DamageableGetBehind(4);

        foreach (var enemy in enemies)
        {
            _spellMethod.ElementalOnSuccess(sprite, enemy, Spell, 2500);
        }

        _spellMethod.ElementalOnSuccess(sprite, target, Spell, 5000);
    }
}

// Explode corpses of summons causing damage 
[Script("Corpse Burst")]
public class Corpse_Burst(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(aisling.Client, Spell);
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var corpsesNearby = GetObjects(aisling.Map, s => s.WithinRangeOf(aisling), Get.Items)
            .Where(i => i is Item item && item.Template.Name == "Corpse").ToList();

        if (corpsesNearby.Count == 0) return;
        var manaSap = aisling.CurrentMp * .50;
        aisling.CurrentMp -= (long)manaSap;
        aisling.Client.SendAttributes(StatUpdateType.Vitality);

        foreach (var corpse in corpsesNearby)
        {
            var targets = GetObjects(aisling.Map, i => i != null && i.WithinRangeOf(corpse, 4), Get.AislingDamage).ToList();
            foreach (var enemy in targets.Where(enemy => enemy != null && enemy.Serial != aisling.Serial && enemy.Attackable))
            {
                var dmgCalc = DamageCalc(aisling);
                enemy.ApplyElementalSpellDamage(sprite, dmgCalc, ElementManager.Element.Void, Spell);
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(77, enemy.Position));
            }

            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, corpse.Position));
            corpse.Remove();
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Corpse Burst";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);
        var mR = Generator.RandNumGen100();

        if (mR <= target.Will)
        {
            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(playerAction, target, Spell);
            }
        }
        else
        {
            playerAction.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
        }
    }

    private static long DamageCalc(Aisling summoner)
    {
        return (long)(summoner.CurrentMp * summoner.AbpLevel * 0.01) * summoner.Int;
    }
}

// Take control of an undead enemy with critical health
[Script("Command Undead")]
public class Command_Undead(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Not a valid target!");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(aisling.Client, Spell);
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (target.CurrentHp > 0)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, target.Serial));
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(Spell.Template.Sound, false));
        }
        else
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, target.Position));
        }

        if (target is not Monster monster)
        {
            _spellMethod.SpellOnFailed(sprite, target, Spell);
            return;
        }

        if (monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Undead) || monster.Template.MonsterRace.MonsterRaceIsSet(MonsterRace.Aberration) && monster.CurrentHp <= monster.MaximumHp * .10)
        {
            ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("RaisedSkel", out var skel);
            var summoned = Monster.Summon(skel, aisling);
            if (summoned == null) return;
            summoned.Image = monster.Image;
            summoned.X = monster.X;
            summoned.Y = monster.Y;
            summoned.Direction = monster.Direction;
            AddObject(summoned);
            monster.Remove();
        }
        else
        {
            OnFailed(sprite, target);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling playerAction) return;
        if (target == null) return;
        if (!Spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        playerAction.ActionUsed = "Command Undead";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);
        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            if (success)
            {
                OnSuccess(sprite, target);
            }
            else
            {
                _spellMethod.SpellOnFailed(playerAction, target, Spell);
            }
        }
        else
        {
            playerAction.Client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, target.Serial));
        }
    }
}

// Summon a powerful undead to fight for you
[Script("Animate Dead")]
public class Animate_Dead(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(aisling.Client, Spell);
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
        {
            _spellMethod.SpellOnFailed(aisling, aisling, Spell);
            return;
        }

        ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("RaisedBrut", out var skel);
        var summoned = Monster.Summon(skel, aisling);
        if (summoned == null) return;
        AddObject(summoned);
    }
}

// Cast Croich Ard Cradh on all enemies in sight
[Script("Circle of Death")]
public class Circle_of_Death(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Circle of Death";

        var manaSap = (long)(aisling.MaximumMp * .50);

        if (aisling.CurrentMp < manaSap)
        {
            OnFailed(aisling, target);
            return;
        }

        aisling.CurrentMp -= manaSap;
        ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("RaisedSkel", out var skel);

        foreach (var nearby in aisling.SpritesNearby())
        {
            if (nearby.Serial == aisling.Serial) continue;

            if (nearby.SpellNegate)
            {
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(64, null, nearby.Serial));
                client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");

                if (nearby is Aisling player)
                    player.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"You deflected {Spell.Template.Name}");

                continue;
            }

            var mR = Generator.RandNumGen100();

            if (mR > nearby.Will)
            {
                nearby.ApplyElementalSpellDamage(aisling, (aisling.Int + aisling.Wis) * (aisling.Level + aisling.AbpLevel), ElementManager.Element.Void, Spell);

                if (!nearby.IsCradhed)
                {
                    var debuff = new DebuffCriochArdCradh();
                    debuff.OnApplied(nearby, debuff);
                }

                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, nearby.Position));
            }
            else
            {
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, nearby.Serial));
            }

            if (nearby.Alive) continue;
            if (aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap)) continue;
            var summoned = Monster.Summon(skel, aisling);
            if (summoned == null) return;
            AddObject(summoned);
        }
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (!Spell.CanUse()) return;
        if (sprite is Aisling aisling)
        {
            var client = aisling.Client;
            _spellMethod.Train(client, Spell);
            OnSuccess(aisling, target);
            client.SendAttributes(StatUpdateType.Vitality);
            return;
        }

        foreach (var targetObj in sprite.AislingsNearby())
        {
            if (targetObj == null) continue;
            targetObj.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, targetObj.Position));

            // monster use of the spell
        }
    }
}

// Summon multiple skeletons to fight for you
[Script("Macabre")]
public class Macabre(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;

        if (aisling.CurrentMp - Spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= Spell.Template.ManaCost;
            _spellMethod.Train(aisling.Client, Spell);
        }
        else
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        if (aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
        {
            _spellMethod.SpellOnFailed(aisling, aisling, Spell);
            return;
        }

        ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue("RaisedSkel", out var skel);

        for (var i = 0; i < 3; i++)
        {
            var summoned = Monster.Summon(skel, aisling);
            if (summoned == null) return;
            AddObject(summoned);
        }
    }
}