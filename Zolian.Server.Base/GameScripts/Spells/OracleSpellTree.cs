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

        if (target is not Identifiable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 5500);
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

        if (target is not Identifiable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 5500);
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

        if (target is not Identifiable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 5500);
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

        if (target is not Identifiable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 5500);
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

        if (target is not Identifiable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 5500);
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

        if (target is not Identifiable identifiable) return;
        var enemies = identifiable.DamageableGetBehind();
        var toSide = identifiable.GetInFrontToSide();
        enemies.AddRange(toSide);
        enemies.Add(target);

        foreach (var enemy in enemies.Where(e => e != null))
            _spellMethod.ElementalOnUse(sprite, enemy, Spell, 5500);
    }
}