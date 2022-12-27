using Darkages.Interfaces;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Scripting;

public abstract class ItemScript : ObjectManager, IScriptBase
{
    protected ItemScript(Item item) => Item = item;
    protected Item Item { get; }
    public abstract void OnUse(Sprite sprite, byte slot);
    public abstract void Equipped(Sprite sprite, byte displaySlot);
    public abstract void UnEquipped(Sprite sprite, byte displaySlot);
    public virtual void OnDropped(Sprite sprite, Position droppedPosition, Area map) { }
    public virtual void OnPickedUp(Sprite sprite, Position pickedPosition, Area map) { }
}