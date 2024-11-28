using Darkages.Common;
using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

#region Ard

[Script("Ard Athar")]
public class Tornado(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Athar";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 300);
    }
}

[Script("Ard Creag")]
public class Landslide(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Creag";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 300);
    }
}

[Script("Ard Sal")]
public class Monsoon(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Sal";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 300);
    }
}

[Script("Ard Srad")]
public class Eruption(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Srad";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 300);
    }
}

[Script("Ard Dorcha")]
public class Twilight(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 300);
    }
}

[Script("Ard Eadrom")]
public class Sanctified(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 300);
    }
}

#endregion

#region Mor

[Script("Mor Athar")]
public class Mor_Athar(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Athar";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 150);
    }
}

[Script("Mor Creag")]
public class Mor_Creag(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Creag";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 150);
    }
}

[Script("Mor Sal")]
public class Mor_Sal(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Sal";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 150);
    }
}

[Script("Mor Srad")]
public class Mor_Srad(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Srad";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 150);
    }
}

[Script("Mor Dorcha")]
public class Mor_Dorcha(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 150);
    }
}

[Script("Mor Eadrom")]
public class Mor_Eadrom(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 150);
    }
}

#endregion

#region Normal

[Script("Athar")]
public class Athar(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Athar";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 80);
    }
}

[Script("Creag")]
public class Creag(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Creag";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 80);
    }
}

[Script("Sal")]
public class Sal(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Sal";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 80);
    }
}

[Script("Srad")]
public class Srad(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Srad";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 80);
    }
}

[Script("Dorcha")]
public class Dorcha(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 80);
    }
}

[Script("Eadrom")]
public class Eadrom(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 80);
    }
}

#endregion

#region Beag

[Script("Beag Athar")]
public class Beag_Athar(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Athar";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 40);
    }
}

[Script("Beag Creag")]
public class Beag_Creag(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Creag";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 40);
    }
}

[Script("Beag Sal")]
public class Beag_Sal(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Sal";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 40);
    }
}

[Script("Beag Srad")]
public class Beag_Srad(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Srad";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 40);
    }
}

[Script("Beag Dorcha")]
public class Beag_Dorcha(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 40);
    }
}

[Script("Beag Eadrom")]
public class Beag_Eadrom(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, Spell, 40);
    }
}

#endregion

#region Weapon

public class Gust
{
    private readonly GlobalSpellMethods _spellMethod;

    public Gust(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Gust";
        _spellMethod = new GlobalSpellMethods();

        if (target is not Damageable damageable) return;

        if (target.SpellReflect)
        {
            damageable.SendAnimationNearby(184, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your weapon's spell has been negated!");
            return;
        }

        if (target.SpellNegate)
        {
            damageable.SendAnimationNearby(64, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            OnSuccess(aisling, target);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{client.Aisling.Username} weapon releases a gust of wind.");

        var dmg = GlobalSpellMethods.WeaponDamageElementalProc(aisling, aisling.Gust);
        if (target is not Damageable damageable) return;
        damageable.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Wind, null);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(73, false));
        aisling.SendAnimationNearby(29, null, target.Serial);
    }
}

public class Quake
{
    private readonly GlobalSpellMethods _spellMethod;

    public Quake(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Quake";
        _spellMethod = new GlobalSpellMethods();

        if (target is not Damageable damageable) return;

        if (target.SpellReflect)
        {
            damageable.SendAnimationNearby(184, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your weapon's spell has been negated!");
            return;
        }

        if (target.SpellNegate)
        {
            damageable.SendAnimationNearby(64, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            OnSuccess(aisling, target);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{client.Aisling.Username} weapon releases a tremor.");

        var dmg = GlobalSpellMethods.WeaponDamageElementalProc(aisling, aisling.Quake);
        if (target is not Damageable damageable) return;
        damageable.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Earth, null);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(73, false));
        aisling.SendAnimationNearby(77, null, target.Serial);
    }
}

public class Rain
{
    private readonly GlobalSpellMethods _spellMethod;

    public Rain(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Rain";
        _spellMethod = new GlobalSpellMethods();

        if (target is not Damageable damageable) return;

        if (target.SpellReflect)
        {
            damageable.SendAnimationNearby(184, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your weapon's spell has been negated!");
            return;
        }

        if (target.SpellNegate)
        {
            damageable.SendAnimationNearby(64, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            OnSuccess(aisling, target);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{client.Aisling.Username} weapon releases a storm.");

        var dmg = GlobalSpellMethods.WeaponDamageElementalProc(aisling, aisling.Rain);
        if (target is not Damageable damageable) return;
        damageable.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Water, null);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(73, false));
        aisling.SendAnimationNearby(9, null, target.Serial);
    }
}

public class Flame
{
    private readonly GlobalSpellMethods _spellMethod;

    public Flame(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Flame";
        _spellMethod = new GlobalSpellMethods();

        if (target is not Damageable damageable) return;

        if (target.SpellReflect)
        {
            damageable.SendAnimationNearby(184, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your weapon's spell has been negated!");
            return;
        }

        if (target.SpellNegate)
        {
            damageable.SendAnimationNearby(64, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            OnSuccess(aisling, target);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{client.Aisling.Username} weapon spews forth flames.");

        var dmg = GlobalSpellMethods.WeaponDamageElementalProc(aisling, aisling.Flame);
        if (target is not Damageable damageable) return;
        damageable.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Fire, null);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(73, false));
        aisling.SendAnimationNearby(12, null, target.Serial);
    }
}

public class Dusk
{
    private readonly GlobalSpellMethods _spellMethod;

    public Dusk(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Dusk";
        _spellMethod = new GlobalSpellMethods();

        if (target is not Damageable damageable) return;

        if (target.SpellReflect)
        {
            damageable.SendAnimationNearby(184, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your weapon's spell has been negated!");
            return;
        }

        if (target.SpellNegate)
        {
            damageable.SendAnimationNearby(64, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            OnSuccess(aisling, target);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{client.Aisling.Username} weapon draws from the night.");

        var dmg = GlobalSpellMethods.WeaponDamageElementalProc(aisling, aisling.Dusk);
        if (target is not Damageable damageable) return;
        damageable.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Void, null);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(73, false));
        aisling.SendAnimationNearby(76, null, target.Serial);
    }
}

public class Dawn
{
    private readonly GlobalSpellMethods _spellMethod;

    public Dawn(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Dawn";
        _spellMethod = new GlobalSpellMethods();

        if (target is not Damageable damageable) return;

        if (target.SpellReflect)
        {
            damageable.SendAnimationNearby(184, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your weapon's spell has been negated!");
            return;
        }

        if (target.SpellNegate)
        {
            damageable.SendAnimationNearby(64, null, target.Serial);
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Your spell has been deflected!");
            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Will)
        {
            OnSuccess(aisling, target);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{client.Aisling.Username} weapon draws from the light.");

        var dmg = GlobalSpellMethods.WeaponDamageElementalProc(aisling, aisling.Dawn);
        if (target is not Damageable damageable) return;
        damageable.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Holy, null);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(73, false));
        aisling.SendAnimationNearby(78, null, target.Serial);
    }
}

#endregion