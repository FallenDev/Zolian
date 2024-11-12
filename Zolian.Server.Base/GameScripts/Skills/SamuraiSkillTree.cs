using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Draw your sword, slashing forward and to the sides, dealing critical damage. Executes if less than 10% health
[Script("Iaido")]
public class Iaido(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private IEnumerable<Sprite> _enemyList;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingAisling) return;
        var client = damageDealingAisling.Client;

        client.SendServerMessage(ServerMessageType.OrangeBar1, "My honour has not faltered..");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Iaido";

    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        var client = aisling.Client;
    }
}

// Slash that does massive mana damage, restoring your own
[Script("Mugai-ryu")]
public class MugaiRyu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Mugai-ryu";

    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;


    }
}

// Slashes enemies eight times, dealing earth, wind, fire, water elemental damage
[Script("Niten Ichi Ryu")]
public class NitenIchiRyu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Niten Ichi Ryu";

    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

    }
}

// Frontal Chain-Attack that attacks any enemy next to it, chaining outward
[Script("Shinto-ryu")]
public class ShintoRyu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Iaido";
    }

    public override void OnUse(Sprite sprite)
    {
      
    }
}

// "One Sword" Expends 100% of your mana to deal a devastating frontal attack that adds the force of your chakra (mana) to it
[Script("Itto-ru")]
public class IttoRu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Itto-ru";
    }

    public override void OnUse(Sprite sprite)
    {

    }
}

// Taunt that takes 100% threat of the enemy, deals a moderate amount of damage
[Script("Tamiya-ryu")]
public class TamiyaRyu(Skill skill) : SkillScript(skill)
{
    private Sprite _target;
    private bool _crit;
    private bool _success;
    private readonly GlobalSkillMethods _skillMethod = new();

    public override void OnFailed(Sprite sprite) { }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling aisling) return;
        var client = aisling.Client;
        aisling.ActionUsed = "Tamiya-ryu";
    }

    public override void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

    }
}