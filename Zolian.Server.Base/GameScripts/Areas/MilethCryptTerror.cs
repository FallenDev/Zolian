using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Shrouded Crypt")]
public class MilethCryptTerror : AreaScript
{
    private Aisling _aisling;
    private WorldServerTimer AnimTimer { get; }
    private bool _animate;

    public MilethCryptTerror(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new WorldServerTimer(TimeSpan.FromMilliseconds(1 + 5000));
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_aisling == null) return;
        if (_aisling.Map.ID != 8)
            _animate = false;

        if (_animate)
            HandleMapAnimations(elapsedTime);
    }

    public override void OnMapEnter(WorldClient client)
    {
        _aisling = client.Aisling;
        _animate = true;
    }

    public override void OnMapExit(WorldClient client)
    {
        _aisling = null;
        _animate = false;
    }

    public override void OnMapClick(WorldClient client, int x, int y) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        if (newLocation.X == 1 && newLocation.Y == 19 && client.Aisling.QuestManager.CryptTerror && !client.Aisling.QuestManager.CryptTerrorSlayed)
        {
            if (client.Aisling.HasKilled("Crypt Terror", 1))
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, "You feel a sense of dread.");
                return;
            }

            client.TransitionToMap(3023, new Position(5, 2));
            client.SendServerMessage(ServerMessageType.OrangeBar1, "Something pulls you in.");
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);

        if (_aisling.Map.ID != 8) return;
        if (!a) return;
        _aisling?.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(214, new Position(1, 19)));
    }

    public override void OnGossip(WorldClient client, string message) { }
}