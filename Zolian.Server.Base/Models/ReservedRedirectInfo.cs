using System.Numerics;

namespace Darkages.Models;

// Cannot make abstract due to compiler error
public record ReservedRedirectInfo : IEqualityOperators<ReservedRedirectInfo, ReservedRedirectInfo, bool>
{
    public byte Id { get; set; }
    public string Name { get; set; }
}