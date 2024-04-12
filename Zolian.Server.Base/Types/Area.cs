using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.ScriptingBase;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.IdentityModel.Tokens;

using ServiceStack;

using System.Numerics;

namespace Darkages.Types;

public class Area : Map, IArea
{
    private readonly byte[] _sotp = File.ReadAllBytes("sotp.dat");
    public byte[] Data;
    public ushort Hash;
    public bool Ready;
    private readonly object _mapLoadLock = new();

    public int MiningNodesCount { get; set; }
    public int WildFlowersCount { get; set; }
    public TileGrid[,] ObjectGrid { get; set; }
    public TileContent[,] TileContent { get; set; }
    public Tuple<string, AreaScript> Script { get; set; }
    public string FilePath { get; set; }

    public Vector2 GetPosFromLoc(Vector2 location) => Vector2.Zero + new Vector2((int)location.X * Vector2.One.X, (int)location.Y * Vector2.One.Y);

    public bool IsLocationOnMap(Sprite sprite)
    {
        var xTrue = false;
        var yTrue = false;

        for (var x = 0; x < sprite.Map.ObjectGrid.GetLength(0); x++)
        {
            if ((int)sprite.Pos.X == x)
                xTrue = true;
        }

        for (var y = 0; y < sprite.Map.ObjectGrid.GetLength(1); y++)
        {
            if ((int)sprite.Pos.Y == y)
                yTrue = true;
        }

        return xTrue && yTrue;
    }

    public IEnumerable<byte> GetRowData(int row)
    {
        try
        {
            var buffer = new byte[Width * 6];
            var bPos = 0;
            var dPos = row * Width * 6;

            for (var i = 0; i < Width; i++, bPos += 6, dPos += 6)
            {
                buffer[bPos + 0] = Data[dPos + 1];
                buffer[bPos + 1] = Data[dPos + 0];
                buffer[bPos + 2] = Data[dPos + 3];
                buffer[bPos + 3] = Data[dPos + 2];
                buffer[bPos + 4] = Data[dPos + 5];
                buffer[bPos + 5] = Data[dPos + 4];
            }

            return buffer;
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
            Crashes.TrackError(ex);
        }

        return default;
    }

    public bool IsWall(int x, int y)
    {
        if (!x.Between(0, Width) || !y.Between(0, Height)) return true; // Out of range, return true
        var isWall = TileContent[x, y] == Enums.TileContent.Wall;
        return isWall;
    }

    public bool IsAStarWall(Sprite sprite, int x, int y)
    {
        if (!x.Between(0, sprite.Map.Width) || !y.Between(0, sprite.Map.Height)) return true; // Out of range, return true
        var isWall = sprite.Map.TileContent[x, y] == Enums.TileContent.Wall;
        return isWall;
    }

    /// <summary>
    /// This method is called in real-time multiple times and calculates each grid square
    /// and sprite positioned within each
    /// </summary>
    public bool IsSpriteInLocationOnWalk(Sprite sprite, int x, int y)
    {
        if (sprite is not Mundane)
            if (sprite is null || sprite.CurrentHp <= 0 || ((int)sprite.Pos.X == x && (int)sprite.Pos.Y == y)) return false;
        if (!x.Between(0, sprite.Map.Width) || !y.Between(0, sprite.Map.Height)) return true; // Out of range, return true
        if (x >= sprite.Map.ObjectGrid.GetLength(0) || y >= sprite.Map.ObjectGrid.GetLength(1)) return false; // Bounds check, return false

        // Grab list of sprites on x & y
        var spritesOnLocation = sprite.Map.ObjectGrid[x, y].Sprites.ToList();
        if (spritesOnLocation.IsNullOrEmpty()) return false;
        var first = spritesOnLocation.First();
        return sprite.Target?.Pos != first.Pos;
    }

