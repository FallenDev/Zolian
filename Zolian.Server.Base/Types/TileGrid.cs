using Darkages.Object;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;

using System.Numerics;

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

    public IEnumerable<Sprite> Sprites => AttemptFetchSprites();

    private IEnumerable<Sprite> AttemptFetchSprites()
    {
        const int maxAttempts = 3;
        Exception lastException = null;
        IEnumerable<Sprite> sprites = null;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                sprites = GetObjects(_map, o => o != null && (int)o.Pos.X == _x && (int)o.Pos.Y == _y && o.Alive,
                    Get.Monsters | Get.Mundanes | Get.Aislings);
                break;
            }
            catch (Exception e)
            {
                lastException = e;
            }
        }

        if (sprites != null) return sprites;
        Crashes.TrackError(lastException);
        return Enumerable.Empty<Sprite>();
    }
}