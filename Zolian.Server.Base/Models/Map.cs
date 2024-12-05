using Darkages.Enums;
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.Models;

public class Map
{
    public int ID { get; init; }
    public string Name { get; init; }
    public byte Width { get; init; }
    public byte Height { get; init; }
    public string ScriptKey { get; init; }
    public MapFlags Flags { get; set; }
    public int Music { get; init; }
    public MiningNodes MiningNodes { get; init; }
    public WildFlowers WildFlowers { get; set; }
    public TileContent[,] TileContent { get; set; }
}

public class DiscoveredMap
{
    public int Serial { get; init; }
    public int MapId { get; init; }
}