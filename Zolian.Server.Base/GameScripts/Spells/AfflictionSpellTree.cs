using Chaos.Common.Definitions;

using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

#region Mastery

[Script("Sun Seal")]
public class Sun_Seal : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffSunSeal();
    private readonly GlobalSpellMethods _spellMethod;

    public Sun_Seal(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Penta Seal") ||
            target.HasDebuff("Moon Seal") ||
            target.HasDebuff("Dark Seal"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Penta Seal")]
public class Penta_Seal : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffPentaSeal();
    private readonly GlobalSpellMethods _spellMethod;

    public Penta_Seal(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Penta Seal"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Moon Seal") ||
            target.HasDebuff("Dark Seal"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Moon Seal")]
public class Moon_Seal : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffMoonSeal();
    private readonly GlobalSpellMethods _spellMethod;

    public Moon_Seal(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Moon Seal"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Dark Seal"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Dark Seal")]
public class Dark_Seal : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffDarkSeal();
    private readonly GlobalSpellMethods _spellMethod;

    public Dark_Seal(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Dark Seal"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Croich Ard Cradh")]
public class CroichArdCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffCriochArdCradh();
    private readonly GlobalSpellMethods _spellMethod;

    public CroichArdCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Croich Mor Cradh")]
public class CroichMorCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffCriochMorCradh();
    private readonly GlobalSpellMethods _spellMethod;

    public CroichMorCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Croich Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Croich Cradh")]
public class CroichCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffCriochCradh();
    private readonly GlobalSpellMethods _spellMethod;

    public CroichCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Croich Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Croich Beag Cradh")]
public class CroichBeagCradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffCriochBeagCradh();
    private readonly GlobalSpellMethods _spellMethod;

    public CroichBeagCradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Croich Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Ard Cradh") ||
            target.HasDebuff("Mor Cradh") || 
            target.HasDebuff("Cradh") || 
            target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

#endregion

#region Ard

[Script("Ard Fas Nadur")]
public class Ard_Fas_Nadur : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffArdfasnadur();
    private readonly GlobalSpellMethods _spellMethod;

    public Ard_Fas_Nadur(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        switch (sprite)
        {
            case Aisling playerAction:
                playerAction.ActionUsed = "Ard Fas Nadur";
                break;
            case Monster monsterAction:
                target = monsterAction;
                break;
        }

        if (target.HasDebuff("Ard Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Fas Nadur") || target.HasDebuff("Fas Nadur") || target.HasDebuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Ard Cradh")]
public class Ard_Cradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffArdcradh();
    private readonly GlobalSpellMethods _spellMethod;

    public Ard_Cradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Ard Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Cradh") || target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Ard Puinsein")]
public class Ard_Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffArdPoison();
    private readonly GlobalSpellMethods _spellMethod;

    public Ard_Puinsein(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Puinsein") || target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

#endregion

#region Mor

[Script("Mor Fas Nadur")]
public class Mor_Fas_Nadur : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffMorfasnadur();
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Fas_Nadur(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        switch (sprite)
        {
            case Aisling playerAction:
                playerAction.ActionUsed = "Mor Fas Nadur";
                break;
            case Monster monsterAction:
                target = monsterAction;
                break;
        }

        if (target.HasDebuff("Ard Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Fas Nadur") || target.HasDebuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Mor Cradh")]
public class Mor_Cradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffMorcradh();
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Cradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Mor Puinsein")]
public class Mor_Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffMorPoison();
    private readonly GlobalSpellMethods _spellMethod;

    public Mor_Puinsein(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

#endregion

#region Normal

[Script("Silence")]
public class Silence : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffSilence();
    private readonly GlobalSpellMethods _spellMethod;

    public Silence(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Silence";

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Fas Nadur")]
public class Fas_Nadur : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffFasnadur();
    private readonly GlobalSpellMethods _spellMethod;

    public Fas_Nadur(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;

        switch (sprite)
        {
            case Aisling playerAction:
                playerAction.ActionUsed = "Fas Nadur";
                break;
            case Monster monsterAction:
                target = monsterAction;
                break;
        }

        if (target.HasDebuff("Ard Fas Nadur") || target.HasDebuff("Mor Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Cradh")]
public class Cradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffCradh();
    private readonly GlobalSpellMethods _spellMethod;

    public Cradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Puinsein")]
public class Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffPoison();
    private readonly GlobalSpellMethods _spellMethod;

    public Puinsein(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Blind")]
public class Blind : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffBlind();
    private readonly GlobalSpellMethods _spellMethod;

    public Blind(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Fas Spiorad")]
public class Fas_Spiorad : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffFasspiorad();
    private readonly GlobalSpellMethods _spellMethod;

    public Fas_Spiorad(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"Your body is too weak.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;

        if (!_spell.CanUse())
        {
            if (sprite is Aisling aisling2)
                aisling2.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Ability is not quite ready yet.");
            return;
        }

        if (aisling.CurrentMp - _spell.Template.ManaCost > 0)
        {
            aisling.CurrentMp -= _spell.Template.ManaCost;
            _spellMethod.Train(client, _spell);
        }
        else
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");
            return;
        }

        var success = _spellMethod.Execute(client, _spell);

        if (success)
        {
            _spellMethod.AfflictionOnSuccess(aisling, target, _spell, _debuff);
        }
        else
        {
            _spellMethod.SpellOnFailed(aisling, target, _spell);
        }

        client.SendAttributes(StatUpdateType.Vitality);
    }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target == null) return;
        if (target.HasDebuff("Fas Spiorad"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your body is too weak.");
            return;
        }

        var healthCheck = (int)(target.MaximumHp * 0.33);

        if (healthCheck > 0)
        {
            _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
        }
        else
        {
            OnFailed(sprite, target);
        }
    }
}

#endregion

#region Beag

[Script("Beag Cradh")]
public class Beag_Cradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffBeagcradh();
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Cradh(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Beag Puinsein")]
public class Beag_Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new DebuffBeagPoison();
    private readonly GlobalSpellMethods _spellMethod;

    public Beag_Puinsein(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

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
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

#endregion