using System.Collections.Concurrent;
using System.Numerics;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Scripting;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Types;

public class Area : Map, IArea
{
    private readonly byte[] _sotp = File.ReadAllBytes("sotp.dat");
    public byte[] Data;
    public ushort Hash;
    public bool Ready;
    private readonly List<List<TileGrid>> _tiles = new();
    private List<List<TileGrid>> _masterGrid = new();

    public int MiningNodes { get; set; }
    public TileGrid[,] ObjectGrid { get; set; }
    public TileContent[,] TileContent { get; set; }
    public ConcurrentDictionary<string, AreaScript> Scripts { get; set; } = new();
    public string FilePath { get; set; }

    public Vector2 GetPosFromLoc(Vector2 location)
    {
        return Vector2.Zero + new Vector2((int)location.X * Vector2.One.X, (int)location.Y * Vector2.One.Y);
    }

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

    public byte[] GetRowData(int row)
    {
        var buffer = new byte[Cols * 6];
        var bPos = 0;
        var dPos = row * Cols * 6;

        lock (ServerSetup.SyncLock)
        {
            for (var i = 0; i < Cols; i++, bPos += 6, dPos += 6)
            {
                buffer[bPos + 0] = Data[dPos + 1];

                buffer[bPos + 1] = Data[dPos + 0];

                buffer[bPos + 2] = Data[dPos + 3];

                buffer[bPos + 3] = Data[dPos + 2];

                buffer[bPos + 4] = Data[dPos + 5];

                buffer[bPos + 5] = Data[dPos + 4];
            }
        }

        return buffer;
    }

    public bool IsWall(int x, int y)
    {
        if (x < 0 || x >= Cols) return true;
        if (y < 0 || y >= Rows) return true;

        var isWall = TileContent[x, y] == Enums.TileContent.Wall;
        return isWall;
    }

    public bool IsAStarWall(Sprite sprite, int x, int y)
    {
        if (x < 0 || x >= sprite.Map.Cols) return true;
        if (y < 0 || y >= sprite.Map.Rows) return true;

        var isWall = sprite.Map.TileContent[x, y] == Enums.TileContent.Wall;
        return isWall;
    }

    public bool IsAStarSprite(Sprite sprite, int x, int y)
    {
        if (sprite is null || sprite.CurrentHp <= 0) return false;
        if ((int)sprite.Pos.X == x && (int)sprite.Pos.Y == y) return false;
        if (x < 0 || x >= sprite.Map.Cols) return true;
        if (y < 0 || y >= sprite.Map.Rows) return true;

        try
        {
            var isWall = sprite.Map.ObjectGrid[x, y].Sprites.Any();

            if (sprite.Target is null) return isWall;
            if ((int)sprite.Target.Pos.X == x && (int)sprite.Target.Pos.Y == y)
            {
                isWall = false;
            }

            return isWall;
        }
        catch (AggregateException ex)
        {
            Crashes.TrackError(ex);
        }

        return false;
    }

    public bool IsSpriteInLocationOnCreation(Sprite sprite, int x, int y)
    {
        if (x < 0 || x >= sprite.Map.Cols) return true;
        if (y < 0 || y >= sprite.Map.Rows) return true;

        try
        {
            var isWall = sprite.Map.ObjectGrid[x, y].Sprites.Any();

            if (sprite.Target is null) return isWall;
            if ((int)sprite.Target.Pos.X == x && (int)sprite.Target.Pos.Y == y)
            {
                isWall = false;
            }

            return isWall;
        }
        catch (AggregateException ex)
        {
            Crashes.TrackError(ex);
        }

        return false;
    }

