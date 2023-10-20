using Darkages.Interfaces;
using Darkages.Sprites;

namespace Darkages.ScriptingBase;

public abstract class WeaponScript(Item item) : IScriptBase
{
    private Item Item { get; } = item;

    public abstract void OnUse(Sprite sprite, Action<int> cb = null);
}