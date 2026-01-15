using System.Collections.Concurrent;

namespace Darkages.Network.Client.Coalescer;

public sealed class SoundCoalescer : IDisposable
{
    private readonly Action<byte, bool> _sendImmediate;
    private readonly int _windowMs;
    private readonly int _maxUniquePerWindow;

    // Key = sound byte (SFX only); Value = sound
    private readonly ConcurrentDictionary<int, byte> _pending = new();

    private int _flushScheduled;
    private CancellationTokenSource? _cts;

    public SoundCoalescer(Action<byte, bool> sendImmediate, int windowMs = 50, int maxUniquePerWindow = 32)
    {
        // Default no-op
        sendImmediate ??= (byte _, bool __) => { };

        _sendImmediate = sendImmediate;
        _windowMs = windowMs <= 0 ? 50 : windowMs;
        _maxUniquePerWindow = maxUniquePerWindow is <= 0 or > 256 ? 32 : maxUniquePerWindow;
    }

    public void Enqueue(byte sound, bool isMusic)
    {
        // Music is not coalesced
        if (isMusic)
        {
            _sendImmediate(sound, true);
            return;
        }

        // Dedupe within the window
        _pending.TryAdd(sound, sound);

        // Schedule one flush task per window.
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
            Span<byte> sentMap = stackalloc byte[256];

            foreach (var kv in _pending)
            {
                // Max unique per window reached
                if (sent >= _maxUniquePerWindow)
                    break;

                // kv.Key is int but should be [0..255] byte
                var key = kv.Key;
                if ((uint)key > 255u)
                    continue;

                if (sentMap[key] != 0)
                    continue;

                if (_pending.TryRemove(key, out var sfx))
                {
                    sentMap[key] = 1;
                    _sendImmediate(sfx, false);
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
