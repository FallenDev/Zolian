﻿using Darkages.Interfaces;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class ItemScript(Item item) : ObjectManager, IScriptBase
{
    protected Item Item { get; } = item;
    public abstract void OnUse(Sprite sprite, byte slot);
    public abstract void Equipped(Sprite sprite, byte displaySlot);
    public abstract void UnEquipped(Sprite sprite, byte displaySlot);
    public virtual void OnDropped(Sprite sprite, Position droppedPosition, Area map) { }
    public virtual void OnPickedUp(Sprite sprite, Position pickedPosition, Area map) { }
}