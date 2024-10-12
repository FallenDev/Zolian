using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class SpellScript(Spell spell) : ObjectManager
{
    public string Arguments { get; set; }
    public bool IsScriptDefault { get; set; }
    public Spell Spell { get; set; } = spell;

    public virtual void OnActivated(Sprite sprite) { }
    public abstract void OnFailed(Sprite sprite, Sprite target);
    public virtual void OnSelectionToggle(Sprite sprite) { }
    public abstract void OnSuccess(Sprite sprite, Sprite target);
    public virtual void OnTriggeredBy(Sprite sprite, Sprite target) { }
    public abstract void OnUse(Sprite sprite, Sprite target);
}