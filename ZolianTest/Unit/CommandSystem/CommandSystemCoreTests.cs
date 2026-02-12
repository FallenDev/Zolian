using System;
using System.Collections.Generic;

using Darkages.CommandSystem;

using Xunit;

namespace ZolianTest.Unit.CommandSystem;

public sealed class CommandSystemCoreTests
{
    [Fact]
    public void TryParseAndExecute_IgnoresEmptyOrWhitespace()
    {
        var errors = new List<string>();
        var cs = CommandSystemCore.CreateBuilder(prefix: "/", onError: (_, msg) => errors.Add(msg)).Build();

        Assert.False(cs.TryParseAndExecute("", context: null));
        Assert.False(cs.TryParseAndExecute("   ", context: null));
        Assert.Empty(errors);
    }

    [Fact]
    public void TryParseAndExecute_RequiresPrefix_WhenPrefixConfigured()
    {
        var called = false;
        var cs = CommandSystemCore.CreateBuilder(prefix: "/")
            .Add(new CommandDefinition(
                Name: "ping",
                Aliases: ["ping"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: (_, _) => called = true))
            .Build();

        Assert.False(cs.TryParseAndExecute("ping", context: null));
        Assert.False(called);

        Assert.True(cs.TryParseAndExecute("/ping", context: null));
        Assert.True(called);
    }

    [Fact]
    public void TryParseAndExecute_FindsCommand_CaseInsensitive()
    {
        var called = false;
        var cs = CommandSystemCore.CreateBuilder(prefix: "/")
            .Add(new CommandDefinition(
                Name: "who",
                Aliases: ["who"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: (_, _) => called = true))
            .Build();

        Assert.True(cs.TryParseAndExecute("/WHO", context: null));
        Assert.True(called);
    }

    [Fact]
    public void TryParseAndExecute_ParsesQuotedArgs_AndUnquotes()
    {
        var got = "";
        var cs = CommandSystemCore.CreateBuilder(prefix: "/")
            .Add(new CommandDefinition(
                Name: "say",
                Aliases: ["say"],
                AccessLevel: 0,
                ArgSpecs: [new ArgSpec("msg", ArgKind.String)],
                Handler: (_, args) => got = args.GetString(0)))
            .Build();

        Assert.True(cs.TryParseAndExecute("/say \"hello world\"", context: null));
        Assert.Equal("hello world", got);
    }

    [Fact]
    public void TryParseAndExecute_ValidatesNumericArgs()
    {
        int got = 0;
        var errors = new List<string>();

        var cs = CommandSystemCore.CreateBuilder(prefix: "/", onError: (_, msg) => errors.Add(msg))
            .Add(new CommandDefinition(
                Name: "add",
                Aliases: ["add"],
                AccessLevel: 0,
                ArgSpecs: [new ArgSpec("x", ArgKind.Int32)],
                Handler: (_, args) => got = args.GetInt32(0)))
            .Build();

        Assert.True(cs.TryParseAndExecute("/add 123", context: null));
        Assert.Equal(123, got);

        got = 0;
        errors.Clear();
        Assert.True(cs.TryParseAndExecute("/add nope", context: null));
        Assert.Equal(0, got);
        Assert.Contains(errors, e => e.Contains("must be an integer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TryParseAndExecute_OptionalArg_DefaultIsApplied()
    {
        int got = -1;

        var cs = CommandSystemCore.CreateBuilder(prefix: "/")
            .Add(new CommandDefinition(
                Name: "roll",
                Aliases: ["roll"],
                AccessLevel: 0,
                ArgSpecs: [new ArgSpec("sides", ArgKind.Int32, Optional: true, Default: "20")],
                Handler: (_, args) => got = args.GetInt32Or(0, fallback: -1)))
            .Build();

        Assert.True(cs.TryParseAndExecute("/roll", context: null));
        Assert.Equal(20, got);

        got = -1;
        Assert.True(cs.TryParseAndExecute("/roll 6", context: null));
        Assert.Equal(6, got);
    }

    [Fact]
    public void TryParseAndExecute_DeniesAccess_WhenAccessLevelTooLow()
    {
        var called = false;
        var errors = new List<string>();

        var cs = CommandSystemCore.CreateBuilder(prefix: "/", onError: (_, msg) => errors.Add(msg))
            .Add(new CommandDefinition(
                Name: "gm",
                Aliases: ["gm"],
                AccessLevel: 10,
                ArgSpecs: [],
                Handler: (_, _) => called = true))
            .Build();

        Assert.True(cs.TryParseAndExecute("/gm", context: null, accessLevel: 0));
        Assert.False(called);
        Assert.Contains(errors, e => e.Contains("requires permission level", StringComparison.OrdinalIgnoreCase));

        errors.Clear();
        Assert.True(cs.TryParseAndExecute("/gm", context: null, accessLevel: 10));
        Assert.True(called);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryParseAndExecute_RespectsCanExecute_GatesExecution()
    {
        var called = false;
        var errors = new List<string>();

        var cs = CommandSystemCore.CreateBuilder(prefix: "/", onError: (_, msg) => errors.Add(msg))
            .Add(new CommandDefinition(
                Name: "save",
                Aliases: ["save"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: (_, _) => called = true,
                CanExecute: _ => "nope"))
            .Build();

        Assert.True(cs.TryParseAndExecute("/save", context: null));
        Assert.False(called);
        Assert.Single(errors);
        Assert.Equal("nope", errors[0]);
    }

    [Fact]
    public void TryParseAndExecute_CatchesHandlerExceptions_AndReports()
    {
        var errors = new List<string>();

        var cs = CommandSystemCore.CreateBuilder(prefix: "/", onError: (_, msg) => errors.Add(msg))
            .Add(new CommandDefinition(
                Name: "boom",
                Aliases: ["boom"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: (_, _) => throw new InvalidOperationException("kaboom")))
            .Build();

        Assert.True(cs.TryParseAndExecute("/boom", context: null));
        Assert.Contains(errors, e => e.Contains("crashed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("InvalidOperationException", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("kaboom", StringComparison.OrdinalIgnoreCase));
    }
}
