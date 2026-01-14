using System.Numerics;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Piet;

[Script("Evermore HallA")]
public class EvermoreHallsA : AreaScript
{
    private readonly Vector2 _poleTrap1;
    private readonly Vector2 _poleTrap2;
    private readonly Vector2 _poleTrap3;
    private readonly Vector2 _poleTrap4;
    private readonly Vector2 _poleTrap5;
    private readonly Vector2 _poleTrap6;
    private readonly Vector2 _poleTrap7;
    private readonly Vector2 _poleTrap8;
    private readonly Vector2 _poleTrap9;
    private readonly Vector2 _poleTrap10;
    private readonly Vector2 _spikeTrap1;
    private readonly Vector2 _spikeTrap2;
    private readonly Vector2 _spikeTrap3;
    private readonly Vector2 _spikeTrap4;
    private readonly Vector2 _spikeTrap5;
    private readonly Vector2 _spikeTrap6;
    private readonly Vector2 _spikeTrap7;
    private readonly Vector2 _spikeTrap8;
    private readonly Vector2 _spikeTrap9;
    private readonly Vector2 _spikeTrap10;


    public EvermoreHallsA(Area area) : base(area)
    {
        Area = area;
        _poleTrap1 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap2 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap3 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap4 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap5 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap6 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap7 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap8 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap9 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _poleTrap10 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap1 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap2 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap3 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap4 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap5 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap6 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap7 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap8 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap9 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
        _spikeTrap10 = new Vector2(Generator.RandNumGen10(), Generator.RandNumGen20());
    }

    public override void Update(TimeSpan elapsedTime) { }
    public override void OnMapEnter(WorldClient client) { }
    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        if (vectorMap == _poleTrap1 || vectorMap == _poleTrap2 || vectorMap == _poleTrap3 ||
            vectorMap == _poleTrap4 || vectorMap == _poleTrap5 || vectorMap == _poleTrap6 || 
            vectorMap == _poleTrap7 || vectorMap == _poleTrap8 || vectorMap == _poleTrap9 || 
            vectorMap == _poleTrap10)
        {
            OnPoleTrap(client);
        }

        if (vectorMap == _spikeTrap1 || vectorMap == _spikeTrap2 || vectorMap == _spikeTrap3 ||
            vectorMap == _spikeTrap4 || vectorMap == _spikeTrap5 || vectorMap == _spikeTrap6 || 
            vectorMap == _spikeTrap7 || vectorMap == _spikeTrap8 || vectorMap == _spikeTrap9 || 
            vectorMap == _spikeTrap10)
        {
            OnSpikeTrap(client);
        }
    }

    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }

    private static void OnPoleTrap(WorldClient client)
    {
        client.Aisling.ApplyTrapDamage(client.Aisling, 500000);
        client.Aisling.SendAnimationNearby(140, client.Aisling.Position);
        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(59, false));
    }

    private static void OnSpikeTrap(WorldClient client)
    {
        client.Aisling.ApplyTrapDamage(client.Aisling, 750000);
        client.Aisling.SendAnimationNearby(112, client.Aisling.Position);
        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendSound(68, false));
    }
}