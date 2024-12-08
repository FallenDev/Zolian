using System.Numerics;

namespace Darkages.Models;

public abstract record ReservedRedirectInfo : IEqualityOperators<ReservedRedirectInfo, ReservedRedirectInfo, bool>
{
    public byte Id { get; set; }
    public string Name { get; set; }
}