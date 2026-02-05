namespace Darkages.CommandSystem;

public readonly record struct CommandDefinition(
    string Name,
    string[] Aliases,
    int AccessLevel,
    ArgSpec[] ArgSpecs,
    CommandHandler Handler,
    Func<object?, string>? CanExecute = null,
    string? Description = null);

public readonly record struct ArgSpec(
    string Name,
    ArgKind Kind,
    bool Optional = false,
    string? Default = null,
    Func<ReadOnlySpan<char>, bool>? Validator = null);

public enum ArgKind
{
    String,
    Int32,
    Int64,
    Double
}

public delegate void CommandHandler(object? ctx, ParsedArgs args);