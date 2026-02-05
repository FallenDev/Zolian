using System.Buffers;
using System.Collections.Frozen;

namespace Darkages.CommandSystem;

/// <summary>
/// Immutable, thread-safe command system:
/// - Command definitions are frozen (no mutation during parsing)
/// - Arguments are parsed into a per-invocation buffer (no shared state)
/// - Alias lookup is O(1) via FrozenDictionary
/// - Tokenization supports quotes
/// </summary>
public sealed class CommandSystemCore
{
    private readonly FrozenDictionary<string, CommandDefinition> _byAlias;
    private readonly string _prefix;
    private readonly Action<object?, string> _onError;

    private CommandSystemCore(FrozenDictionary<string, CommandDefinition> byAlias, string prefix, Action<object?, string> onError)
    {
        _byAlias = byAlias;
        _prefix = prefix;
        _onError = onError;
    }

    public static Builder CreateBuilder(string prefix = "/", Action<object?, string>? onError = null)
        => new(prefix, onError ?? ((_, _) => { }));

    public IReadOnlyCollection<CommandDefinition> Definitions
        => _byAlias.Values.Distinct().ToArray();

    public bool TryParseAndExecute(string input, object? context, int accessLevel = 0)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        if (!string.IsNullOrEmpty(_prefix))
        {
            if (!input.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            input = input[_prefix.Length..].TrimStart();
            if (input.Length == 0)
                return false;
        }

        var span = input.AsSpan();

        // Tokenize (ArrayPool to avoid stack/span escape restrictions)
        Token[]? tokenArray = null;
        int tokenCount;

        try
        {
            tokenArray = ArrayPool<Token>.Shared.Rent(16);

            // NOTE: Tokenizer may grow the array; pass by ref so we keep the correct reference.
            tokenCount = Tokenizer.Tokenize(span, ref tokenArray);

            if (tokenCount <= 0)
                return true;

            var alias = tokenArray[0].Slice(span).ToString().ToLowerInvariant();

            if (!_byAlias.TryGetValue(alias, out var command))
            {
                _onError(context, $"Command '{alias}' not found.");
                Suggest(alias, context);
                return true;
            }

            if (command.AccessLevel > accessLevel)
            {
                _onError(context,
                    $"Command '{command.Name}' requires permission level {command.AccessLevel}. (Currently {accessLevel})");
                return true;
            }

            var canExecuteError = command.CanExecute?.Invoke(context) ?? string.Empty;
            if (!string.IsNullOrEmpty(canExecuteError))
            {
                _onError(context, canExecuteError);
                return true;
            }

            var argsStart = 1;
            var argsCount = Math.Max(0, tokenCount - 1);

            using var parsed = new ParsedArgs(command.ArgSpecs.Length);

            if (!ArgumentParser.TryParse(command, span, tokenArray, argsStart, argsCount, parsed, _onError, context))
                return true;

            try
            {
                command.Handler(context, parsed);
            }
            catch (Exception ex)
            {
                _onError(context, $"Command '{command.Name}' crashed: {ex.GetType().Name}: {ex.Message}");
            }

            return true;
        }
        finally
        {
            if (tokenArray is not null)
                ArrayPool<Token>.Shared.Return(tokenArray);
        }
    }

    private void Suggest(string alias, object? ctx)
    {
        var suggestions = new List<string>(5);

        foreach (var kvp in _byAlias)
        {
            var a = kvp.Key;
            if (a.StartsWith(alias, StringComparison.OrdinalIgnoreCase) ||
                a.Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add(a);
                if (suggestions.Count == 5) break;
            }
        }

        if (suggestions.Count > 0)
            _onError(ctx, $"Did you mean: {string.Join(", ", suggestions)}?");
    }

    public sealed class Builder
    {
        private readonly Dictionary<string, CommandDefinition> _byAlias = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _prefix;
        private readonly Action<object?, string> _onError;

        internal Builder(string prefix, Action<object?, string> onError)
        {
            _prefix = prefix;
            _onError = onError;
        }

        public Builder Add(CommandDefinition command)
        {
            foreach (var a in command.Aliases)
            {
                var key = a.ToLowerInvariant();
                if (_byAlias.TryGetValue(key, out var existing))
                    throw new InvalidOperationException($"Alias '{a}' is already registered to '{existing.Name}'.");

                _byAlias[key] = command;
            }

            return this;
        }

        public CommandSystemCore Build()
        {
            var frozen = _byAlias.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            return new CommandSystemCore(frozen, _prefix, _onError);
        }
    }
}