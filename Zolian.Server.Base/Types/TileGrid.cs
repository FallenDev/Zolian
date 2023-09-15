using System.Numerics;

using Darkages.Object;
using Darkages.Sprites;
using Microsoft.AppCenter.Crashes;

namespace Darkages.Types;

public class TileGrid : ObjectManager
{
    private readonly Area _map;
    private readonly int _x;
    private readonly int _y;
    public readonly bool Impassable;
    public bool HasBeenUsed, IsViewable;
    public float FScore;
    public readonly float Cost;
    public float CurrentDist;
    public Vector2 Parent, Pos;

    public TileGrid(Area map, int x, int y)
    {
        _map = map;
        _x = x;
        _y = y;
        Impassable = false;
        HasBeenUsed = false;
        IsViewable = false;
        Cost = 1.0f;
    }

    public TileGrid(float cost)
    {
        Cost = cost;
        HasBeenUsed = false;
        IsViewable = false;
    }

    public TileGrid(Vector2 pos, float cost, bool filled, float fScore)
    {
        Cost = cost;
        Impassable = filled;
        HasBeenUsed = false;
        IsViewable = false;

        Pos = pos;
        FScore = fScore;
    }

    public void SetNode(Vector2 parent, float fScore, float currentDist)
    {
        Parent = parent;
        FScore = fScore;
        CurrentDist = currentDist;
    }

    public IEnumerable<Sprite> Sprites
    {
        get
        {
            const int maxAttempts = 3;
            Exception lastException = null;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    return GetObjects(_map, o => (int)o.Pos.X == _x && (int)o.Pos.Y == _y && o.Alive,
                        Get.Monsters | Get.Mundanes | Get.Aislings);
                }
                catch (Exception e)
                {
                    lastException = e;
                }
            }

            Crashes.TrackError(lastException);
            return null;
        }
    }

    public List<Sprite> SpritesList
    {
        get
        {
            const int maxAttempts = 3;
            Exception lastException = null;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    return GetObjects(_map, o => (int)o.Pos.X == _x && (int)o.Pos.Y == _y && o.Alive,
                        Get.Monsters | Get.Mundanes | Get.Aislings).ToList();
                }
                catch (Exception e)
                {
                    lastException = e;
                }
            }

            Crashes.TrackError(lastException);
            return null;
        }
    }
}