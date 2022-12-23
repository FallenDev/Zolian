using System.Numerics;

using Darkages.Enums;

namespace Darkages.Types
{
    [Serializable]
    public class Position
    {
        public Position() {}

        public Position(Vector2 position)
        {
            X = (ushort)position.X;
            Y = (ushort)position.Y;
        }

        public Position(int x, ushort y)
        {
            X = (ushort) x;
            Y = y;
        }

        public Position(ushort x, int y)
        {
            X = x;
            Y = (ushort) y;
        }

        public Position(int x, int y)
        {
            X = (ushort) x;
            Y = (ushort) y;
        }

        public Position(float x, float y)
        {
            X = (ushort)x;
            Y = (ushort)y;
        }

        public Position(ushort readX, ushort readY)
        {
            X = readX;
            Y = readY;
        }

        public ushort X { get; set; }
        public ushort Y { get; set; }

        public int DistanceFrom(ushort xDist, ushort yDist)
        {
            double xDiff = Math.Abs(xDist - this.X);
            double yDiff = Math.Abs(yDist - this.Y);

            return (int) (xDiff > yDiff ? xDiff : yDiff);
        }

        public int DistanceFrom(Position pos)
        {
            return DistanceFrom(pos.X, pos.Y);
        }

        public bool IsNearby(Position pos)
        {
            return pos.DistanceFrom(X, Y) <= ServerSetup.Instance.Config.VeryNearByProximity;
        }

        public bool IsNextTo(Position pos, int distance = 1)
        {
            if (X == pos.X && Y + distance == pos.Y) return true;
            if (X == pos.X && Y - distance == pos.Y) return true;
            if (X == pos.X + distance && Y == pos.Y) return true;
            return X == pos.X - distance && Y == pos.Y;
        }

        public TileContentPosition[] SurroundingContent(Area map)
        {
            var list = new List<TileContentPosition>();

            if (X > 0)
                list.Add(new TileContentPosition(new Position(X - 1, Y),
                    !map.ObjectGrid[X - 1, Y].Sprites.Any() ? !map.IsWall(X - 1, Y) ? TileContent.None : TileContent.Wall : TileContent.Wall));

            if (Y > 0)
                list.Add(new TileContentPosition(new Position(X, Y - 1),
                    !map.ObjectGrid[X, Y - 1].Sprites.Any() ? !map.IsWall(X, Y - 1) ? TileContent.None : TileContent.Wall : TileContent.Wall));

            if (X < map.Rows - 1)
                list.Add(new TileContentPosition(new Position(X + 1, Y),
                    !map.ObjectGrid[X + 1, Y].Sprites.Any() ? !map.IsWall(X + 1, Y) ? TileContent.None : TileContent.Wall : TileContent.Wall));

            if (Y < map.Cols - 1)
                list.Add(new TileContentPosition(new Position(X, Y + 1),
                    !map.ObjectGrid[X, Y + 1].Sprites.Any() ? !map.IsWall(X, Y + 1) ? TileContent.None : TileContent.Wall : TileContent.Wall));

            return list.ToArray();
        }

        public class TileContentPosition
        {
            public TileContentPosition(Position pos, TileContent content)
            {
                Position = pos;
                Content = content;
            }

            public TileContent Content { get; set; }
            public Position Position { get; set; }
        }

        public static bool TryParse(string xValue, string yValue,  out Position position)
        {
            position = null;

            if (!int.TryParse(xValue, out var x) || !int.TryParse(yValue, out var y))
                return false;

            position = new Position(x, y);
            return true;
        }
    }
}