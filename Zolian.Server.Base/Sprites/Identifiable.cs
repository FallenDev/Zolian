using Darkages.Common;
using Darkages.Enums;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Sprites;

public class Identifiable : Sprite
{
    private static int[][] DirectionTable { get; } =
    [
        [-1, +3, -1],
        [+0, -1, +2],
        [-1, +1, -1]
    ];

    public void ShowTo(Aisling nearbyAisling)
    {
        if (nearbyAisling == null) return;
        if (this is Aisling aisling)
        {
            nearbyAisling.Client.SendDisplayAisling(aisling);
            aisling.SpritesInView.AddOrUpdate(nearbyAisling.Serial, nearbyAisling, (_, _) => nearbyAisling);
        }
        else
        {
            var sprite = new List<Sprite> { this };
            nearbyAisling.Client.SendVisibleEntities(sprite);
        }
    }

    public List<Sprite> GetInFrontToSide(int tileCount = 1)
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            case 0:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 1:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;

            case 2:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 3:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;
        }

        return results;
    }

    public List<Sprite> MonsterGetInFrontToSide(int tileCount = 1)
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            case 0:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 1:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;

            case 2:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 3:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;
        }

        return results;
    }

    public List<Sprite> MonsterGetFiveByFourRectInFront()
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            // North
            case 0:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 1));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 3));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 4));
                break;
            // East
            case 1:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y - 2));
                break;
            // South
            case 2:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 1));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 3));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 4));
                break;
            // West
            case 3:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y - 2));
                break;
        }

        return results;
    }

    public List<Sprite> GetHorizontalInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            case 0:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                break;

            case 1:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                break;

            case 2:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                break;

            case 3:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                break;
        }

        return results;
    }

    public Position GetFromAllSidesEmpty(Sprite target, int tileCount = 1)
    {
        var empty = Position;
        var blocks = target.Position.SurroundingContent(Map);

        if (blocks.Length <= 0) return empty;

        var selections = blocks.Where(i => i.Content is TileContent.None or TileContent.Item or TileContent.Money);
        var selection = selections.MaxBy(i => i.Position.DistanceFrom(Position));

        if (selection != null)
            empty = selection.Position;

        return empty;
    }

    public List<Sprite> GetAllInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(GetSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(GetSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(GetSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(GetSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> DamageableGetInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Position> GetTilesInFront(int tileCount = 1)
    {
        var results = new List<Position>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.Add(new Position((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.Add(new Position((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.Add(new Position((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.Add(new Position((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> DamageableGetAwayInFront(int tileCount = 2)
    {
        var results = new List<Sprite>();

        for (var i = 2; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> DamageableGetBehind(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 1:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 3:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> MonsterGetInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public Position GetPendingChargePosition(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0)
                pendingY--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1)
                pendingX++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2)
                pendingY++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3)
                pendingX--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX++;
            break;
        }

        return new Position(pendingX, pendingY);
    }

    public Position GetPendingChargePositionNoTarget(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0)
                pendingY--;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1)
                pendingX++;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2)
                pendingY++;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3)
                pendingX--;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX++;
            break;
        }

        return new Position(pendingX, pendingY);
    }

    public Position GetPendingThrowPosition(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0)
                pendingY++;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingY--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1)
                pendingX--;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingX++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2)
                pendingY--;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingY++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3)
                pendingX++;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingX--;
            break;
        }

        return new Position(pendingX, pendingY);
    }
    public bool GetPendingThrowIsWall(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0) pendingY++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1) pendingX--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2) pendingY--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3) pendingX++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }

        return false;
    }

    public bool NextTo(int x, int y)
    {
        var xDist = Math.Abs(x - X);
        var yDist = Math.Abs(y - Y);

        return xDist + yDist == 1;
    }

    public bool Facing(int x, int y, out int direction)
    {
        var xDist = (x - (int)Pos.X).IntClamp(-1, +1);
        var yDist = (y - (int)Pos.Y).IntClamp(-1, +1);

        direction = DirectionTable[xDist + 1][yDist + 1];
        return Direction == direction;
    }

    public bool FacingFarAway(int x, int y, out int direction)
    {
        var orgPos = Pos; // Sprites current position
        var xDiff = orgPos.X - x; // Difference between points
        var yDiff = orgPos.Y - y; // Difference between points
        var xGap = Math.Abs(xDiff); // Absolute value of point
        var yGap = Math.Abs(yDiff); // Absolute value of point

        // Determine which point has a greater distance
        if (xGap > yGap)
            switch (xDiff)
            {
                case <= -1: // East
                    direction = 1;
                    return Direction == direction;
                case >= 0: // West
                    direction = 3;
                    return Direction == direction;
            }

        switch (yDiff)
        {
            case <= -1: // South
                direction = 2;
                return Direction == direction;
            case >= 0: // North
                direction = 0;
                return Direction == direction;
        }

        direction = 0;
        return Direction == direction;
    }

    public void UpdateAddAndRemove()
    {
        foreach (var playerNearby in AislingsEarShotNearby())
        {
            uint objectId;

            if (this is Item item)
                objectId = item.ItemVisibilityId;
            else
                objectId = Serial;

            playerNearby.Client.SendRemoveObject(objectId);
            var obj = new List<Sprite> { this };
            playerNearby.Client.SendVisibleEntities(obj);
        }
    }

    public void Remove()
    {
        var nearby = AislingsEarShotNearby();
        uint objectId;

        if (this is Item item)
            objectId = item.ItemVisibilityId;
        else
            objectId = Serial;

        foreach (var o in nearby)
            o?.Client?.SendRemoveObject(objectId);

        DeleteObject();
    }

    public void HideFrom(Aisling nearbyAisling)
    {
        uint objectId;

        if (this is Item item)
            objectId = item.ItemVisibilityId;
        else
            objectId = Serial;

        nearbyAisling.Client.SendRemoveObject(objectId);
    }

    private void DeleteObject()
    {
        if (this is Monster)
            ObjectManager.DelObject(this as Monster);
        if (this is Aisling)
            ObjectManager.DelObject(this as Aisling);
        if (this is Money)
            ObjectManager.DelObject(this as Money);
        if (this is Item)
            ObjectManager.DelObject(this as Item);
        if (this is Mundane)
            ObjectManager.DelObject(this as Mundane);
    }
}