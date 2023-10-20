﻿using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class MonsterScript(Monster monster, Area map) : ObjectManager, IScriptBase
{
    protected readonly Area Map = map;
    protected readonly Monster Monster = monster;

    public abstract void Update(TimeSpan elapsedTime);
    public abstract void OnClick(WorldClient client);
    public abstract void OnDeath(WorldClient client = null);

    public virtual void MonsterState(TimeSpan elapsedTime) { }
    public virtual void OnApproach(WorldClient client) { }
    public virtual void OnLeave(WorldClient client) { }
    public virtual bool OnGossip(WorldClient client) => false;
    public virtual bool OnDispelled(WorldClient client) => false;
    public virtual void OnSkulled(WorldClient client) { }
    public virtual void OnDamaged(WorldClient client, long dmg, Sprite source) { }
    public virtual void OnItemDropped(WorldClient client, Item item) { }
    public virtual void OnGoldDropped(WorldClient client, uint gold) { }
}