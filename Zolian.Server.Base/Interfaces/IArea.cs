using System.Collections.Concurrent;
using System.Numerics;
using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IArea
{
    int MiningNodes { get; set; }
    TileGrid[,] ObjectGrid { get; set; }
    TileContent[,] TileContent { get; set; }
    Tuple<string, AreaScript> Script { get; set; }
    string FilePath { get; set; }

    Vector2 GetPosFromLoc(Vector2 location);
    bool IsLocationOnMap(Sprite sprite);
    byte[] GetRowData(int row);
    bool IsWall(int x, int y);
    bool IsAStarWall(Sprite sprite, int x, int y);
    bool IsAStarSprite(Sprite sprite, int x, int y);
    bool IsSpriteInLocationOnCreation(Sprite sprite, int x, int y);
    bool OnLoaded();
    bool ParseMapWalls(short lWall, short rWall);
    void Update(in TimeSpan elapsedTime);
    Task<List<Vector2>> GetPath(Monster sprite, Vector2 start, Vector2 end);
    void CheckDirectionOfNode(IReadOnlyList<IList<TileGrid>> masterGrid, List<TileGrid> viewable,
        ICollection<TileGrid> used);
    void SetAStarNode(List<TileGrid> viewable, TileGrid nextNode, Vector2 nextParent, float d, float distanceMultiply);
    void SetAStarNodeInsert(List<TileGrid> list, TileGrid newNode);
}