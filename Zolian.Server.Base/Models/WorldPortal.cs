namespace Darkages.Models;

public class WorldPortal
{
    public Warp Destination { get; init; }

    public string DisplayName { get; init; }

    public short PointX { get; init; }

    public short PointY { get; init; }
}