using System.Numerics;

namespace Darkages.Common;

/// <summary>
/// Generates short-lived random numeric IDs and keeps a small history
/// to greatly reduce the chance of immediate repeats.
///
/// Supports ONLY integral types:
/// sbyte, byte, short, ushort, int, uint, long, ulong.
/// </summary>
/// <typeparam name="T">The numeric type to generate.</typeparam>
public sealed class EphemeralRandomIdGenerator<T> where T : INumber<T>
{
    private const int HistorySize = ushort.MaxValue;
    private readonly T[] _history = new T[HistorySize];
    private int _index;
    private static readonly Func<T> _nextRandom = CreateGenerator();
    private static readonly EqualityComparer<T> _cmp = EqualityComparer<T>.Default;

    /// <summary>
    /// Shared singleton instance
    /// </summary>
    public static EphemeralRandomIdGenerator<T> Shared { get; } = new();

    /// <summary>
    /// Returns the next random ID
    /// </summary>
    public T NextId
    {
        get
        {
            T id;

            // Avoid immediate repeats within the last HistorySize values.
            do
            {
                id = _nextRandom();
            } while (IsDuplicate(id));

            var slot = Interlocked.Increment(ref _index) % HistorySize;
            _history[slot] = id;

            return id;
        }
    }

    private bool IsDuplicate(T value)
    {
        for (var i = 0; i < HistorySize; i++)
        {
            if (_cmp.Equals(_history[i], value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Build a per-type random generator once, then reuse.
    /// </summary>
    private static Func<T> CreateGenerator()
    {
        var t = typeof(T);

        return t switch
        {
            // signed
            var _ when t == typeof(sbyte) => () =>
            {
                var value = (sbyte)Random.Shared.Next(sbyte.MinValue, sbyte.MaxValue + 1);
                return (T)(object)value;
            }
            ,

            var _ when t == typeof(short) => () =>
            {
                var value = (short)Random.Shared.Next(short.MinValue, short.MaxValue + 1);
                return (T)(object)value;
            }
            ,

            var _ when t == typeof(int) => () =>
            {
                var value = Random.Shared.Next(int.MinValue, int.MaxValue); // max is exclusive
                return (T)(object)value;
            }
            ,

            var _ when t == typeof(long) => () =>
            {
                var value = Random.Shared.NextInt64(long.MinValue, long.MaxValue);
                return (T)(object)value;
            }
            ,

            // unsigned
            var _ when t == typeof(byte) => () =>
            {
                var value = (byte)Random.Shared.Next(0, byte.MaxValue + 1);
                return (T)(object)value;
            }
            ,

            var _ when t == typeof(ushort) => () =>
            {
                var value = (ushort)Random.Shared.Next(0, ushort.MaxValue + 1);
                return (T)(object)value;
            }
            ,

            var _ when t == typeof(uint) => () =>
            {
                var value = (uint)Random.Shared.NextInt64(0, (long)uint.MaxValue + 1);
                return (T)(object)value;
            }
            ,

            var _ when t == typeof(ulong) => () =>
            {
                // Build a full 64-bit value from two 32-bit chunks.
                var hi = (ulong)(uint)Random.Shared.NextInt64(0, (long)uint.MaxValue + 1);
                var lo = (ulong)(uint)Random.Shared.NextInt64(0, (long)uint.MaxValue + 1);
                var value = (hi << 32) | lo;
                return (T)(object)value;
            }
            ,

            _ => throw new NotSupportedException(
                $"EphemeralRandomIdGenerator<{t.Name}> supports only integral types " +
                "(sbyte, byte, short, ushort, int, uint, long, ulong).")
        };
    }
}
