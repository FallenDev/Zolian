using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

#region Ard

[Script("Ard Athar")]
public class Tornado : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Tornado(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Athar";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 200);
    }
}

[Script("Ard Creag")]
public class Landslide : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Landslide(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Creag";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 200);
    }
}

[Script("Ard Sal")]
public class Monsoon : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Monsoon(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Sal";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 200);
    }
}

[Script("Ard Srad")]
public class Eruption : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Eruption(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Srad";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 200);
    }
}

[Script("Ard Dorcha")]
public class Twilight : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Twilight(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 200);
    }
}

[Script("Ard Eadrom")]
public class Sanctified : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Sanctified(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 200);
    }
}

#endregion

#region Mor

[Script("Mor Athar")]
public class Mor_Athar : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Athar(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Athar";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 150);
    }
}

[Script("Mor Creag")]
public class Mor_Creag : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Creag(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Creag";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 150);
    }
}

[Script("Mor Sal")]
public class Mor_Sal : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Sal(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Sal";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 150);
    }
}

[Script("Mor Srad")]
public class Mor_Srad : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Srad(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Srad";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 150);
    }
}

[Script("Mor Dorcha")]
public class Mor_Dorcha : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Dorcha(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 150);
    }
}

[Script("Mor Eadrom")]
public class Mor_Eadrom : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Eadrom(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 150);
    }
}

#endregion

#region Normal

[Script("Athar")]
public class Athar : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Athar(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Athar";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 80);
    }
}

[Script("Creag")]
public class Creag : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Creag(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Creag";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 80);
    }
}

[Script("Sal")]
public class Sal : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Sal(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Sal";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 80);
    }
}

[Script("Srad")]
public class Srad : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Srad(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Srad";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 80);
    }
}

[Script("Dorcha")]
public class Dorcha : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Dorcha(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 80);
    }
}

[Script("Eadrom")]
public class Eadrom : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Eadrom(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 80);
    }
}

#endregion

#region Beag

[Script("Beag Athar")]
public class Beag_Athar : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Athar(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Athar";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 40);
    }
}

[Script("Beag Creag")]
public class Beag_Creag : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Creag(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Creag";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 40);
    }
}

[Script("Beag Sal")]
public class Beag_Sal : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Sal(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Sal";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 40);
    }
}

[Script("Beag Srad")]
public class Beag_Srad : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Srad(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Srad";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 40);
    }
}

[Script("Beag Dorcha")]
public class Beag_Dorcha : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Dorcha(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Dorcha";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 40);
    }
}

[Script("Beag Eadrom")]
public class Beag_Eadrom : SpellScript
{
    private readonly Spell _spell;
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Eadrom(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Eadrom";

        _spellMethod.ElementalOnUse(sprite, target, _spell, 40);
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

        if (target.SpellReflect)
        {
            target.Animate(184);
            client.SendMessage(0x02, "Your weapon's spell has been negated!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You negated Gust.");

            return;
        }

        if (target.SpellNegate)
        {
            target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You deflected Gust.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Mr)
        {
            OnSuccess(aisling, target);
        }

        client.SendStats(StatusFlags.StructB);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client
                .SendMessage(0x02, $"{client.Aisling.Username} weapon releases a gust of wind.");

        var dmg = _spellMethod.WeaponDamageElementalProc(aisling, aisling.Gust);

        target.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Wind, null);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(73));

        client.SendAnimation(29, target, aisling);
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

        if (target.SpellReflect)
        {
            target.Animate(184);
            client.SendMessage(0x02, "Your weapon's spell has been negated!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You negated Quake.");

            return;
        }

        if (target.SpellNegate)
        {
            target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You deflected Quake.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Mr)
        {
            OnSuccess(aisling, target);
        }

        client.SendStats(StatusFlags.StructB);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client
                .SendMessage(0x02, $"{client.Aisling.Username} weapon releases a tremor.");

        var dmg = _spellMethod.WeaponDamageElementalProc(aisling, aisling.Quake);

        target.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Earth, null);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(73));

        client.SendAnimation(77, target, aisling);
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

        if (target.SpellReflect)
        {
            target.Animate(184);
            client.SendMessage(0x02, "Your weapon's spell has been negated!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You negated Rain.");

            return;
        }

        if (target.SpellNegate)
        {
            target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You deflected Rain.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Mr)
        {
            OnSuccess(aisling, target);
        }

        client.SendStats(StatusFlags.StructB);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client
                .SendMessage(0x02, $"{client.Aisling.Username} weapon releases a storm.");

        var dmg = _spellMethod.WeaponDamageElementalProc(aisling, aisling.Rain);

        target.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Water, null);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(73));

        client.SendAnimation(9, target, aisling);
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

        if (target.SpellReflect)
        {
            target.Animate(184);
            client.SendMessage(0x02, "Your weapon's spell has been negated!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You negated Flame.");

            return;
        }

        if (target.SpellNegate)
        {
            target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You deflected Flame.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Mr)
        {
            OnSuccess(aisling, target);
        }

        client.SendStats(StatusFlags.StructB);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client
                .SendMessage(0x02, $"{client.Aisling.Username} weapon spews forth flames.");

        var dmg = _spellMethod.WeaponDamageElementalProc(aisling, aisling.Flame);

        target.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Fire, null);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(73));

        client.SendAnimation(12, target, aisling);
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

        if (target.SpellReflect)
        {
            target.Animate(184);
            client.SendMessage(0x02, "Your weapon's spell has been negated!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You negated Dusk.");

            return;
        }

        if (target.SpellNegate)
        {
            target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You deflected Dusk.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Mr)
        {
            OnSuccess(aisling, target);
        }

        client.SendStats(StatusFlags.StructB);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client
                .SendMessage(0x02, $"{client.Aisling.Username} weapon draws from the night.");

        var dmg = _spellMethod.WeaponDamageElementalProc(aisling, aisling.Dusk);

        target.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Void, null);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(73));

        client.SendAnimation(76, target, aisling);
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

        if (target.SpellReflect)
        {
            target.Animate(184);
            client.SendMessage(0x02, "Your weapon's spell has been negated!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You negated Dawn.");
            return;
        }

        if (target.SpellNegate)
        {
            target.Animate(64);
            client.SendMessage(0x02, "Your spell has been deflected!");
            if (target is Aisling)
                target.Client.SendMessage(0x02, "You deflected Dawn.");

            return;
        }

        if (aisling.CurrentMp < 0)
            aisling.CurrentMp = 0;

        var mR = Generator.RandNumGen100();

        if (mR > target.Mr)
        {
            OnSuccess(aisling, target);
        }

        client.SendStats(StatusFlags.StructB);
    }

    private void OnSuccess(Sprite sprite, Sprite target)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (target is Aisling aislingTarget)
            aislingTarget.Client
                .SendMessage(0x02, $"{client.Aisling.Username} weapon draws from the light.");

        var dmg = _spellMethod.WeaponDamageElementalProc(aisling, aisling.Dawn);

        target.ApplyElementalSpellDamage(aisling, dmg, ElementManager.Element.Holy, null);
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(73));

        client.SendAnimation(78, target, aisling);
    }
}

#endregion