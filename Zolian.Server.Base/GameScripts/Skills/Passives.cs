using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Reduces threat more frequently | Monsters have increased damage resistance 10%
[Script("Camouflage")]
public class Camouflage(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite) { }
    protected override void OnSuccess(Sprite sprite) { }
    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite) { }
}

// Damage resistance by 5%
[Script("Pain Bane")]
public class PainBane(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite) { }
    protected override void OnSuccess(Sprite sprite) { }
    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite) { }
}

// Passive reduction of all physical damage by 15%
[Script("Crane Stance")]
public class CraneStance(Skill skill) : SkillScript(skill)
{
    protected override void OnFailed(Sprite sprite) { }

    protected override void OnSuccess(Sprite sprite) { }
    public override void OnCleanup() { }

    public override void OnUse(Sprite sprite) { }
}