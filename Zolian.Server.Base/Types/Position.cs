using Darkages.Enums;
using Darkages.Network.Server;
using System.Numerics;
using Darkages.Sprites;

namespace Darkages.Types;

[Serializable]
public class Position
{
    private readonly Lock _sync = new();
    private ushort _x;
    private ushort _y;

    public Position() { }

    public Position(Vector2 position)
    {
        _x = (ushort)position.X;
        _y = (ushort)position.Y;
    }

    public Position(int x, ushort y)
    {
        _x = (ushort)x;
        _y = y;
    }

    public Position(ushort x, int y)
    {
        _x = x;
        _y = (ushort)y;
    }

    public Position(int x, int y)
    {
        _x = (ushort)x;
        _y = (ushort)y;
    }

    public Position(float x, float y)
    {
        _x = (ushort)x;
        _y = (ushort)y;
    }

    public Position(ushort readX, ushort readY)
    {
        _x = readX;
        _y = readY;
    }

    public ushort X
    {
        get
        {
            lock (_sync)
                return _x;
        }
        set => Update(value, null);
    }

    public ushort Y
    {
        get
        {
            lock (_sync)
                return _y;
        }
        set => Update(null, value);
    }

    private void Update(ushort? x, ushort? y)
    {
        lock (_sync)
        {
            _x = x ?? _x;
            _y = y ?? _y;
        }
    }

    public void GetSnapshot(out ushort x, out ushort y)
    {
        lock (_sync)
        {
            x = _x;
            y = _y;
        }
    }

    public int DistanceFrom(ushort xDist, ushort yDist)
    {
        GetSnapshot(out var currentX, out var currentY);

        double xDiff = Math.Abs(xDist - currentX);
        double yDiff = Math.Abs(yDist - currentY);

        return (int)(xDiff > yDiff ? xDiff : yDiff);
    }

    public int DistanceFrom(Position pos)
    {
        if (pos == null)
            return 0;

        pos.GetSnapshot(out var posX, out var posY);
        return DistanceFrom(posX, posY);
    }

    public bool IsNearby(Position pos)
    {
        GetSnapshot(out var currentX, out var currentY);
        return pos.DistanceFrom(currentX, currentY) <= ServerSetup.Instance.Config.VeryNearByProximity;
    }

    public bool IsNextTo(Position pos, int distance = 1)
    {
        GetSnapshot(out var x, out var y);
        pos.GetSnapshot(out var posX, out var posY);

        if (x == posX && y + distance == posY) return true;
        if (x == posX && y - distance == posY) return true;
        if (x == posX + distance && y == posY) return true;
        return x == posX - distance && y == posY;
    }

    public TileContentPosition[] SurroundingContent(Sprite sprite, Area map)
    {
        var list = new List<TileContentPosition>();

        try
        {
            GetSnapshot(out var x, out var y);

            if (x > 0)
                list.Add(new TileContentPosition(new Position(x - 1, y),
                    sprite.GetMovableSpritesInPosition(x - 1, y).Count == 0
                        ? !map.IsWall(x - 1, y) ? TileContent.None : TileContent.Wall
                        : TileContent.Wall));

            if (y > 0)
                list.Add(new TileContentPosition(new Position(x, y - 1),
                    sprite.GetMovableSpritesInPosition(x, y - 1).Count == 0
                        ? !map.IsWall(x, y - 1) ? TileContent.None : TileContent.Wall
                        : TileContent.Wall));

            if (x < map.Height - 1)
                list.Add(new TileContentPosition(new Position(x + 1, y),
                    sprite.GetMovableSpritesInPosition(x + 1, y).Count == 0
                        ? !map.IsWall(x + 1, y) ? TileContent.None : TileContent.Wall
                        : TileContent.Wall));

            if (y < map.Width - 1)
                list.Add(new TileContentPosition(new Position(x, y + 1),
                    sprite.GetMovableSpritesInPosition(x, y + 1).Count == 0
                        ? !map.IsWall(x, y + 1) ? TileContent.None : TileContent.Wall
                        : TileContent.Wall));
        }
        catch
        {
            return null;
        }

        return list.ToArray();
    }

    public class TileContentPosition(Position pos, TileContent content)
    {
        public TileContent Content { get; set; } = content;
        public Position Position { get; set; } = pos;
    }

    public static bool TryParse(string xValue, string yValue, out Position position)
    {
        position = null;

        if (!int.TryParse(xValue, out var x) || !int.TryParse(yValue, out var y))
            return false;

        position = new Position(x, y);
        return true;
    }
}