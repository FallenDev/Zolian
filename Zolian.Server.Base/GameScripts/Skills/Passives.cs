using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Reduces threat more frequently | Monsters have increased damage resistance 10%
[Script("Camouflage")]
public class Camouflage(Skill skill) : SkillScript(skill)
{
    public override void OnFailed(Sprite sprite) { }
    public override void OnSuccess(Sprite sprite) { }
    public override void OnUse(Sprite sprite) { }
}

// Damage resistance by 5%
[Script("Pain Bane")]
public class PainBane(Skill skill) : SkillScript(skill)
{
    public override void OnFailed(Sprite sprite) { }
    public override void OnSuccess(Sprite sprite) { }
    public override void OnUse(Sprite sprite) { }
}