    public bool OnLoaded()
    {
        var delete = false;

        lock (ServerSetup.SyncLock)
        {
            TileContent = new TileContent[Cols, Rows];
            ObjectGrid = new TileGrid[Cols, Rows];

            using (var stream = new MemoryStream(Data))
            {
                using var reader = new BinaryReader(stream);

                try
                {
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);

                    for (var y = 0; y < Rows; y++)
                    {
                        _tiles.Add(new List<TileGrid>());

                        for (var x = 0; x < Cols; x++)
                        {
                            _tiles[y].Add(new TileGrid(x));
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
                    ServerSetup.Logger(ex.Message, Microsoft.Extensions.Logging.LogLevel.Error);
                    ServerSetup.Logger(ex.StackTrace, Microsoft.Extensions.Logging.LogLevel.Error);
                    Crashes.TrackError(ex);

                    delete = true;
                }
                finally
                {
                    reader.Close();
                    stream.Close();
                }
            }

            if (!delete) return true;
        }

        return Ready;
    }

    public bool ParseMapWalls(short lWall, short rWall)
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
        if (Scripts == null) return;
        foreach (var script in Scripts.Values)
        {
            script.Update(elapsedTime);
        }
    }

    #region A* (A Star)

    public async Task<List<Vector2>> GetPath(Monster sprite, Vector2 start, Vector2 end)
    {
        var path = new List<Vector2>();

        switch (sprite.Target)
        {
            case null:
                return path;
            case Aisling { LoggedIn: false }:
                return path;
            case Aisling { Invisible: true }:
                return path;
            case Aisling { Map: null }:
                return path;
        }

        if (sprite.Target.Map.ID != sprite.Map.ID) return path;
        if (!sprite.WithinRangeOf(sprite.Target)) return path;
        if (start == Vector2.Zero) return path;
        if (end == Vector2.Zero) return path;

        List<TileGrid> viewable = new(), used = new();
        await Task.Run(CheckNode(sprite));

        #region Try to set viewable Nodes

        try
        {
            if (sprite.Target == null) return path;
            if (sprite.Map.ID != sprite.Target?.Map.ID) return path;
            if (_masterGrid.Count == 0) return path;

            viewable.Add(_masterGrid[(int)start.X][(int)start.Y]);
        }
        catch (Exception ex)
        {
            Console.Write($"Pathing Issue... {ex}\n");
            Crashes.TrackError(ex);

            return path;
        }

        #endregion

        #region Check Direction of Node & Set Pathing

        if (viewable.Count <= 0) return path;

        while (viewable.Count > 0 && !((int)viewable[0].Pos.X == (int)end.X && (int)viewable[0].Pos.Y == (int)end.Y))
        {
            CheckDirectionOfNode(_masterGrid, viewable, used);
        }

        if (viewable.Count <= 0) return path;

        #endregion

        var currentNode = viewable[0];
        path.Clear();
        var aStar = SetPath(sprite, currentNode, path, start, viewable);

        return aStar;
    }

    private List<Vector2> SetPath(Sprite sprite, TileGrid currentNode, List<Vector2> path, Vector2 start, List<TileGrid> viewable)
    {
        var currentViewableStart = 0;

        while (true)
        {
            var tempPos = sprite.Map.GetPosFromLoc(currentNode.Pos) + Vector2.One / 2;
            path.Add(new Vector2((int)tempPos.X, (int)tempPos.Y));

            if (currentNode.Pos == start)
            {
                break;
            }

            if ((int)currentNode.Parent.X != -1 && (int)currentNode.Parent.Y != -1)
            {
                if ((int)currentNode.Pos.X == (int)_masterGrid[(int)currentNode.Parent.X][(int)currentNode.Parent.Y].Pos.X && (int)currentNode.Pos.Y == (int)_masterGrid[(int)currentNode.Parent.X][(int)currentNode.Parent.Y].Pos.Y)
                {
                    currentNode = viewable[currentViewableStart];
                    currentViewableStart++;
                }

                currentNode = _masterGrid[(int)currentNode.Parent.X][(int)currentNode.Parent.Y];
            }
            else
            {
                currentNode = viewable[currentViewableStart];
                currentViewableStart++;
            }
        }

        path.Reverse();
        if (path.Any())
        {
            path.RemoveAt(0);
        }

        return path;
    }

