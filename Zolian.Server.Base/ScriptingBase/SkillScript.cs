using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class SkillScript(Skill skill) : ObjectManager
{
    public Skill Skill { get; set; } = skill;

    public abstract void OnFailed(Sprite sprite);
    public abstract void OnSuccess(Sprite sprite);
    public abstract void OnUse(Sprite sprite);
    public virtual void ItemOnDropped(Sprite sprite, Position pos, Area map) { }
}