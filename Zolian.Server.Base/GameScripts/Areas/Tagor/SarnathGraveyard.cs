using Darkages.Common;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;
using System.Numerics;

namespace Darkages.GameScripts.Areas.Tagor;

[Script("Sarnath Graveyard")]
public class SarnathGraveyard : AreaScript
{
    private Debuff _debuff1;
    private Debuff _debuff2;

    public SarnathGraveyard(Area area) : base(area) => Area = area;
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client) { }

    public override void OnMapExit(WorldClient client) { }

    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation)
    {
        var vectorMap = new Vector2(newLocation.X, newLocation.Y);
        if (client.Aisling.Pos != vectorMap) return;

        if (ReflexCheck(client.Aisling)) return;
        _debuff1 = new DebuffArdPoison();
        _debuff2 = new DebuffDecay();

        PoisonPool1(client, vectorMap);
        PoisonPool2(client, vectorMap);
        PoisonPool3(client, vectorMap);
        PoisonPool4(client, vectorMap);
        PoisonPool5(client, vectorMap);
        PoisonPool6(client, vectorMap);
        PoisonPool7(client, vectorMap);
        PoisonPool8(client, vectorMap);
        PoisonPool9(client, vectorMap);
        PoisonPool10(client, vectorMap);
        PoisonPool11(client, vectorMap);
        PoisonPool12(client, vectorMap);
        PoisonPool13(client, vectorMap);
    }

    private void PoisonPool1(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(33, 46) &&
            location != new Vector2(34, 46) &&
            location != new Vector2(35, 46) &&
            location != new Vector2(34, 47) &&
            location != new Vector2(34, 45)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool2(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(35, 37) &&
            location != new Vector2(36, 37) &&
            location != new Vector2(37, 37)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool3(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(45, 36) &&
            location != new Vector2(46, 36) &&
            location != new Vector2(47, 36) &&
            location != new Vector2(46, 37) &&
            location != new Vector2(46, 35)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool4(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(39, 31) &&
            location != new Vector2(40, 31) &&
            location != new Vector2(41, 31) &&
            location != new Vector2(40, 32) &&
            location != new Vector2(40, 30)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool5(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(46, 21) &&
            location != new Vector2(47, 21) &&
            location != new Vector2(48, 21) &&
            location != new Vector2(47, 22) &&
            location != new Vector2(47, 20)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool6(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(17, 36) &&
            location != new Vector2(18, 36) &&
            location != new Vector2(19, 36) &&
            location != new Vector2(18, 37) &&
            location != new Vector2(18, 35)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool7(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(7, 33) &&
            location != new Vector2(8, 33) &&
            location != new Vector2(9, 33) &&
            location != new Vector2(8, 34) &&
            location != new Vector2(8, 32)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool8(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(5, 44) &&
            location != new Vector2(6, 44) &&
            location != new Vector2(7, 44) &&
            location != new Vector2(6, 45) &&
            location != new Vector2(6, 43)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool9(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(24, 27) &&
            location != new Vector2(25, 27) &&
            location != new Vector2(26, 27) &&
            location != new Vector2(25, 28) &&
            location != new Vector2(25, 26)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool10(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(37, 12) &&
            location != new Vector2(38, 12) &&
            location != new Vector2(39, 12) &&
            location != new Vector2(38, 13) &&
            location != new Vector2(38, 11)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool11(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(43, 3) &&
            location != new Vector2(44, 3) &&
            location != new Vector2(45, 3) &&
            location != new Vector2(44, 4) &&
            location != new Vector2(44, 2)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool12(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(17, 3) &&
            location != new Vector2(18, 3) &&
            location != new Vector2(19, 3) &&
            location != new Vector2(18, 4) &&
            location != new Vector2(18, 2)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private void PoisonPool13(WorldClient client, Vector2 location)
    {
        if (location != new Vector2(10, 12) &&
            location != new Vector2(11, 12) &&
            location != new Vector2(12, 12) &&
            location != new Vector2(11, 13) &&
            location != new Vector2(11, 11)) return;

        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff1);
        client.EnqueueDebuffAppliedEvent(client.Aisling, _debuff2);
        foreach (var buff in client.Aisling.Buffs.Values)
        {
            buff?.OnEnded(client.Aisling, buff);
        }

        client.Aisling.SendAnimationNearby(75, new Position(location));
    }

    private static bool ReflexCheck(Aisling aisling)
    {
        var check = Generator.RandNumGen100();
        return !(check > aisling.Reflex);
    }
}