using System.Collections.Concurrent;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.ScriptingBase;
using Darkages.Sprites;
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

    public ConcurrentDictionary<int, ConcurrentDictionary<Type, object>> SpriteCollections { get; set; } = [];
    public int MiningNodesCount { get; set; }
    public int WildFlowersCount { get; set; }
    public TileGrid[,] ObjectGrid { get; set; }
    public TileContent[,] TileContent { get; set; }
    public Tuple<string, AreaScript> Script { get; set; }
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
            SentrySdk.CaptureException(ex);
        }

        return default;
    }

    public bool IsWall(int x, int y)
    {
        if (x < 0 || x >= Width) return true;
        if (y < 0 || y >= Height) return true;

        var isWall = TileContent[x, y] == Enums.TileContent.Wall;
        return isWall;
    }

    public bool IsAStarWall(Sprite sprite, int x, int y)
    {
        if (x < 0 || x >= sprite.Map.Width) return true;
        if (y < 0 || y >= sprite.Map.Height) return true;

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
        if (x < 0 || y < 0 || x >= sprite.Map.Width || y >= sprite.Map.Height) return true; // Is wall, return true
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
        if (x < 0 || y < 0 || x >= sprite.Map.Width || y >= sprite.Map.Height) return true; // Is wall, return true
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
                SentrySdk.CaptureException(ex);
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

    public async Task<IList<Vector2>> GetPath(Monster sprite, Vector2 start, Vector2 end)
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

        List<TileGrid> viewable = [], used = [];
        await Task.Run(CheckNode(sprite));

        #region Try to set viewable Nodes

        try
        {
            if (sprite.Target == null) return path;
            if (sprite.Map.ID != sprite.Target?.Map.ID) return path;
            if (sprite.MasterGrid.Count == 0) return path;

            viewable.Add(sprite.MasterGrid[(int)start.X][(int)start.Y]);
        }
        catch (Exception ex)
        {
            Console.Write($"Pathing Issue... {ex}\n");
            SentrySdk.CaptureException(ex);

            return path;
        }

        #endregion

        #region Check Direction of Node & Set Pathing

        if (viewable.Count <= 0) return path;

        while (viewable.Count > 0 && !((int)viewable[0].Pos.X == (int)end.X && (int)viewable[0].Pos.Y == (int)end.Y))
        {
            CheckDirectionOfNode(sprite.MasterGrid, viewable, used);
        }

        if (viewable.Count <= 0) return path;

        #endregion

        var currentNode = viewable[0];
        path.Clear();
        return SetPath(sprite, currentNode, path, start, viewable);
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
                if ((int)currentNode.Pos.X == (int)sprite.MasterGrid[(int)currentNode.Parent.X][(int)currentNode.Parent.Y].Pos.X && (int)currentNode.Pos.Y == (int)sprite.MasterGrid[(int)currentNode.Parent.X][(int)currentNode.Parent.Y].Pos.Y)
                {
                    currentNode = viewable[currentViewableStart];
                    currentViewableStart++;
                }

                currentNode = sprite.MasterGrid[(int)currentNode.Parent.X][(int)currentNode.Parent.Y];
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
        sprite.MasterGrid = [];

        return delegate
        {
            for (var x = 0; x < sprite.Map.Width; x++)
            {
                sprite.MasterGrid.Add([]);

                for (var y = 0; y < sprite.Map.Height; y++)
                {
                    var impassable = sprite.Map.IsAStarWall(sprite, x, y);
                    var filled = sprite.Map.IsSpriteInLocationOnWalk(sprite, x, y);
                    var cost = 1;

                    if (filled)
                    {
                        impassable = true;
                    }

                    if (impassable)
                    {
                        cost = 999;
                    }

                    sprite.MasterGrid[x].Add(new TileGrid(new Vector2(x, y), cost, impassable, sprite.Position.DistanceFrom(sprite.Target?.Position)));
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