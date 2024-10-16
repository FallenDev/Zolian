﻿using Darkages.Enums;
using Darkages.Object;
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.Models;

public class Map : ObjectManager
{
    public byte Width { get; init; }
    public MapFlags Flags { get; set; }
    public int Music { get; init; }
    public string Name { get; init; }
    public byte Height { get; init; }
    public string ScriptKey { get; init; }
    public MiningNodes MiningNodes { get; init; }
    public WildFlowers WildFlowers { get; set; }
    public int ID { get; init; }
}

public class DiscoveredMap
{
    public int Serial { get; init; }
    public int MapId { get; init; }
}