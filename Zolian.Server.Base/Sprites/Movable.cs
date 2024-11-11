using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Darkages.Types;
using System.Numerics;
using System.Security.Cryptography;
using Darkages.Network.Server;
using Darkages.Sprites.Entity;

namespace Darkages.Sprites;

public class Movable : Identifiable
{
    private readonly object _walkLock = new();

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

            var allowGhostWalk = this is Aisling { GameMaster: true };

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
            else if (Area.IsSpriteWithinBoundsWhileGhostWalking(this, PendingX, PendingY)) return false;

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
            else if (Area.IsSpriteWithinBoundsWhileGhostWalking(this, PendingX, PendingY)) return false;

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
}