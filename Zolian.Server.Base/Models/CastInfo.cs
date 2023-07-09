using Darkages.Types;

namespace Darkages.Models;

public class CastInfo
{
    public byte Slot;
    public byte SpellLines;
    public DateTime Started;

    public string Data { get; set; }
    public Position Position { get; set; }
    public uint Target { get; set; }
}