using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class UpdateGroundObjectsComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 60000;

    protected internal override async Task Update()
    {
        const int tickMs = ComponentSpeed;
        const int jitterMs = 5000;

        var lastTick = Stopwatch.GetTimestamp();

        while (ServerSetup.Instance.Running)
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(lastTick, now);

            if (elapsed.TotalMilliseconds < tickMs)
            {
                var remaining = tickMs - (int)elapsed.TotalMilliseconds;

                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, jitterMs)).ConfigureAwait(false);

                continue;
            }

            UpdateGroundItems();
            UpdateGroundMoney();

            lastTick = Stopwatch.GetTimestamp();
        }
    }

    private static void UpdateGroundItems()
    {
        try
        {
            var now = DateTime.UtcNow;

            foreach (var mapKvp in ServerSetup.Instance.GlobalMapCache)
            {
                var map = mapKvp.Value;

                // Existing API returns a new dictionary; iterate it directly (no SelectMany/ToArray).
                var itemsById = ObjectManager.GetObjects<Item>(map, i => i.ItemPane == Item.ItemPanes.Ground);
                if (itemsById.IsEmpty)
                    continue;

                foreach (var kv in itemsById)
                {
                    var item = kv.Value;
                    if (item is null)
                        continue;

                    var abandonedDiff = now - item.AbandonedDate;

                    // For corpses: remove if abandoned for more than 3 minutes
                    if (abandonedDiff.TotalMinutes > 3 && item.Template.Name == "Corpse")
                    {
                        item.Remove();
                        continue;
                    }

                    if (abandonedDiff.TotalMinutes > 30)
                        item.Remove();
                }
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void UpdateGroundMoney()
    {
        try
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in ServerSetup.Instance.GlobalGroundMoneyCache)
            {
                var money = kvp.Value;
                if (money is null)
                    continue;

                var abandonedDiff = now - money.AbandonedDate;
                if (abandonedDiff.TotalMinutes <= 30)
                    continue;

                if (ServerSetup.Instance.GlobalGroundMoneyCache.TryRemove(money.MoneyId, out _))
                    money.Remove();
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}