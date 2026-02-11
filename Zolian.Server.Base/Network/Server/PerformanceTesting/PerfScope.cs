using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Darkages.Network.Server.PerformanceTesting;

/// <summary>
/// Use: 
/// using var _ = new PerfScope("MethodName");
/// MethodName
/// </summary>
public readonly ref struct PerfScope
{
    private readonly string _name;
    private readonly long _start;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PerfScope(string name)
    {
        _name = name;
        _start = Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        var elapsed = Stopwatch.GetTimestamp() - _start;
        PerfAggregator.Record(_name, elapsed);
    }
}
