using Darkages.Enums;
using Darkages.Object;

namespace Darkages.Models;

public class Map : ObjectManager
{
    public ushort Cols { get; init; }
    public MapFlags Flags { get; set; }
    public int Music { get; init; }
    public string Name { get; init; }
    public ushort Rows { get; init; }
    public string ScriptKey { get; init; }
    public int ID { get; set; }
}

public class DiscoveredMap
{
    public int DmId { get; init; }
    public int Serial { get; init; }
    public int MapId { get; init; }
}