using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Numerics;
using Darkages.Enums;
using Darkages.GameScripts.Affects;

namespace Darkages.GameScripts.Areas;

[Script("Necro Courtyard")]
public class NecroCourtyard : AreaScript
{
    private Aisling _aisling;
    private Debuff _debuff1;
    private Debuff _debuff2;


    public NecroCourtyard(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) => _aisling = client.Aisling;

    public override void OnMapClick(WorldClient client, int x, int y) => _aisling ??= client.Aisling;

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (_aisling.Pos != vectorMap) return;
        _debuff1 = new DebuffArdPoison();
        _debuff2 = new DebuffDecay();

        // Poison Pool 1
        if (vectorMap == new Vector2(15, 6) ||
            vectorMap == new Vector2(16, 6) ||
            vectorMap == new Vector2(17, 6) ||
            vectorMap == new Vector2(16, 7) ||
            vectorMap == new Vector2(16, 5))
        {
            _debuff1.OnApplied(_aisling, _debuff1);
            _debuff2.OnApplied(_aisling, _debuff2);
            foreach (var buff in _aisling.Buffs.Values)
            {
                buff?.OnEnded(_aisling, buff);
            }

            _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(vectorMap)));
        }

        // Poison Pool 2
        if (vectorMap != new Vector2(21, 22) &&
            vectorMap != new Vector2(22, 22) &&
            vectorMap != new Vector2(23, 22) &&
            vectorMap != new Vector2(22, 23) &&
            vectorMap != new Vector2(22, 21)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(vectorMap)));
    }
}