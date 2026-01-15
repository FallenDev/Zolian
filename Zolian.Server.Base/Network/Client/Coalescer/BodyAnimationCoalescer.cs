using System.Collections.Concurrent;

using Chaos.Networking.Entities.Server;

namespace Darkages.Network.Client.Coalescer;

public sealed class BodyAnimationCoalescer : IDisposable
{
    private readonly Action<BodyAnimationArgs> _sendImmediate;
    private readonly int _windowMs;
    private readonly int _maxPerWindow;

    // Latest-wins per SourceId
    private readonly ConcurrentDictionary<uint, BodyAnimationArgs> _pending = new();

    private int _flushScheduled; // 0/1
    private CancellationTokenSource? _cts;

    public BodyAnimationCoalescer(Action<BodyAnimationArgs>? sendImmediate, int windowMs = 50, int maxPerWindow = 32)
    {
        // Default no-op
        sendImmediate ??= static _ => { };

        _sendImmediate = sendImmediate;
        _windowMs = windowMs <= 0 ? 30 : windowMs;
        _maxPerWindow = maxPerWindow <= 0 ? 64 : maxPerWindow;
    }

    public void Enqueue(BodyAnimationArgs args)
    {
        // Dedupe: if the newest is identical to what we already have pending for this SourceId, kick out
        if (_pending.TryGetValue(args.SourceId, out var existing) && AreEquivalent(existing, args))
            return;

        // Latest wins for this SourceId
        _pending[args.SourceId] = args;

        // Schedule one flush task per window
        if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
        {
            _cts ??= new CancellationTokenSource();
            _ = FlushAfterDelayAsync(_cts.Token);
        }
    }

    private static bool AreEquivalent(BodyAnimationArgs a, BodyAnimationArgs b)
        => a.BodyAnimation == b.BodyAnimation
           && a.AnimationSpeed == b.AnimationSpeed
           && a.Sound == b.Sound;

    private async Task FlushAfterDelayAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(_windowMs, ct).ConfigureAwait(false);

            // Send at most _maxPerWindow unique SourceIds this tick
            var sent = 0;

            foreach (var kv in _pending)
            {
                if (sent >= _maxPerWindow)
                    break;

                if (_pending.TryRemove(kv.Key, out var args))
                {
                    _sendImmediate(args);
                    sent++;
                }
            }

            // Hard cap protection: drop any remaining this window
            if (!_pending.IsEmpty)
                _pending.Clear();
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Allow another window to schedule
            Interlocked.Exchange(ref _flushScheduled, 0);

            // If more arrived after finished, schedule again
            if (!_pending.IsEmpty && Interlocked.Exchange(ref _flushScheduled, 1) == 0)
            {
                _ = FlushAfterDelayAsync(ct);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _pending.Clear();
    }
}
