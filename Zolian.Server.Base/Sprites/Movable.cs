using System.Numerics;
using System.Security.Cryptography;

using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Sprites;

public class Movable : Identifiable
{
    private readonly Lock _walkLock = new();

    /// <summary>
    /// Walking logic for NPCs
    /// </summary>
    private bool Walk()
    {
        lock (_walkLock)
        {
            var currentPosX = X;
            var currentPosY = Y;

            PendingX = X;
            PendingY = Y;

            var allowGhostWalk = false;

            if (this is Monster { Template: not null } monster)
            {
                allowGhostWalk = monster.Template.IgnoreCollision;
                if (monster.ThrownBack) return false;
            }

            if (this is Mundane)
                allowGhostWalk = false;

            // Check position before we add direction, add direction, check position to see if we can commit
            if (!allowGhostWalk)
            {
                if (Map.IsWall(currentPosX, currentPosY)) return false;
                if (Area.IsSpriteInLocationOnWalk(this, PendingX, PendingY)) return false;
            }
            else if (!Map.IsSpriteWithinBounds(PendingX, PendingY)) return false;

            switch (Direction)
            {
                case 0:
                    PendingY--;
                    break;
                case 1:
                    PendingX++;
                    break;
                case 2:
                    PendingY++;
                    break;
                case 3:
                    PendingX--;
                    break;
            }

            if (!allowGhostWalk)
            {
                if (Map.IsWall(PendingX, PendingY)) return false;
                if (Area.IsSpriteInLocationOnWalk(this, PendingX, PendingY)) return false;
            }
            else if (!Map.IsSpriteWithinBounds(PendingX, PendingY)) return false;

            // Commit Walk to other Player Clients
            Step0COnWalk(currentPosX, currentPosY);

            // Check Trap Activation & Run Map OnWalk Script
            if (this is Monster trapCheck)
            {
                CheckTraps(trapCheck);
                trapCheck.Map?.Script?.Item2.OnNpcWalk(trapCheck);
            }

            // Reset our PendingX & PendingY
            PendingX = currentPosX;
            PendingY = currentPosY;

            return true;
        }
    }

    public bool WalkTo(int x, int y)
    {
        var buffer = new byte[2];
        var length = float.PositiveInfinity;
        var offset = 0;

        for (byte i = 0; i < 4; i++)
        {
            var newX = (int)Pos.X + Directions[i][0];
            var newY = (int)Pos.Y + Directions[i][1];
            var pos = new Vector2(newX, newY);

            if (this is Monster { AStar: false })
            {
                if ((int)pos.X == x && (int)pos.Y == y) return false;
            }

            var ghostWalk = false;

            if (this is Monster monster)
                ghostWalk = monster.Template.IgnoreCollision;

            try
            {
                if (!ghostWalk && Map.IsWall((int)pos.X, (int)pos.Y)) continue;
                if (Area.IsSpriteInLocationOnWalk(this, (int)pos.X, (int)pos.Y)) continue;
            }
            catch (Exception ex)
            {
                ServerSetup.EventsLogger($"{ex}\nUnknown exception in WalkTo method.");
                SentrySdk.CaptureException(ex);
            }

            var xDist = x - (int)pos.X;
            var yDist = y - (int)pos.Y;

            // Chebyshev Distance
            TargetDistance = Math.Max(Math.Abs(xDist), Math.Abs(yDist));

            if (length < TargetDistance) continue;

            if (length > TargetDistance)
            {
                length = (float)TargetDistance;
                offset = 0;
            }

            if (offset < buffer.Length)
                buffer[offset] = i;

            offset++;
        }

        if (offset == 0) return false;
        var r = Random.Shared.Next(0, offset) % buffer.Length;
        if (r < 0 || buffer.Length <= r) return Walk();
        var pendingDirection = buffer[r];
        Direction = pendingDirection;

        return Walk();
    }

