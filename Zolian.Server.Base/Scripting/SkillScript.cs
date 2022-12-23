using Darkages.Interfaces;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Scripting
{
    public abstract class SkillScript : ObjectManager, IScriptBase, IUseable
    {
        protected SkillScript(Skill skill) => Skill = skill;

        public Skill Skill { get; set; }

        public abstract void OnFailed(Sprite sprite);
        public abstract void OnSuccess(Sprite sprite);
        public abstract void OnUse(Sprite sprite);
        public virtual void ItemOnDropped(Sprite sprite, Position pos, Area map) { }
    }
}