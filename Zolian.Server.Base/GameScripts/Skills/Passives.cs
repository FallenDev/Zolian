using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Reduces threat more frequently | Monsters have increased damage resistance 10%
[Script("Camouflage")]
public class Camouflage : SkillScript
{
    public Camouflage(Skill skill) : base(skill) { }
    public override void OnFailed(Sprite sprite) { }
    public override void OnSuccess(Sprite sprite) { }
    public override void OnUse(Sprite sprite) { }
}

// Damage resistance by 5%
[Script("Pain Bane")]
public class PainBane : SkillScript
{
    public PainBane(Skill skill) : base(skill) { }
    public override void OnFailed(Sprite sprite) { }
    public override void OnSuccess(Sprite sprite) { }
    public override void OnUse(Sprite sprite) { }
}