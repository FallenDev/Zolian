using System.Numerics;

using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

using Xunit;

namespace ZolianTest.Integration.Objects;

[Collection("Integration")]
public sealed class ObjectMundaneTests
{
    [Fact]
    public Task Create_mundane_from_template()
    {
        // Obtain template and create
        var template = PickTemplateNotAlreadySpawned(out var map);
        var npc = CreateMundane(template, map);

        // Assert
        Assert.NotNull(npc);
        Assert.NotNull(npc.Template);
        Assert.False(string.IsNullOrWhiteSpace(npc.Template.Name));
        Assert.Equal(template.AreaID, npc.CurrentMapId);
        Assert.Equal(new Vector2(template.X, template.Y), npc.Pos);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Add_and_query_mundane_on_map()
    {
        // Obtain template and create
        var template = PickTemplateNotAlreadySpawned(out var map);
        var npc = CreateMundane(template, map);

        // Query
        var found = ObjectManager.GetObject<Mundane>(map, m => ReferenceEquals(m, npc));
        Assert.NotNull(found);
        Assert.Equal(npc.Serial, found.Serial);
        Assert.Equal(npc.Pos.X, found.Pos.X);
        Assert.Equal(npc.Pos.Y, found.Pos.Y);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Remove_mundane_from_map()
    {
        // Obtain template and create
        var template = PickTemplateNotAlreadySpawned(out var map);
        var npc = CreateMundane(template, map);

        // Query
        var exists = ObjectManager.GetObject<Mundane>(map, m => ReferenceEquals(m, npc));
        Assert.NotNull(exists);

        // Delete
        ObjectManager.DelObject(npc);
        ServerSetup.Instance.GlobalMundaneCache.TryRemove(npc.Serial, out _);

        // Query
        var deleted = ObjectManager.GetObject<Mundane>(map, m => ReferenceEquals(m, npc));
        Assert.Null(deleted);

        return Task.CompletedTask;
    }

    private static Mundane CreateMundane(MundaneTemplate template, Area map)
    {
        var npc = new Mundane { Template = template };

        if (npc.Template.TurnRate == 0) npc.Template.TurnRate = 5;
        if (npc.Template.CastRate == 0) npc.Template.CastRate = 2;
        if (npc.Template.WalkRate == 0) npc.Template.WalkRate = 2;

        npc.CurrentMapId = npc.Template.AreaID;
        npc.Serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        npc.Pos = new Vector2(template.X, template.Y);
        npc.Direction = npc.Template.Direction;

        if (npc.Template.ChatRate == 0) npc.Template.ChatRate = 5;
        if (npc.Template.TurnRate == 0) npc.Template.TurnRate = 8;

        npc.Template.EnableTurning = false;
        npc.Template.WalkTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.WalkRate));
        npc.Template.ChatTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.ChatRate));
        npc.Template.TurnTimer = new WorldServerTimer(TimeSpan.FromSeconds(npc.Template.TurnRate));

        // Cache + object registration
        ServerSetup.Instance.GlobalMundaneCache.TryAdd(npc.Serial, npc);
        ObjectManager.AddObject(npc);

        return npc;
    }

    private static MundaneTemplate PickTemplateNotAlreadySpawned(out Area map)
    {
        var setup = ServerSetup.Instance;

        Assert.NotNull(setup.GlobalMundaneTemplateCache);
        Assert.True(setup.GlobalMundaneTemplateCache.Count > 0);

        // Choose a mundane template whose map exists and that isn't already present.
        foreach (var template in setup.GlobalMundaneTemplateCache.Values)
        {
            if (template is null) continue;

            // Valid map?
            if (!setup.GlobalMapCache.TryGetValue(template.AreaID, out map) || map is null)
                continue;

            // Skip templates with bad coordinates
            if (template.X < 0 || template.Y < 0 || template.X >= map.Width || template.Y >= map.Height)
                continue;

            // Production Create() early-returns if one already exists by name
            var existing = ObjectManager.GetObject<Mundane>(map, p => p.Template?.Name == template.Name);
            if (existing is not null)
                continue;

            return template;
        }

        throw new InvalidOperationException("Could not find a MundaneTemplate suitable for creation tests.");
    }
}