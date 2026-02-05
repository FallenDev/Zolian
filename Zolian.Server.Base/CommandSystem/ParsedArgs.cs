using System.Buffers;
using System.Globalization;

namespace Darkages.CommandSystem;

/// <summary>
/// Pooled per-invocation argument buffer.
/// IMPORTANT: ArgValue contains string refs, so we pool and clear before returning.
/// </summary>
public sealed class ParsedArgs : IDisposable
{
    private ArgValue[] _values;
    private readonly int _count;

    public ParsedArgs(int count)
    {
        _count = count;
        _values = count == 0 ? Array.Empty<ArgValue>() : ArrayPool<ArgValue>.Shared.Rent(count);

        // Initialize to Missing
        for (var i = 0; i < count; i++)
            _values[i] = ArgValue.Missing();
    }

    internal ArgValue[] Values => _values;

    public string GetString(int index) => _values[index].AsString();
    public int GetInt32(int index) => _values[index].AsInt32();
    public long GetInt64(int index) => _values[index].AsInt64();
    public double GetDouble(int index) => _values[index].AsDouble();

    public string GetStringOr(int index, string fallback)
        => _values[index].HasValue ? _values[index].AsString() : fallback;

    public int GetInt32Or(int index, int fallback)
        => _values[index].HasValue ? _values[index].AsInt32() : fallback;

    public void Dispose()
    {
        if (_values.Length == 0)
            return;

        // Clear only what we used so we don't keep string references alive.
        Array.Clear(_values, 0, _count);
        ArrayPool<ArgValue>.Shared.Return(_values);

        _values = Array.Empty<ArgValue>();
    }
}

public readonly struct ArgValue
{
    private readonly string? _string;
    private readonly long _i64;
    private readonly double _dbl;
    private readonly ArgKind _kind;

    public bool HasValue { get; }

    private ArgValue(string? s, long i64, double dbl, ArgKind kind, bool has)
    {
        _string = s;
        _i64 = i64;
        _dbl = dbl;
        _kind = kind;
        HasValue = has;
    }

    public static ArgValue FromString(string s) => new(s, 0, 0, ArgKind.String, has: true);
    public static ArgValue FromInt32(int v) => new(null, v, 0, ArgKind.Int32, has: true);
    public static ArgValue FromInt64(long v) => new(null, v, 0, ArgKind.Int64, has: true);
    public static ArgValue FromDouble(double v) => new(null, 0, v, ArgKind.Double, has: true);
    public static ArgValue Missing() => new(null, 0, 0, ArgKind.String, has: false);

    public string AsString()
    {
        if (!HasValue) throw new InvalidOperationException("Argument not provided.");
        return _kind switch
        {
            ArgKind.String => _string ?? string.Empty,
            ArgKind.Int32 => _i64.ToString(CultureInfo.InvariantCulture),
            ArgKind.Int64 => _i64.ToString(CultureInfo.InvariantCulture),
            ArgKind.Double => _dbl.ToString(CultureInfo.InvariantCulture),
            _ => _string ?? string.Empty
        };
    }

    public int AsInt32()
    {
        if (!HasValue) throw new InvalidOperationException("Argument not provided.");
        return _kind switch
        {
            ArgKind.Int32 => checked((int)_i64),
            ArgKind.Int64 => checked((int)_i64),
            ArgKind.String => int.Parse(_string ?? "0", CultureInfo.InvariantCulture),
            ArgKind.Double => checked((int)_dbl),
            _ => 0
        };
    }

    public long AsInt64()
    {
        if (!HasValue) throw new InvalidOperationException("Argument not provided.");
        return _kind switch
        {
            ArgKind.Int32 => _i64,
            ArgKind.Int64 => _i64,
            ArgKind.String => long.Parse(_string ?? "0", CultureInfo.InvariantCulture),
            ArgKind.Double => checked((long)_dbl),
            _ => 0
        };
    }

    public double AsDouble()
    {
        if (!HasValue) throw new InvalidOperationException("Argument not provided.");
        return _kind switch
        {
            ArgKind.Double => _dbl,
            ArgKind.Int32 => _i64,
            ArgKind.Int64 => _i64,
            ArgKind.String => double.Parse(_string ?? "0", CultureInfo.InvariantCulture),
            _ => 0
        };
    }
}