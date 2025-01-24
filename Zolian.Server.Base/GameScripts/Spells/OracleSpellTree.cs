using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

[Script("Uas Athar")]
public class UasTornado(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Athar";

        if (target is not Damageable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 3500);
    }
}

[Script("Uas Creag")]
public class UasLandslide(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Creag";

        if (target is not Damageable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 3500);
    }
}

[Script("Uas Sal")]
public class UasMonsoon(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Sal";

        if (target is not Damageable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 3500);
    }
}

[Script("Uas Srad")]
public class UasEruption(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Srad";

        if (target is not Damageable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 3500);
    }
}

[Script("Uas Dorcha")]
public class UasTwilight(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Dorcha";

        if (target is not Damageable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 3500);
    }
}

[Script("Uas Eadrom")]
public class UasSanctified(Spell spell) : SpellScript(spell)
{
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Eadrom";

        if (target is not Damageable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 3500);
    }
}

[Script("Uas Cradh")]
public class UasCradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffUasCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Cradh";

        if (target.HasDebuff("Uas Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Croich Ard Cradh") ||
            target.HasDebuff("Croich Mor Cradh") ||
            target.HasDebuff("Croich Cradh") ||
            target.HasDebuff("Croich Beag Cradh") ||
            target.HasDebuff("Ard Cradh") ||
            target.HasDebuff("Mor Cradh") ||
            target.HasDebuff("Cradh") ||
            target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Eclipse Seal")]
public class Eclipse_Seal(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffEclipseSeal();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Eclipse Seal";

        if (target.HasDebuff("Eclipse Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Sun Seal") ||
            target.HasDebuff("Penta Seal") ||
            target.HasDebuff("Moon Seal") ||
            target.HasDebuff("Dark Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Uas Puinsein")]
public class Uas_Puinsein(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffUasPoison();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Uas Puinsein";

        if (target.HasDebuff("Uas Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") || target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Decay'n Ruin")]
public class DecayAndRuin(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffDecay();
    private readonly Debuff _debuffTwo = new DebuffHalt();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    { 
        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuffTwo);
    }
}