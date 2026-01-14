using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

#region Mastery

[Script("Sun Seal")]
public class Sun_Seal(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffSunSeal();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Sun Seal";

        if (target.HasDebuff("Sun Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Penta Seal") ||
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

[Script("Penta Seal")]
public class Penta_Seal(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffPentaSeal();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Penta Seal";

        if (target.HasDebuff("Sun Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Penta Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Moon Seal") ||
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

[Script("Moon Seal")]
public class Moon_Seal(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffMoonSeal();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Moon Seal";

        if (target.HasDebuff("Sun Seal") ||
            target.HasDebuff("Penta Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Moon Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Dark Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Dark Seal")]
public class Dark_Seal(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffDarkSeal();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Dark Seal";

        if (target.HasDebuff("Sun Seal") ||
            target.HasDebuff("Penta Seal") ||
            target.HasDebuff("Moon Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Dark Seal"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Croich Ard Cradh")]
public class CroichArdCradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffCriochArdCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Croich Ard Cradh";

        if (target.HasDebuff("Croich Ard Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Croich Mor Cradh") ||
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

[Script("Croich Mor Cradh")]
public class CroichMorCradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffCriochMorCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Croich Mor Cradh";

        if (target.HasDebuff("Croich Ard Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Croich Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Croich Cradh") ||
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

[Script("Croich Cradh")]
public class CroichCradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffCriochCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Croich Cradh";

        if (target.HasDebuff("Croich Ard Cradh") ||
            target.HasDebuff("Croich Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Croich Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Croich Beag Cradh") ||
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

[Script("Croich Beag Cradh")]
public class CroichBeagCradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffCriochBeagCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Croich Beag Cradh";

        if (target.HasDebuff("Croich Ard Cradh") ||
            target.HasDebuff("Croich Mor Cradh") ||
            target.HasDebuff("Croich Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Croich Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Ard Cradh") ||
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

#endregion

#region Ard

[Script("Ard Cradh")]
public class Ard_Cradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffArdcradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Cradh";

        if (target.HasDebuff("Croich Ard Cradh") ||
            target.HasDebuff("Croich Mor Cradh") ||
            target.HasDebuff("Croich Cradh") ||
            target.HasDebuff("Croich Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Ard Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Cradh") || target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Ard Puinsein")]
public class Ard_Puinsein(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffArdPoison();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Puinsein";

        if (target.HasDebuff("Ard Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Puinsein") || target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

#endregion

#region Mor

[Script("Mor Cradh")]
public class Mor_Cradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffMorcradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Cradh";

        if (target.HasDebuff("Croich Ard Cradh") ||
            target.HasDebuff("Croich Mor Cradh") ||
            target.HasDebuff("Croich Cradh") ||
            target.HasDebuff("Croich Beag Cradh") ||
            target.HasDebuff("Ard Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Mor Puinsein")]
public class Mor_Puinsein(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffMorPoison();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Puinsein";

        if (target.HasDebuff("Ard Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

#endregion

#region Normal

[Script("Silence")]
public class Silence(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffSilence();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Silence";

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Cradh")]
public class Cradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffCradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Cradh";

        if (target.HasDebuff("Croich Ard Cradh") ||
            target.HasDebuff("Croich Mor Cradh") ||
            target.HasDebuff("Croich Cradh") ||
            target.HasDebuff("Croich Beag Cradh") ||
            target.HasDebuff("Ard Cradh") ||
            target.HasDebuff("Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Puinsein")]
public class Puinsein(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffPoison();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Puinsein";

        if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Blind")]
public class Blind(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffBlind();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Blind";

        if (target.HasDebuff("Blind"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

#endregion

#region Beag

[Script("Beag Cradh")]
public class Beag_Cradh(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffBeagcradh();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Cradh";

        if (target.HasDebuff("Croich Ard Cradh") ||
            target.HasDebuff("Croich Mor Cradh") ||
            target.HasDebuff("Croich Cradh") ||
            target.HasDebuff("Croich Beag Cradh") ||
            target.HasDebuff("Ard Cradh") ||
            target.HasDebuff("Mor Cradh") ||
            target.HasDebuff("Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

[Script("Beag Puinsein")]
public class Beag_Puinsein(Spell spell) : SpellScript(spell)
{
    private readonly Debuff _debuff = new DebuffBeagPoison();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Puinsein";

        if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") || target.HasDebuff("Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, Spell, _debuff);
    }
}

#endregion