    public void Wander()
    {
        if (!CanUpdate()) return;

        var savedDirection = Direction;
        var update = false;

        Direction = (byte)RandomNumberGenerator.GetInt32(5);
        if (Direction != savedDirection) update = true;

        if (Walk() || !update) return;

        foreach (var player in AislingsNearby())
        {
            player?.Client.SendCreatureTurn(Serial, (Direction)Direction);
        }

        LastTurnUpdated = DateTime.UtcNow;
    }

    private void CheckTraps(Monster monster)
    {
        foreach (var trap in ServerSetup.Instance.Traps.Values.Where(t => t.TrapItem.Map.ID == monster.Map.ID))
        {
            if (trap.Owner == null || trap.Owner.Serial == monster.Serial ||
                monster.X != trap.Location.X || monster.Y != trap.Location.Y) continue;

            var triggered = Trap.Activate(trap, monster);
            if (!triggered) continue;
            ServerSetup.Instance.Traps.TryRemove(trap.Serial, out _);
            break;
        }
    }

    public void Turn()
    {
        if (!CanUpdate()) return;

        foreach (var player in AislingsNearby())
        {
            player?.Client.SendCreatureTurn(Serial, (Direction)Direction);
        }

        LastTurnUpdated = DateTime.UtcNow;
    }

    public async Task<bool> StepAndRemove(Sprite sprite, int savedXStep, int savedYStep)
    {
        try
        {
            if (sprite is Aisling movingPlayer)
            {
                var warpPos = new Position(savedXStep, savedYStep);
                movingPlayer.Client.WarpTo(warpPos);
                movingPlayer.Client.CheckWarpTransitions(movingPlayer.Client, savedXStep, savedYStep);
                return await movingPlayer.Client.SendRemoveObject(movingPlayer.Serial);
            }

            if (sprite is not Monster movingMonster) return await Task.FromResult(false);

            return await StepAddAndRemoveObjectOnMovementAbilities(savedXStep, savedYStep);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with Step called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with Step called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
            return await Task.FromResult(false);
        }
    }

    private void Step0COnWalk(int x, int y)
    {
        lock (_walkLock)
        {
            var readyTime = DateTime.UtcNow;
            Pos = new Vector2(PendingX, PendingY);

            foreach (var player in AislingsNearby())
            {
                player?.Client.SendCreatureWalk(Serial, new Point(x, y), (Direction)Direction);
            }

            LastMovementChanged = readyTime;
            LastPosition = new Position(x, y);
        }
    }

    private Task<bool> StepAddAndRemoveObjectOnMovementAbilities(int x, int y)
    {
        var readyTime = DateTime.UtcNow;
        Pos = new Vector2(x, y);
        LastMovementChanged = readyTime;
        LastPosition = new Position(x, y);
        PendingX = x;
        PendingY = y;
        UpdateAddAndRemove();
        return Task.FromResult(true);
    }

    public void StepAddAndUpdateDisplay(Sprite sprite)
    {
        sprite.LastTurnUpdated = DateTime.UtcNow;
        if (sprite is not Aisling movingPlayer) return;
        movingPlayer.Client.UpdateDisplay();
        movingPlayer.Client.LastMovement = DateTime.UtcNow;
    }

    /// <summary>
    /// Sends an Animation Server Packet to players nearby
    /// </summary>
    /// <returns>The sprite calling the animation</returns>
    public Movable SendAnimationNearby(ushort targetEffect, Position position = null, uint targetSerial = 0, ushort speed = 100, ushort casterEffect = 0, uint casterSerial = 0)
    {
        try
        {
            SendToAislings(WithinRangeOf, c => c.SendAnimation(targetEffect, position, targetSerial, speed, casterEffect, casterSerial), filterAnimations: true);
        }
        catch { }

        return this;
    }

