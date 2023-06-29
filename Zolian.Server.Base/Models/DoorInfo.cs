namespace Darkages.Models;

public sealed record DoorInfo
{
    /// <summary>
    ///     Whether or not the door is closed
    /// </summary>
    public bool Closed { get; set; }
    /// <summary>
    ///     Whether or not the door opens to the right
    /// </summary>
    public bool OpenRight { get; set; }
    /// <summary>
    ///     The X coordinate of the door
    /// </summary>
    public int X { get; set; }
    /// <summary>
    ///     The Y coordinate of the door
    /// </summary>
    public int Y { get; set; }
}