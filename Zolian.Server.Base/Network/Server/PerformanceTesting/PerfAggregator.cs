using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Darkages.Network.Server.PerformanceTesting;

public static class PerfAggregator
{
    private sealed class Stats
    {
        public long Count;
        public long TotalTicks;
        public long MinTicks = long.MaxValue;
        public long MaxTicks;
    }

    private static readonly ConcurrentDictionary<string, Stats> _stats = new();
    private static long _lastReportTimestamp;
    private static int _g0, _g1, _g2;
    private static long _lastAllocated;
    private const int ReportIntervalSeconds = 5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Record(string name, long ticks)
    {
        var stats = _stats.GetOrAdd(name, static _ => new Stats());

        Interlocked.Increment(ref stats.Count);
        Interlocked.Add(ref stats.TotalTicks, ticks);

        UpdateMin(ref stats.MinTicks, ticks);
        UpdateMax(ref stats.MaxTicks, ticks);

        TryReport();
    }

    private static void TryReport()
    {
        var now = Stopwatch.GetTimestamp();
        var last = Volatile.Read(ref _lastReportTimestamp);

        if ((now - last) < Stopwatch.Frequency * ReportIntervalSeconds)
            return;

        if (Interlocked.CompareExchange(ref _lastReportTimestamp, now, last) != last)
            return;

        Report();
        _stats.Clear();
    }

    private static void Report()
    {
        foreach (var (name, s) in _stats)
        {
            var count = Volatile.Read(ref s.Count);
            if (count == 0)
                continue;

            var total = Volatile.Read(ref s.TotalTicks);
            var min = Volatile.Read(ref s.MinTicks);
            var max = Volatile.Read(ref s.MaxTicks);

            var avgTicks = total / count;

            var toMs = 1000.0 / Stopwatch.Frequency;

            Console.WriteLine(
                $"[PERF] {name} | " +
                $"Calls: {count} | " +
                $"Avg: {avgTicks * toMs:0.000} ms | " +
                $"Min: {min * toMs:0.000} ms | " +
                $"Max: {max * toMs:0.000} ms"
            );

            // Report GC
            ReportGcDelta();
            // Report Allocations
            //ReportAllocDelta();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateMin(ref long location, long value)
    {
        long current;
        while ((current = Volatile.Read(ref location)) > value &&
               Interlocked.CompareExchange(ref location, value, current) != current)
        {
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateMax(ref long location, long value)
    {
        long current;
        while ((current = Volatile.Read(ref location)) < value &&
               Interlocked.CompareExchange(ref location, value, current) != current)
        {
        }
    }

    static void ReportGcDelta()
    {
        var g0 = GC.CollectionCount(0);
        var g1 = GC.CollectionCount(1);
        var g2 = GC.CollectionCount(2);

        Console.WriteLine($"[PERF] GC Δ | Gen0: {g0 - _g0} Gen1: {g1 - _g1} Gen2: {g2 - _g2}");

        _g0 = g0; _g1 = g1; _g2 = g2;
    }

    private static void ReportAllocDelta()
    {
        var now = GC.GetTotalAllocatedBytes(precise: false);
        var delta = now - _lastAllocated;
        _lastAllocated = now;

        Console.WriteLine($"[PERF] Alloc Δ | {delta:n0} bytes");
    }
}
