using Chaos.Networking.Entities.Server;

using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

// Blinds a radius of 6x6 on target
[Script("Flash Bang")]
public class FlashBang(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target == null) return;

        var targets = GetObjects(aisling.Map, i => i != null && i.WithinRangeOf(target, 6), Get.AislingDamage).ToList();

        foreach (var enemy in targets.Where(enemy => enemy != null && enemy.Serial != aisling.Serial && enemy.Attackable))
        {
            if (enemy is Aisling target2Aisling && !target2Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(53, enemy.Position));
            var debuff = new DebuffBlind();
            debuff.OnApplied(enemy, debuff);
        }

        var debuffMain = new DebuffBlind();
        debuffMain.OnApplied(target, debuffMain);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(Spell.Template.Sound, false));
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

        playerAction.ActionUsed = "Flash Bang";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        var success = _spellMethod.Execute(client, Spell);

        if (success)
        {
            OnSuccess(sprite, target);
        }
        else
        {
            _spellMethod.SpellOnFailed(playerAction, target, Spell);
        }
    }
}

// Favors an enemy race for five minutes, dealing double damage 
[Script("Favored Enemy")]
public class FavoredEnemy(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        if (target is not Monster monster) return;

        var action = new BodyAnimationArgs
        {
            AnimationSpeed = 30,
            BodyAnimation = BodyAnimation.BowShot,
            Sound = null,
            SourceId = aisling.Serial
        };

        aisling.FavoredEnemy = monster.Template.MonsterRace;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qYou now favor {aisling.FavoredEnemy}s as an enemy");
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, monster.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(action.SourceId, action.BodyAnimation, action.AnimationSpeed));

        // Favored Removal
        Task.Run(async () =>
        {
            await Task.Delay(300000);
            aisling.FavoredEnemy = MonsterRace.None;
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(80, null, aisling.Serial));
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qFavor has dissipated");
        });
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

        playerAction.ActionUsed = "Favored Enemy";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        OnSuccess(sprite, target);
    }
}

// Secures your location, reflecting magic
[Script("Secured Position")]
public class SecuredPosition(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        aisling.HeldPosition = aisling.Position;
        var buff = new aura_SecuredPosition();
        buff.OnApplied(aisling, buff);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qNow in position");
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

        playerAction.ActionUsed = "Secured Position";
        var client = playerAction.Client;
        _spellMethod.Train(client, Spell);
        OnSuccess(sprite, target);
    }
}