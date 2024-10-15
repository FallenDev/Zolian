using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Spells;

// Ice DPS spell, causes "Slow" 
[Script("Chill Touch")]
public class Chill_Touch(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target)
    {

    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {

    }

    public override void OnUse(Sprite sprite, Sprite target)
    {

    }
}

// Multiple status afflictions (Afflictions cannot be removed by dispelling)
[Script("Ray of Sickness")]
public class Ray_of_Sickness(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target)
    {

    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {

    }

    public override void OnUse(Sprite sprite, Sprite target)
    {

    }
}

// Death ray - dps - you get it...
[Script("Finger of Death")]
public class Finger_of_Death(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target)
    {

    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {

    }

    public override void OnUse(Sprite sprite, Sprite target)
    {

    }
}

// Over the next 3 minutes where enemies die a (trap) will be set on their corpse to explode after 3 seconds
[Script("Corpse Burst")]
public class Corpse_Burst(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target)
    {

    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {

    }

    public override void OnUse(Sprite sprite, Sprite target)
    {

    }
}

// Take control of an undead enemy with critical health
[Script("Command Undead")]
public class Command_Undead(Spell spell) : SpellScript(spell)
{
    public override void OnFailed(Sprite sprite, Sprite target)
    {

    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {

    }

    public override void OnUse(Sprite sprite, Sprite target)
    {

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
        if (aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
        {
            _spellMethod.SpellOnFailed(aisling, aisling, spell);
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
                nearby.ApplyElementalSpellDamage(aisling, 500 * nearby.Level, ElementManager.Element.Terror, Spell);
                
                if (!nearby.IsCradhed)
                {
                    var debuff = new DebuffCriochArdCradh();
                    debuff.OnApplied(nearby, debuff);
                }

                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(Spell.Template.TargetAnimation, null, nearby.Serial));
            }
            else
            {
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(115, null, nearby.Serial));
            }
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
        if (aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
        {
            _spellMethod.SpellOnFailed(aisling, aisling, spell);
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