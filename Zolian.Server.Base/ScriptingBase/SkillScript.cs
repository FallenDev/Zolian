using Darkages.GameScripts.Skills;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class SkillScript(Skill skill) : ObjectManager
{
    public Skill Skill { get; set; } = skill;

    protected abstract void OnFailed(Sprite sprite, Sprite target = null);
    protected abstract void OnSuccess(Sprite sprite);
    // For skills that pass a single target
    protected virtual void OnSuccess(Sprite sprite, Sprite target) { }
    // For skills that pass multiple targets as an array
    protected virtual void OnSuccess(Sprite sprite, Sprite[] targets) { }

    public virtual void OnUse(Sprite sprite)
    {
        if (!Skill.CanUse()) return;

        if (sprite is Aisling aisling)
        {
            var success = GlobalSkillMethods.OnUse(aisling, Skill);

            if (success)
            {
                OnSuccess(aisling);
            }
            else
            {
                OnFailed(aisling);
            }
        }
        else
        {
            OnSuccess(sprite);
        }
    }

    public virtual void ItemOnDropped(Sprite sprite, Position pos, Area map) { }
}