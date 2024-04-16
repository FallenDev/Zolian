using System.Diagnostics;
using Chaos.Common.Definitions;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Mundanes.CthonicRemains;

[Script("Cthonic Guard")]
public class CthonicGuard(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnResponse(WorldClient client, ushort responseID, string args) { }

    public override void OnGossip(WorldClient client, string message) { }

    public override void OnApproach(WorldClient client)
    {
        if (Mundane.GuardModeActivated) return;
        Task.Run(ActivateGuardMode);
    }

    private void ActivateGuardMode()
    {
        Mundane.GuardModeActivated = true;
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        while (Mundane.GuardModeActivated)
        {
            if (!(stopWatch.Elapsed.TotalMilliseconds >= 2000)) continue;
            stopWatch.Restart();
            var targets = Mundane.MonstersNearby().ToList();
            if (targets.Count <= 0) continue;

            foreach (var target in targets)
            {
                target.Target = Mundane;
                Mundane.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, Mundane.Position));
                Mundane.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(72, target.Position));
                Mundane.PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: Kahflooshka!"));
                target.Remove();
                ServerSetup.Instance.GlobalMonsterCache.TryRemove(target.Serial, out _);
                DelObject(target);
            }
        }
    }
}