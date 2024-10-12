using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class MonsterScript(Monster monster, Area map) : ObjectManager
{
    protected readonly Area Map = map;
    protected readonly Monster Monster = monster;

    public abstract void Update(TimeSpan elapsedTime);
    public abstract void OnClick(WorldClient client);
    public abstract void OnDeath(WorldClient client = null);
    public virtual void MonsterState(TimeSpan elapsedTime) { }
    public virtual void OnApproach(WorldClient client) { }
    public virtual bool OnGossip(WorldClient client) => false;
    public virtual bool OnDispelled(WorldClient client) => false;
    public virtual void OnSkulled(WorldClient client) { }
    public virtual void OnItemDropped(WorldClient client, Item item) { }
    public virtual void OnGoldDropped(WorldClient client, uint gold) { }

    public virtual void OnLeave(WorldClient client)
    {
        try
        {
            Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (player.dmg, player.player, false, false), player);
            if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) Monster.ClearTarget();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }

    public virtual void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);
            if (!tagged)
                Monster.TargetRecord.TaggedAislings.TryAdd(client.Aisling.Serial, (dmg, client.Aisling, true, false));
            else
                Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, (++dmg, player.player, true, player.blocked), player);
            Monster.Aggressive = true;
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }
}