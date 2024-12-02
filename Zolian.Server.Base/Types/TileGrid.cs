using Darkages.Object;
using System.Numerics;

namespace Darkages.Types;

public class TileGrid : ObjectManager
{
    public readonly bool Impassable;
    public bool HasBeenUsed, IsViewable;
    public float FScore;
    public readonly float Cost;
    public float CurrentDist;
    public Vector2 Parent, Pos;
    public DateTime LastDoorClicked { get; set; } = DateTime.UtcNow;

    public TileGrid()
    {
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

    public bool ShouldRegisterClick
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime - LastDoorClicked > new TimeSpan(0, 0, 0, 1, 500);
        }
    }
}