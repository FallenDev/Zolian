using Darkages.GameScripts.Affects;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;

#region Ard

[Script("Ard Fas Nadur")]
public class Ard_Fas_Nadur : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_ardfasnadur();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Fas Nadur";

        if (target.HasDebuff("Ard Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Fas Nadur") || target.HasDebuff("Fas Nadur") || target.HasDebuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Ard Cradh")]
public class Ard_Cradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_ardcradh();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Cradh";

        if (target.HasDebuff("Ard Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Cradh") || target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Ard Puinsein")]
public class Ard_Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_ArdPoison();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Ard Puinsein";

        if (target.HasDebuff("Ard Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Mor Puinsein") || target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
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
    private readonly Debuff _debuff = new debuff_morfasnadur();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Fas Nadur";

        if (target.HasDebuff("Ard Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Fas Nadur") || target.HasDebuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Mor Cradh")]
public class Mor_Cradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_morcradh();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Cradh";

        if (target.HasDebuff("Ard Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Cradh") || target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Mor Puinsein")]
public class Mor_Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_MorPoison();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Mor Puinsein";

        if (target.HasDebuff("Ard Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Mor Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Puinsein") || target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
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
    private readonly Debuff _debuff = new debuff_Silence();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Silence";

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Fas Nadur")]
public class Fas_Nadur : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_fasnadur();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Fas Nadur";

        if (target.HasDebuff("Ard Fas Nadur") || target.HasDebuff("Mor Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Fas Nadur"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Cradh")]
public class Cradh : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_cradh();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Cradh";

        if (target.HasDebuff("Ard Cradh") || target.HasDebuff("Mor Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Puinsein")]
public class Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_Poison();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Puinsein";

        if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        if (target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A lessor version has already been cast.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Blind")]
public class Blind : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_blind();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Blind";

        if (target.HasDebuff("Blind"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "Another spell of similar nature is already applied.");
            return;
        };

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Fas Spiorad")]
public class Fas_Spiorad : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_fasspiorad();
    private readonly GlobalSpellMethods _spellMethod;

    public Fas_Spiorad(Spell spell) : base(spell)
    {
        _spell = spell;
        _spellMethod = new GlobalSpellMethods();
    }

    public override void OnFailed(Sprite sprite, Sprite target)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendMessage(0x02, $"Your body is too weak.");
    }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (target.HasDebuff("Fas Spiorad"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            target.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your body is too weak.");
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
    private readonly Debuff _debuff = new debuff_beagcradh();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Cradh";

        if (target.HasDebuff("Ard Cradh") || target.HasDebuff("Mor Cradh") || target.HasDebuff("Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Beag Cradh"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

[Script("Beag Puinsein")]
public class Beag_Puinsein : SpellScript
{
    private readonly Spell _spell;
    private readonly Debuff _debuff = new debuff_BeagPoison();
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
        if (sprite is Aisling playerAction)
            playerAction.ActionUsed = "Beag Puinsein";

        if (target.HasDebuff("Ard Puinsein") || target.HasDebuff("Mor Puinsein") || target.HasDebuff("Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "A more potent version has already been cast.");
            return;
        }

        if (target.HasDebuff("Beag Puinsein"))
        {
            if (sprite is not Aisling aisling) return;
            _spellMethod.Train(aisling.Client, _spell);
            aisling.Client.SendMessage(0x02, "You've already cast that spell.");
            return;
        }

        _spellMethod.AfflictionOnUse(sprite, target, _spell, _debuff);
    }
}

#endregion