    /// <summary>
    /// Similar to the IsAStarSprite method, this method is called on Monster creation to ensure monsters aren't created
    /// on top of other sprites or walls
    /// </summary>
    public bool IsSpriteInLocationOnCreation(Sprite sprite, int x, int y)
    {
        if (!x.Between(0, sprite.Map.Width) || !y.Between(0, sprite.Map.Height)) return true; // Out of range, return true
        if (x >= sprite.Map.ObjectGrid.GetLength(0) || y >= sprite.Map.ObjectGrid.GetLength(1)) return false; // Bounds check, return false
        return !sprite.Map.ObjectGrid[x, y].Sprites.IsNullOrEmpty();
    }

    public bool OnLoaded()
    {
        lock (_mapLoadLock)
        {
            TileContent = new TileContent[Width, Height];
            ObjectGrid = new TileGrid[Width, Height];

            using var stream = new MemoryStream(Data);
            using var reader = new BinaryReader(stream);

            try
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                for (byte y = 0; y < Height; y++)
                {
                    for (byte x = 0; x < Width; x++)
                    {
                        ObjectGrid[x, y] = new TileGrid(this, x, y);

                        reader.BaseStream.Seek(2, SeekOrigin.Current);

                        if (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            var a = reader.ReadInt16();
                            var b = reader.ReadInt16();

                            if (ParseMapWalls(a, b))
                                TileContent[x, y] = Enums.TileContent.Wall;
                            else
                                TileContent[x, y] = Enums.TileContent.None;
                        }
                        else
                        {
                            TileContent[x, y] = Enums.TileContent.Wall;
                        }
                    }
                }

                Ready = true;
            }
            catch (Exception ex)
            {
                ServerSetup.EventsLogger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
                ServerSetup.EventsLogger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
                Crashes.TrackError(ex);
                return false;
            }
        }

