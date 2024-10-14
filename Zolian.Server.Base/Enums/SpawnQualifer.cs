
namespace Darkages.Enums;

[Flags]
public enum SpawnQualifer
{
    Random = 1 << 1,
    Defined = 1 << 2,
    Event = 1 << 3,
    Summoned = 1 << 4
}