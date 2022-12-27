using System.Numerics;
using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Areas;

[Script("Shrouded Crypt")]
public class MilethCryptTerror : AreaScript
{
    private Sprite _aisling;
    private GameServerTimer AnimTimer { get; }
    private bool _animate;

    public MilethCryptTerror(Area area) : base(area)
    {
        Area = area;
        AnimTimer = new GameServerTimer(TimeSpan.FromMilliseconds(1 + 5000));
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (_aisling == null) return;
        if (_aisling.Map.ID != 8)
            _animate = false;

        if (_animate)
            HandleMapAnimations(elapsedTime);
    }

    public override void OnMapEnter(GameClient client)
    {
        _aisling = client.Aisling;
        _animate = true;
    }

    public override void OnMapExit(GameClient client)
    {
        _aisling = null;
        _animate = false;
    }

    public override void OnMapClick(GameClient client, int x, int y) { }

    public override void OnPlayerWalk(GameClient client, Position oldLocation, Position newLocation)
    {
        if (newLocation.X == 1 && newLocation.Y == 19 && client.Aisling.QuestManager.CryptTerror && !client.Aisling.QuestManager.CryptTerrorSlayed)
        {
            if (client.Aisling.HasKilled("Crypt Terror", 1))
            {
                client.SendMessage(0x02, "You feel a sense of dread.");
                return;
            }

            client.TransitionToMap(3023, new Position(5, 2));
            client.SendMessage(0x02, "Something pulls you in.");
        }
    }

    public override void OnItemDropped(GameClient client, Item itemDropped, Position locationDropped) { }

    private void HandleMapAnimations(TimeSpan elapsedTime)
    {
        var a = AnimTimer.Update(elapsedTime);

        if (_aisling.Map.ID != 8) return;
        if (!a) return;
        _aisling?.Show(Scope.NearbyAislings, new ServerFormat29(214, new Vector2(1, 19)));
    }

    public override void OnGossip(GameClient client, string message) { }
}