    /// <summary>
    /// Sends a ServerFormat to target players using a scope or definer. Uses client of the players returned to the Action
    /// </summary>
    /// <param name="op">Scope of the method call</param>
    /// <param name="method">IWorldClient method to send</param>
    /// <param name="definer">Specific users, Scope must also be "DefinedAislings"</param>
    public void SendTargetedClientMethod(PlayerScope op, Action<WorldClient> method, IEnumerable<Aisling> definer = null)
    {
        var filterAnimations = method.Method.Name.Contains("SendArmorBodyAnimationNearby") || method.Method.Name.Contains("SendAnimationNearby");

        try
        {
            switch (op)
            {
                // Player, Monster, Mundane Scope
                case PlayerScope.NearbyAislings:
                    SendToAislings(WithinRangeOf, method, filterAnimations);
                    return;
                case PlayerScope.VeryNearbyAislings:
                    SendToAislings(player => WithinRangeOf(player, ServerSetup.Instance.Config.VeryNearByProximity), method, filterAnimations);
                    return;
                case PlayerScope.AislingsOnSameMap:
                    SendToAislings(player => CurrentMapId == player.CurrentMapId, method, filterAnimations);
                    return;
                case PlayerScope.DefinedAislings when definer == null:
                    return;
                case PlayerScope.DefinedAislings:
                    SendToDefinedAislings(definer, method, filterAnimations);
                    return;
                case PlayerScope.All:
                    SendToAllLoggedInAislings(method, filterAnimations);
                    return;
                // Player only Scope
                case PlayerScope.NearbyAislingsExludingSelf when this is Aisling:
                    SendToAislings(player => WithinRangeOf(player) && Serial != player.Serial, method, filterAnimations);
                    return;
                case PlayerScope.GroupMembers when this is Aisling aisling:
                    SendToAislings(player => aisling.GroupParty.Has(player), method, filterAnimations);
                    return;
                case PlayerScope.NearbyGroupMembersExcludingSelf when this is Aisling aisling:
                    SendToAislings(player => WithinRangeOf(player) && aisling.GroupParty.Has(player) && Serial != player.Serial, method, filterAnimations);
                    return;
                case PlayerScope.NearbyGroupMembers when this is Aisling aisling:
                    SendToAislings(player => WithinRangeOf(player) && aisling.GroupParty.Has(player) && Serial != player.Serial, method, filterAnimations);
                    return;
                case PlayerScope.Clan when this is Aisling aisling:
                    SendToAislings(player => !string.IsNullOrEmpty(player.Clan) && string.Equals(player.Clan, aisling.Clan, StringComparison.CurrentCultureIgnoreCase) && Serial != player.Serial, method, filterAnimations);
                    return;
                case PlayerScope.Self when this is Aisling aisling:
                    method(aisling.Client);
                    return;
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {method.Method.Name} within SendTargetedClientMethod called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {method.Method.Name} within SendTargetedClientMethod called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private void SendToAislings(Predicate<Aisling> predicate, Action<WorldClient> method, bool filterAnimations)
    {
        ObjectManager.ForEachObject(Map, predicate, player =>
        {
            SendToPlayer(player, method, filterAnimations);
        });
    }

    private static void SendToDefinedAislings(IEnumerable<Aisling> players, Action<WorldClient> method, bool filterAnimations)
    {
        foreach (var player in players)
            SendToPlayer(player, method, filterAnimations);
    }

    private static void SendToAllLoggedInAislings(Action<WorldClient> method, bool filterAnimations)
    {
        ServerSetup.Instance.Game.ForEachLoggedInAisling(player => SendToPlayer(player, method, filterAnimations));
    }

    private static void SendToPlayer(Aisling player, Action<WorldClient> method, bool filterAnimations)
    {
        try
        {
            if (player?.Client == null) return;
            if (filterAnimations && !player.GameSettings.Animations) return;
            method(player.Client);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {method.Method.Name} within SendToPlayer called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {method.Method.Name} within SendToPlayer called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}