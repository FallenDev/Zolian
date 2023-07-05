using Darkages.Infrastructure;
using Darkages.Network.Server;
using Darkages.Sprites;

namespace Darkages.Network.Components;

public class MundaneComponent : GameServerComponent
{
    public MundaneComponent(WorldServer server) : base(server)
    {
        Timer = new GameServerTimer(TimeSpan.FromSeconds(ServerSetup.Instance.Config.MundaneRespawnInterval));
    }

    private GameServerTimer Timer { get; }

    private void SpawnMundanes()
    {
        foreach (var mundane in from mundane in ServerSetup.Instance.GlobalMundaneTemplateCache
                                where mundane.Value != null
                     && mundane.Value.AreaID != 0
                                where ServerSetup.Instance.GlobalMapCache.ContainsKey(mundane.Value.AreaID)
                                let map = ServerSetup.Instance.GlobalMapCache[mundane.Value.AreaID]
                                where map is { Ready: true }
                                let npc = Server.ObjectHandlers.GetObject<Mundane>(map, i => i.CurrentMapId == map.ID
                     && i.Template != null
                     && i.Template.Name ==
                     mundane.Value.Name)
                                where npc is not { CurrentHp: > 0 }
                                select mundane)
        {
            Mundane.Create(mundane.Value);
        }
    }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (Timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(SpawnMundanes);
    }
}