        return Ready;
    }

    public bool ParseMapWalls(int lWall, int rWall)
    {
        if (lWall == 0 && rWall == 0) return false;
        if (lWall == 0) return _sotp[rWall - 1] == 0x0F;
        if (rWall == 0) return _sotp[lWall - 1] == 0x0F;

        var left = _sotp[lWall - 1];
        var right = _sotp[rWall - 1];

        return left == 0x0F || right == 0x0F;
    }

    public void Update(in TimeSpan elapsedTime)
    {
        if (Script.Item1.IsNullOrEmpty()) return;
        Script.Item2.Update(elapsedTime);
    }

    #region A* (A Star)

    public List<Vector2> GetPath(Monster sprite, Vector2 start, Vector2 end)
    {
        var path = new List<Vector2>();

        switch (sprite.Target)
        {
            case null:
                return path;
            case Aisling { LoggedIn: false }:
                return path;
            case Aisling { IsInvisible: true }:
                return path;
            case Aisling { Map: null }:
                return path;
        }

        if (sprite.Target.Map.ID != sprite.Map.ID) return path;
        if (!sprite.WithinEarShotOf(sprite.Target)) return path;
        if (start == Vector2.Zero) return path;
        if (end == Vector2.Zero) return path;

        CheckNode(sprite, start);

        if (sprite.TempAlgoGrid.Count == 0) return path;

        //Set direction of node
        //CheckDirectionOfNode(sprite, start);

        path.Clear();
        return SetPath(sprite, path, start);
    }

    /// <summary>
    /// Check nodes near monster and apply cost, fscore
    /// </summary>
    private void CheckNode(Sprite sprite, Vector2 start)
    {
        sprite.TempAlgoGrid.Clear();
        //sprite.TempCurrentGrid.Clear();
        sprite.TempAlgoGrid = [new TileGrid(start, 0, 999, false, 999)];
        //sprite.TempCurrentGrid = [new TileGrid(start, 0, 999, false, 999)];

        for (var x = 0; x < sprite.Map.Width; x++)
        {
            for (var y = 0; y < sprite.Map.Height; y++)
            {
                var node = new Position(x, y);
                if (!sprite.Position.CanPathToSprite(node)) continue;
                if (sprite.Map.IsAStarWall(sprite, x, y)) continue;
                var targetDist = node.DistanceFrom(sprite.Target?.Position);
                var nodeDist = node.DistanceFrom(sprite.Position);
                var filled = sprite.Map.IsSpriteInLocationOnWalk(sprite, x, y);
                var cost = 1;
                cost *= nodeDist;

                if (filled)
                    cost *= 5;

                sprite.TempAlgoGrid.Add(new TileGrid(new Vector2(x, y), targetDist, cost, filled, nodeDist * cost));
            }
        }

        var list = sprite.TempAlgoGrid.OrderBy(o => o.Cost).ToList();
        sprite.TempAlgoGrid = list;
    }

    //private void CheckDirectionOfNode(Sprite sprite, Vector2 start)
    //{
    //    var north = start with { Y = start.Y - 1 };
    //    var east = start with { X = start.X + 1 };
    //    var south = start with { Y = start.Y + 1 };
    //    var west = start with { X = start.X - 1 };
        
    //    if (tileGrid.Pos == north)
    //    {
    //        var nPos = new Position(north);
    //        var targetDist = nPos.DistanceFrom(sprite.Target?.Position);
    //        var northTile = new TileGrid(north, targetDist, tileGrid.Cost, tileGrid.FilledNode, tileGrid.FScore);
    //        SetAStarNodeInsert(sprite.TempCurrentGrid, northTile);
    //    }

    //    if (tileGrid.Pos == east)
    //    {
    //        var ePos = new Position(east);
    //        var targetDist = ePos.DistanceFrom(sprite.Target?.Position);
    //        var eastTile = new TileGrid(east, targetDist, tileGrid.Cost, tileGrid.FilledNode, tileGrid.FScore);
    //        SetAStarNodeInsert(sprite.TempCurrentGrid, eastTile);
    //    }

    //    if (tileGrid.Pos == south)
    //    {
    //        var sPos = new Position(south);
    //        var targetDist = sPos.DistanceFrom(sprite.Target?.Position);
    //        var southTile = new TileGrid(south, targetDist, tileGrid.Cost, tileGrid.FilledNode, tileGrid.FScore);
    //        SetAStarNodeInsert(sprite.TempCurrentGrid, southTile);
    //    }

    //    if (tileGrid.Pos == west)
    //    {
    //        var wPos = new Position(west);
    //        var targetDist = wPos.DistanceFrom(sprite.Target?.Position);
    //        var westTile = new TileGrid(west, targetDist, tileGrid.Cost, tileGrid.FilledNode, tileGrid.FScore);
    //        SetAStarNodeInsert(sprite.TempCurrentGrid, westTile);
    //    }


    //    sprite.TempCurrentGrid[0].HasBeenUsed = true;
    //    sprite.TempCurrentGrid.RemoveAt(0);
    //}


    private List<Vector2> SetPath(Sprite sprite, List<Vector2> path, Vector2 start)
    {
        var count = 0;
        var lastTile = new TileGrid(Vector2.Zero, 0, 0, false, 0);

        while (true)
        {
            var tempTile = sprite.TempAlgoGrid[count];
            var tempPos = sprite.Map.GetPosFromLoc(tempTile.Pos) + Vector2.One / 2;
            if (tempTile.Cost <= lastTile.Cost) continue;

            path.Add(new Vector2((int)tempPos.X, (int)tempPos.Y));
            lastTile = tempTile;

            if (tempTile.Pos == start) break;

            count++;
        }

        path.Reverse();
        if (path.Count != 0) path.RemoveAt(0);
        return path;
    }

    #endregion
}

public static class RangeExtensions
{
    public static bool Between(this int num, int start, int end, bool inclusive = false) => inclusive ? start <= num && num <= end : start < num && num < end;
}