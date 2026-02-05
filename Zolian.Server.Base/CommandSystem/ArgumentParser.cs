using System.Globalization;

namespace Darkages.CommandSystem;

internal static class ArgumentParser
{
    public static bool TryParse(
        CommandDefinition command,
        ReadOnlySpan<char> input,
        Token[] tokens,
        int argsStart,
        int argsCount,
        ParsedArgs parsed,
        Action<object?, string> onError,
        object? ctx)
    {
        var specs = command.ArgSpecs;
        var values = parsed.Values;

        // Too many args?
        if (argsCount > specs.Length)
        {
            onError(ctx, $"Too many arguments. Usage: {Usage(command)}");
            return false;
        }

        for (int i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];

            if (i >= argsCount)
            {
                if (spec.Optional)
                {
                    if (spec.Default is not null)
                    {
                        if (!TryCoerce(spec, spec.Default.AsSpan(), out values[i], onError, ctx))
                            return false;
                    }
                    continue;
                }

                onError(ctx, $"Missing required argument '{spec.Name}'. Usage: {Usage(command)}");
                return false;
            }

            var raw = tokens[argsStart + i].Slice(input);
            raw = Tokenizer.Unquote(raw);

            if (spec.Validator is not null && !spec.Validator(raw))
            {
                onError(ctx, $"Argument '{spec.Name}' is invalid.");
                return false;
            }

            if (!TryCoerce(spec, raw, out values[i], onError, ctx))
                return false;
        }

        return true;
    }

    private static bool TryCoerce(
        ArgSpec spec,
        ReadOnlySpan<char> raw,
        out ArgValue value,
        Action<object?, string> onError,
        object? ctx)
    {
        switch (spec.Kind)
        {
            case ArgKind.String:
                value = ArgValue.FromString(raw.ToString());
                return true;

            case ArgKind.Int32:
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32))
                {
                    value = ArgValue.FromInt32(i32);
                    return true;
                }
                onError(ctx, $"Argument '{spec.Name}' must be an integer.");
                value = default;
                return false;

            case ArgKind.Int64:
                if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i64))
                {
                    value = ArgValue.FromInt64(i64);
                    return true;
                }
                onError(ctx, $"Argument '{spec.Name}' must be a 64-bit integer.");
                value = default;
                return false;

            case ArgKind.Double:
                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var dbl))
                {
                    value = ArgValue.FromDouble(dbl);
                    return true;
                }
                onError(ctx, $"Argument '{spec.Name}' must be a number.");
                value = default;
                return false;

            default:
                value = ArgValue.FromString(raw.ToString());
                return true;
        }
    }

    private static string Usage(CommandDefinition cmd)
    {
        var alias = cmd.Aliases.Length > 0 ? cmd.Aliases[0] : cmd.Name;

        if (cmd.ArgSpecs.Length == 0)
            return alias;

        var parts = new string[cmd.ArgSpecs.Length];
        for (int i = 0; i < cmd.ArgSpecs.Length; i++)
        {
            var a = cmd.ArgSpecs[i];
            parts[i] = a.Optional ? $"[{a.Name}]" : $"<{a.Name}>";
        }

        return $"{alias} {string.Join(' ', parts)}";
    }
}