using System.Collections.Concurrent;
using System.Collections.Frozen;
using Darkages.Enums;
using Darkages.Models;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using ServiceStack;
using System.Numerics;
using Darkages.Network.Server;
using Darkages.Sprites.Entity;

namespace Darkages.Types;

public class Area : Map
{
    private readonly byte[] _sotp = File.ReadAllBytes("sotp.dat");
    public byte[] Data;
    public ushort Hash;
    public bool Ready;
    private readonly Lock _mapLoadLock = new();

    public ConcurrentDictionary<Tuple<int, Type>, object> SpriteCollections { get; } = []; 
    public FrozenDictionary<int, Vector2> MapGridDict { get; set; }
    private Dictionary<int, Vector2> TempMapGridDict { get; } = [];
    public int MiningNodesCount { get; set; }
    public int WildFlowersCount { get; set; }
    public Tuple<string, AreaScript> Script { get; set; }
    public string FilePath { get; set; }
    public DateTime LastDoorClicked { get; set; } = DateTime.UtcNow;


    public static bool IsLocationOnMap(Sprite sprite)
    {
        foreach (var (nodeId, node) in sprite.Map.MapGridDict)
        {
            if ((int)node.X != (int)sprite.Pos.X && (int)node.Y != (int)sprite.Pos.Y) continue;
            return true;
        }

        return false;
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

    public static bool IsAStarWall(Sprite sprite, int x, int y)
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
    public static bool IsSpriteInLocationOnWalk(Sprite sprite, int x, int y)
    {
        if (sprite.Map.TileContent[x, y] == Enums.TileContent.Wall) return true; // Is wall, return true
        var bounds = MaxVectorValuesXAndYOnMap(sprite);
        if (x < 0 || y < 0 || x > bounds.Item1 || y > bounds.Item2) return true; // OOB return true

        // Grab list of sprites on x & y
        var spritesOnLocation = sprite.GetMovableSpritesInPosition(x, y);
        if (spritesOnLocation.IsNullOrEmpty()) return false;
        var first = spritesOnLocation.FirstOrDefault();
        if (sprite is Mundane or Aisling)
            return sprite.Pos != first?.Pos;
        return sprite.Target?.Pos != first?.Pos;
    }

    /// <summary>
    /// This method is called for ghost walking to prevent OOB on maps
    /// </summary>
    public static bool IsSpriteWithinBoundsWhileGhostWalking(Sprite sprite, int x, int y)
    {
        if (sprite is not Mundane)
            if (sprite is null || sprite.CurrentHp <= 0) return false;
        var bounds = MaxVectorValuesXAndYOnMap(sprite);
        return x >= 0 && y >= 0 && x <= bounds.Item1 && y <= bounds.Item2;
    }

    /// <summary>
    /// Similar to the IsAStarSprite method, this method is called on Monster creation to ensure monsters aren't created
    /// on top of other sprites or walls
    /// </summary>
    public static bool IsSpriteInLocationOnCreation(Sprite sprite, int x, int y)
    {
        var bounds = MaxVectorValuesXAndYOnMap(sprite);
        if (x < 0 || y < 0 || x > bounds.Item1 || y > bounds.Item2) return true; // OOB return true
        return !sprite.GetMovableSpritesInPosition(x, y).IsNullOrEmpty();
    }

    public bool OnLoaded()
    {
        lock (_mapLoadLock)
        {
            TileContent = new TileContent[Width, Height];
            using var stream = new MemoryStream(Data);
            using var reader = new BinaryReader(stream);
            var count = 0;

            try
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                for (byte y = 0; y < Height; y++)
                {
                    for (byte x = 0; x < Width; x++)
                    {
                        TempMapGridDict.TryAdd(count++, new Vector2(x, y));
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

        MapGridDict = TempMapGridDict.ToFrozenDictionary();
        TempMapGridDict.Clear();
        return Ready;
    }

    private bool ParseMapWalls(int lWall, int rWall)
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

    private static (int, int) MaxVectorValuesXAndYOnMap(Sprite sprite)
    {
        var gridSeries = sprite.Map.MapGridDict.Values.ToList();
        var maxX = (int)gridSeries[0].X;
        var maxY = (int)gridSeries[0].Y;

        try
        {
            for (var i = 0; i < gridSeries.Count; i++)
            {
                maxX = Math.Max((int)gridSeries[i].X, maxX);
                maxY = Math.Max((int)gridSeries[i].Y, maxY);
            }
        }
        catch
        {
            // ignored
        }

        return (maxX, maxY);
    }

    public bool ShouldRegisterClick
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime - LastDoorClicked > new TimeSpan(0, 0, 0, 1, 500);
        }
    }

    #region A* new

    public List<Vector2> FindPath(Monster sprite, Vector2 start, Vector2 end)
    {
        switch (sprite.Target)
        {
            case null:
            case Aisling { LoggedIn: false }:
            case Aisling { IsInvisible: true }:
            case Aisling { Map: null }:
                return [];
        }

        if (sprite.Target.Map.ID != sprite.Map.ID) return [];
        if (!sprite.WithinEarShotOf(sprite.Target)) return [];
        if (start == Vector2.Zero) return [];
        if (end == Vector2.Zero) return [];

        // Define the possible movements (up, down, left, right)  
        var movements = new[]
                {
                    new Vector2(-1, 0),
                    new Vector2(1, 0),
                    new Vector2(0, -1),
                    new Vector2(0, 1)
                };

        // Create a 2D array to store the distances from the start point  
        var distances = new float[Width, Height];
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                distances[x, y] = float.MaxValue;
            }
        }

        distances[(int)start.X, (int)start.Y] = 0;

        // Create a 2D array to store the previous node in the path  
        var previous = new Vector2?[Width, Height];

        // Create a 2D array to store the priorities of each node  
        var priorities = new float[Width, Height];

        // Calculate the priorities of each node  
        CalculateNodePriorities(sprite, start, end, priorities);

        // Create a priority queue to store the nodes to be processed  
        var queue = new PriorityQueue<Vector2, float>();
        queue.Enqueue(start, priorities[(int)start.X, (int)start.Y]);

        while (queue.Count > 0)
        {
            if (!sprite.IsAlive) break;
            switch (sprite.Target)
            {
                case null:
                case Aisling { LoggedIn: false }:
                case Aisling { IsInvisible: true }:
                case Aisling { Map: null }:
                    return [];
            }
            if (sprite.Target.Map.ID != sprite.Map.ID) return [];
            if (!sprite.WithinEarShotOf(sprite.Target)) return [];

            // Get the node with the smallest distance from the queue  
            var current = queue.Dequeue();

            // If we've reached the end point, construct the path  
            if (current == end)
            {
                var path = new List<Vector2>();
                while (current != start)
                {
                    path.Add(current);
                    current = previous[(int)current.X, (int)current.Y].GetValueOrDefault();
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            // Process the neighbors of the current node  
            foreach (var movement in movements)
            {
                var neighbor = current + movement;

                // Skip if the neighbor is out of bounds  
                var bounds = MaxVectorValuesXAndYOnMap(sprite);
                if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X > bounds.Item1 || neighbor.Y > bounds.Item2) continue;

                // Calculate the tentative distance from the start point to the neighbor  
                var distance = distances[(int)current.X, (int)current.Y] + 1;

                // If the calculated distance is smaller than the stored distance, update the distance and previous node  
                if (distance < distances[(int)neighbor.X, (int)neighbor.Y])
                {
                    distances[(int)neighbor.X, (int)neighbor.Y] = distance;
                    previous[(int)neighbor.X, (int)neighbor.Y] = current;
                    queue.Enqueue(neighbor, priorities[(int)neighbor.X, (int)neighbor.Y]);
                }
            }
        }

        // If there's no path to the end point, return an empty list  
        return [];
    }

    private void CalculateNodePriorities(Sprite sprite, Vector2 start, Vector2 end, float[,] priorities)
    {
        for (var x = 0; x < Width; x++)
        {
            for (var y = 0; y < Height; y++)
            {
                var impassable = IsAStarWall(sprite, x, y);
                var filled = IsSpriteInLocationOnWalk(sprite, x, y);
                var cost = 1;

                if (filled)
                {
                    impassable = true;
                }

                if (impassable)
                {
                    cost = 999;
                }

                var distanceFromStart = Vector2.Distance(start, new Vector2(x, y));
                var heuristic = Vector2.Distance(new Vector2(x, y), end);
                var priority = distanceFromStart + cost + heuristic;

                priorities[x, y] = priority;
            }
        }
    }

    #endregion
}