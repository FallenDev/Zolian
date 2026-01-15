using System.Collections.Concurrent;

using Chaos.Networking.Entities.Server;

namespace Darkages.Network.Client;

public sealed class HealthBarCoalescer : IDisposable
{
    private readonly Action<HealthBarArgs> _sendImmediate;
    private readonly int _windowMs;
    private readonly int _maxPerFlush;

    // Latest-wins per serial (SourceId)
    private readonly ConcurrentDictionary<uint, HealthBarArgs> _pending = new();

    private int _flushScheduled;
    private CancellationTokenSource? _cts;

    /// <sumary>
    /// Health bar coalescer to reduce UI client spam
    /// </sumary> 
    public HealthBarCoalescer(Action<HealthBarArgs>? sendImmediate, int windowMs = 50, int maxPerFlush = 32)
    {
        // Never crash the server over UI packets
        sendImmediate ??= static _ => { };

        _sendImmediate = sendImmediate;
        _windowMs = windowMs <= 0 ? 50 : windowMs;
        _maxPerFlush = maxPerFlush <= 0 ? 32 : maxPerFlush;
    }

    public void Enqueue(HealthBarArgs args)
    {
        // Newest wins for this serial
        _pending[args.SourceId] = args;

        // Schedule one flush task per window
        if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
        {
            _cts ??= new CancellationTokenSource();
            _ = FlushAfterDelayAsync(_cts.Token);
        }
    }

    private async Task FlushAfterDelayAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(_windowMs, ct).ConfigureAwait(false);

            var sent = 0;

            // Send at most one per serial (dictionary holds only latest per serial)
            foreach (var kv in _pending)
            {
                // Limit per flush window
                if (sent >= _maxPerFlush)
                    break;

                if (_pending.TryRemove(kv.Key, out var args))
                {
                    _sendImmediate(args);
                    sent++;
                }
            }

            // Hard cap protection: drop anything still pending this window
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
