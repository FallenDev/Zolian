using Darkages.GameScripts.Affects;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Spells;
// Berserker Spells

// Defender Spells
[Script("Asgall")]
public class Asgall(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_skill_reflect();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Incapacitated.");
            return;
        }

        if (sprite.HasBuff("Asgall"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        GlobalSpellMethods.EnhancementOnUse(sprite, sprite, Spell, _buff);
    }
}

[Script("Defensive Stance")]
public class Defensive_Stance(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_DefenseUp();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Incapacitated.");
            return;
        }

        if (sprite.HasBuff("Defensive Stance") || sprite.HasBuff("Spectral Shield"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        GlobalSpellMethods.EnhancementOnUse(sprite, sprite, Spell, _buff);
    }
}

// Master Spells

[Script("Perfect Defense")]
public class Perfect_Defense(Spell spell) : SpellScript(spell)
{
    private readonly Buff _buff = new buff_PerfectDefense();
    private readonly GlobalSpellMethods _spellMethod = new();

    public override void OnFailed(Sprite sprite, Sprite target) { }

    public override void OnSuccess(Sprite sprite, Sprite target) { }

    public override void OnUse(Sprite sprite, Sprite target)
    {
        if (sprite.CantCast)
        {
            if (sprite is Aisling aisling)
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Incapacitated.");
            return;
        }

        if (sprite.HasBuff("Perfect Defense") || sprite.HasBuff("Deireas Faileas"))
        {
            if (sprite is not Aisling aisling) return;
            GlobalSpellMethods.Train(aisling.Client, Spell);
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Another spell of similar nature is already applied.");
            return;
        }

        GlobalSpellMethods.EnhancementOnUse(sprite, sprite, Spell, _buff);
    }
}