    private Action CheckNode(Sprite sprite)
    {
        var tempGrid = sprite.Map._tiles;
        _masterGrid = new List<List<TileGrid>>();

        return delegate
        {
            for (var x = 0; x < tempGrid.Count; x++)
            {
                _masterGrid.Add(new List<TileGrid>());
                for (var y = 0; y < tempGrid.Count; y++)
                {
                    var impassable = sprite.Map.IsAStarWall(sprite, x, y);
                    var filled = sprite.Map.IsAStarSprite(sprite, x, y);
                    var cost = 1;

                    if (filled)
                    {
                        impassable = true;
                    }

                    if (impassable)
                    {
                        cost = 999;
                    }

                    _masterGrid[x].Add(new TileGrid(new Vector2(x, y), cost, impassable, 99999999));
                }
            }
        };
    }

    public void CheckDirectionOfNode(IReadOnlyList<IList<TileGrid>> masterGrid, IList<TileGrid> viewable, ICollection<TileGrid> used)
    {
        TileGrid currentNode;

        //North
        if (viewable[0].Pos.Y > 0 && viewable[0].Pos.Y < masterGrid[0].Count && !masterGrid[(int)viewable[0].Pos.X][(int)viewable[0].Pos.Y - 1].Impassable)
        {
            currentNode = masterGrid[(int)viewable[0].Pos.X][(int)viewable[0].Pos.Y - 1];
            SetAStarNode(viewable, currentNode, new Vector2(viewable[0].Pos.X, viewable[0].Pos.Y), viewable[0].CurrentDist, 1);
        }

        //East
        if (viewable[0].Pos.X >= 0 && viewable[0].Pos.X + 1 < masterGrid.Count && !masterGrid[(int)viewable[0].Pos.X + 1][(int)viewable[0].Pos.Y].Impassable)
        {
            currentNode = masterGrid[(int)viewable[0].Pos.X + 1][(int)viewable[0].Pos.Y];
            SetAStarNode(viewable, currentNode, new Vector2(viewable[0].Pos.X, viewable[0].Pos.Y), viewable[0].CurrentDist, 1);
        }

        //South
        if (viewable[0].Pos.Y >= 0 && viewable[0].Pos.Y + 1 < masterGrid[0].Count && !masterGrid[(int)viewable[0].Pos.X][(int)viewable[0].Pos.Y + 1].Impassable)
        {
            currentNode = masterGrid[(int)viewable[0].Pos.X][(int)viewable[0].Pos.Y + 1];
            SetAStarNode(viewable, currentNode, new Vector2(viewable[0].Pos.X, viewable[0].Pos.Y), viewable[0].CurrentDist, 1);
        }

        //West
        if (viewable[0].Pos.X > 0 && viewable[0].Pos.X < masterGrid.Count && !masterGrid[(int)viewable[0].Pos.X - 1][(int)viewable[0].Pos.Y].Impassable)
        {
            currentNode = masterGrid[(int)viewable[0].Pos.X - 1][(int)viewable[0].Pos.Y];
            SetAStarNode(viewable, currentNode, new Vector2(viewable[0].Pos.X, viewable[0].Pos.Y), viewable[0].CurrentDist, 1);
        }

        viewable[0].HasBeenUsed = true;
        used.Add(viewable[0]);
        viewable.RemoveAt(0);
    }

    public void SetAStarNode(IList<TileGrid> viewable, TileGrid nextNode, Vector2 nextParent, float d, float distanceMultiply)
    {
        var addedDist = nextNode.Cost * distanceMultiply;

        switch (nextNode.IsViewable)
        {
            case false when !nextNode.HasBeenUsed:
            {
                nextNode.SetNode(nextParent, d, d + addedDist);
                nextNode.IsViewable = true;
                SetAStarNodeInsert(viewable, nextNode);
            }
                break;
            case true:
            {
                if (d < nextNode.FScore)
                {
                    nextNode.SetNode(nextParent, d, d + addedDist);
                }
            }
                break;
        }
    }

    public void SetAStarNodeInsert(IList<TileGrid> list, TileGrid newNode)
    {
        var added = false;
        for (var i = 0; i < list.Count; i++)
        {
            if (!(list[i].FScore > newNode.FScore)) continue;
            list.Insert(Math.Max(1, i), newNode);
            added = true;
            break;
        }

        if (!added)
        {
            list.Add(newNode);
        }
    }

    #endregion
}