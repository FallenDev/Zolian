using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.Scripting
{
    public abstract class WeaponScript : IScriptBase
    {
        protected WeaponScript(Item item) => Item = item;

        private Item Item { get; }

        public abstract void OnUse(Sprite sprite, Action<int> cb = null);
    }
}