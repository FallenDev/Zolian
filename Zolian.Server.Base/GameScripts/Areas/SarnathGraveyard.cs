using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Numerics;
using Darkages.Enums;
using Darkages.GameScripts.Affects;

namespace Darkages.GameScripts.Areas;

[Script("Sarnath Graveyard")]
public class SarnathGraveyard : AreaScript
{
    private Aisling _aisling;
    private Debuff _debuff1;
    private Debuff _debuff2;


    public SarnathGraveyard(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) => _aisling = client.Aisling;

    public override void OnMapClick(WorldClient client, int x, int y) => _aisling ??= client.Aisling;

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (_aisling.Pos != vectorMap) return;
        _debuff1 = new DebuffArdPoison();
        _debuff2 = new DebuffDecay();

        PoisonPool1(vectorMap);
        PoisonPool2(vectorMap);
        PoisonPool3(vectorMap);
        PoisonPool4(vectorMap);
        PoisonPool5(vectorMap);
        PoisonPool6(vectorMap);
        PoisonPool7(vectorMap);
        PoisonPool8(vectorMap);
        PoisonPool9(vectorMap);
        PoisonPool10(vectorMap);
        PoisonPool11(vectorMap);
        PoisonPool12(vectorMap);
        PoisonPool13(vectorMap);

    }

    private void PoisonPool1(Vector2 location)
    {
        if (location != new Vector2(33, 46) &&
            location != new Vector2(34, 46) &&
            location != new Vector2(35, 46) &&
            location != new Vector2(34, 47) &&
            location != new Vector2(34, 45)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool2(Vector2 location)
    {
        if (location != new Vector2(35, 37) &&
            location != new Vector2(36, 37) &&
            location != new Vector2(37, 37)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool3(Vector2 location)
    {
        if (location != new Vector2(45, 36) &&
            location != new Vector2(46, 36) &&
            location != new Vector2(47, 36) &&
            location != new Vector2(46, 37) &&
            location != new Vector2(46, 35)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool4(Vector2 location)
    {
        if (location != new Vector2(39, 31) &&
            location != new Vector2(40, 31) &&
            location != new Vector2(41, 31) &&
            location != new Vector2(40, 32) &&
            location != new Vector2(40, 30)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool5(Vector2 location)
    {
        if (location != new Vector2(46, 21) &&
            location != new Vector2(47, 21) &&
            location != new Vector2(48, 21) &&
            location != new Vector2(47, 22) &&
            location != new Vector2(47, 20)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool6(Vector2 location)
    {
        if (location != new Vector2(17, 36) &&
            location != new Vector2(18, 36) &&
            location != new Vector2(19, 36) &&
            location != new Vector2(18, 37) &&
            location != new Vector2(18, 35)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool7(Vector2 location)
    {
        if (location != new Vector2(7, 33) &&
            location != new Vector2(8, 33) &&
            location != new Vector2(9, 33) &&
            location != new Vector2(8, 34) &&
            location != new Vector2(8, 32)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool8(Vector2 location)
    {
        if (location != new Vector2(5, 44) &&
            location != new Vector2(6, 44) &&
            location != new Vector2(7, 44) &&
            location != new Vector2(6, 45) &&
            location != new Vector2(6, 43)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool9(Vector2 location)
    {
        if (location != new Vector2(24, 27) &&
            location != new Vector2(25, 27) &&
            location != new Vector2(26, 27) &&
            location != new Vector2(25, 28) &&
            location != new Vector2(25, 26)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool10(Vector2 location)
    {
        if (location != new Vector2(37, 12) &&
            location != new Vector2(38, 12) &&
            location != new Vector2(39, 12) &&
            location != new Vector2(38, 13) &&
            location != new Vector2(38, 11)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool11(Vector2 location)
    {
        if (location != new Vector2(43, 3) &&
            location != new Vector2(44, 3) &&
            location != new Vector2(45, 3) &&
            location != new Vector2(44, 4) &&
            location != new Vector2(44, 2)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool12(Vector2 location)
    {
        if (location != new Vector2(17, 3) &&
            location != new Vector2(18, 3) &&
            location != new Vector2(19, 3) &&
            location != new Vector2(18, 4) &&
            location != new Vector2(18, 2)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }

    private void PoisonPool13(Vector2 location)
    {
        if (location != new Vector2(10, 12) &&
            location != new Vector2(11, 12) &&
            location != new Vector2(12, 12) &&
            location != new Vector2(11, 13) &&
            location != new Vector2(11, 11)) return;

        _debuff1.OnApplied(_aisling, _debuff1);
        _debuff2.OnApplied(_aisling, _debuff2);
        foreach (var buff in _aisling.Buffs.Values)
        {
            buff?.OnEnded(_aisling, buff);
        }

        _aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, new Position(location)));
    }
}