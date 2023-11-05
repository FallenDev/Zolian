using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using System.Numerics;

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
    IEnumerable<byte> GetRowData(int row);
    bool IsWall(int x, int y);
    bool IsAStarWall(Sprite sprite, int x, int y);
    bool IsSpriteInLocationOnWalk(Sprite sprite, int x, int y);
    bool IsSpriteInLocationOnCreation(Sprite sprite, int x, int y);
    bool OnLoaded();
    bool ParseMapWalls(int lWall, int rWall);
    void Update(in TimeSpan elapsedTime);
    Task<IList<Vector2>> GetPath(Monster sprite, Vector2 start, Vector2 end);
    void CheckDirectionOfNode(IReadOnlyList<IList<TileGrid>> masterGrid, IList<TileGrid> viewable,
        ICollection<TileGrid> used);
    void SetAStarNode(IList<TileGrid> viewable, TileGrid nextNode, Vector2 nextParent, float d, float distanceMultiply);
    void SetAStarNodeInsert(IList<TileGrid> list, TileGrid newNode);
}