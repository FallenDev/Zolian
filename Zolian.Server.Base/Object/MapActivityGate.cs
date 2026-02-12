using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Darkages.Types;

namespace Darkages.Object;

public static class MapActivityGate
{
    // 30 minutes in milliseconds
    private const long InactivityWindowMs = 30L * 60L * 1000L;

    // MapId -> last time we saw at least 1 player on the map (Environment.TickCount64)
    private static readonly ConcurrentDictionary<int, long> _lastSeenPlayerMs = new();

    /// <summary>
    /// Returns true if monsters should update for this map:
    /// - Players currently on map => true, refresh lastSeen
    /// - No players, but players were seen within window => true
    /// - No players and never seen => false
    /// - No players and window expired => false
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldUpdateMonsters(Area map)
    {
        if (map is null) return false;

        var now = Environment.TickCount64;

        if (map.HasPlayers)
        {
            _lastSeenPlayerMs[map.ID] = now;
            return true;
        }

        return _lastSeenPlayerMs.TryGetValue(map.ID, out var lastSeen)
            && (now - lastSeen) < InactivityWindowMs;
    }

    /// <summary>
    /// Unload / Reload maps - Use for quests or other special events where we want to reset the map state (rifts)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(int mapId) => _lastSeenPlayerMs.TryRemove(mapId, out _);

    /// <summary>
    /// Clear all state (server restart)
    /// </summary>
    public static void ClearAll() => _lastSeenPlayerMs.Clear();
}