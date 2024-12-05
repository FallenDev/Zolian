using System.Collections.Concurrent;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Darkages.Types;
using System.Numerics;
using System.Security.Cryptography;
using Darkages.Network.Server;
using Darkages.Sprites.Entity;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Object;

namespace Darkages.Sprites;

public class Movable : Identifiable
{
    private readonly Lock _walkLock = new();

    /// <summary>
    /// Walking logic for NPCs
    /// </summary>
    private bool Walk()
    {
        void Step0C(int x, int y)
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
            else if (!Area.IsSpriteWithinBoundsWhileGhostWalking(this, PendingX, PendingY)) return false;

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
            else if (!Area.IsSpriteWithinBoundsWhileGhostWalking(this, PendingX, PendingY)) return false;

            // Commit Walk to other Player Clients
            Step0C(currentPosX, currentPosY);

            // Check Trap Activation
            if (this is Monster trapCheck)
                CheckTraps(trapCheck);

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

            try
            {
                if (Map.IsWall((int)pos.X, (int)pos.Y)) continue;
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

    /// <summary>
    /// Sends an Animation Server Packet to players nearby
    /// </summary>
    /// <returns>The sprite calling the animation</returns>
    public Movable SendAnimationNearby(ushort targetEffect, Position position = null, uint targetSerial = 0, ushort speed = 100, ushort casterEffect = 0, uint casterSerial = 0)
    {
        try
        {
            var selectedPlayers = new ConcurrentDictionary<long, Aisling>();
            AddPlayersToSelection(selectedPlayers, WithinRangeOf);
            SendToSelectedPlayers(selectedPlayers, c => c.SendAnimation(targetEffect, position, targetSerial, speed, casterEffect, casterSerial));
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with SendAnimationNearby called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with SendAnimationNearby called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }

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
        var selectedPlayers = new ConcurrentDictionary<long, Aisling>();

        try
        {
            switch (op)
            {
                // Player, Monster, Mundane Scope
                case PlayerScope.NearbyAislings:
                    {
                        AddPlayersToSelection(selectedPlayers, WithinRangeOf);
                    }
                    break;
                case PlayerScope.VeryNearbyAislings:
                    {
                        AddPlayersToSelection(selectedPlayers, player => WithinRangeOf(player, ServerSetup.Instance.Config.VeryNearByProximity));
                    }
                    break;
                case PlayerScope.AislingsOnSameMap:
                    {
                        AddPlayersToSelection(selectedPlayers, player => CurrentMapId == player.CurrentMapId);
                    }
                    break;
                case PlayerScope.DefinedAislings when definer == null:
                    return;
                case PlayerScope.DefinedAislings:
                    {
                        foreach (var player in definer)
                            selectedPlayers.TryAdd(player.Serial, player);
                    }
                    break;
                case PlayerScope.All:
                    {
                        foreach (var player in ServerSetup.Instance.Game.Aislings)
                            selectedPlayers.TryAdd(player.Serial, player);
                    }
                    break;
                // Player only Scope
                case PlayerScope.NearbyAislingsExludingSelf when this is Aisling:
                    {
                        AddPlayersToSelection(selectedPlayers, player => WithinRangeOf(player) && Serial != player.Serial);
                    }
                    break;
                case PlayerScope.GroupMembers when this is Aisling aisling:
                    {
                        AddPlayersToSelection(selectedPlayers, player => aisling.GroupParty.Has(player));
                    }
                    break;
                case PlayerScope.NearbyGroupMembersExcludingSelf when this is Aisling aisling:
                    {
                        AddPlayersToSelection(selectedPlayers, player => WithinRangeOf(player) && aisling.GroupParty.Has(player) && Serial != player.Serial);
                    }
                    break;
                case PlayerScope.NearbyGroupMembers when this is Aisling aisling:
                    {
                        AddPlayersToSelection(selectedPlayers, player => WithinRangeOf(player) && aisling.GroupParty.Has(player) && Serial != player.Serial);
                    }
                    break;
                case PlayerScope.Clan when this is Aisling aisling:
                    {
                        AddPlayersToSelection(selectedPlayers, player => !string.IsNullOrEmpty(player.Clan) && string.Equals(player.Clan, aisling.Clan, StringComparison.CurrentCultureIgnoreCase) && Serial != player.Serial);
                    }
                    break;
                case PlayerScope.Self when this is Aisling aisling:
                    method(aisling.Client);
                    return;
            }

            SendToSelectedPlayers(selectedPlayers, method);
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {method.Method.Name} within SendTargetedClientMethod called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {method.Method.Name} within SendTargetedClientMethod called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }

    private void AddPlayersToSelection(ConcurrentDictionary<long, Aisling> selectedPlayers, Func<Aisling, bool> filter)
    {
        var objs = ObjectManager.GetObjects<Aisling>(Map, player => player != null && filter(player));
        foreach (var (serial, player) in objs)
        {
            selectedPlayers.TryAdd(serial, player);
        }
    }

    private static void SendToSelectedPlayers(ConcurrentDictionary<long, Aisling> players, Action<WorldClient> method)
    {
        try
        {
            foreach (var (serial, player) in players)
            {
                if (player?.Client == null) continue;
                if ((method.Method.Name.Contains("SendArmorBodyAnimationNearby") || method.Method.Name.Contains("SendAnimationNearby")) 
                    && !player.GameSettings.Animations) continue;
                method(player.Client);
            }
        }
        catch
        {
            ServerSetup.EventsLogger($"Issue with {method.Method.Name} within SendToSelectedPlayers called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}");
            SentrySdk.CaptureMessage($"Issue with {method.Method.Name} within SendToSelectedPlayers called from {new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod()?.Name ?? "Unknown"}", SentryLevel.Error);
        }
    }
}