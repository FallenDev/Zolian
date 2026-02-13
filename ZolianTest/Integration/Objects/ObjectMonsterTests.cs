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
public sealed class ObjectMonsterTests
{
    [Fact]
    public Task Create_monster_from_template()
    {
        // Obtain template/map and create
        var (map, template) = PickMapAndTemplate();
        var monster = CreateMonsterOrThrow(template, map);

        // Assert
        Assert.NotNull(monster);
        Assert.NotNull(monster.Template);
        Assert.False(string.IsNullOrWhiteSpace(monster.Template.Name));

        return Task.CompletedTask;
    }

    [Fact]
    public Task Add_and_query_monster_on_map()
    {
        // Obtain template/map and create
        var (map, template) = PickMapAndTemplate();
        var monster = CreateMonsterOrThrow(template, map);
        Assert.NotNull(monster);

        // Set location and map
        monster.CurrentMapId = map.ID;
        var pos = PlaceAndAddObjectRandomly(map, monster);

        // Query
        var found = ObjectManager.GetObject<Monster>(map, m => ReferenceEquals(m, monster));
        Assert.NotNull(found);
        Assert.Equal(pos.X, found.Pos.X);
        Assert.Equal(pos.Y, found.Pos.Y);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Remove_monster_from_map()
    {
        // Obtain template/map and create
        var (map, template) = PickMapAndTemplate();
        var monster = CreateMonsterOrThrow(template, map);
        Assert.NotNull(monster);

        // Set location and map
        monster.CurrentMapId = map.ID;
        PlaceAndAddObjectRandomly(map, monster);
        var found = ObjectManager.GetObject<Monster>(map, m => ReferenceEquals(m, monster));
        Assert.NotNull(found);

        // Delete and query
        ObjectManager.DelObject(monster);
        var deleted = ObjectManager.GetObject<Monster>(map, m => ReferenceEquals(m, monster));
        Assert.Null(deleted);

        return Task.CompletedTask;
    }

    private static (Area Map, MonsterTemplate Template) PickMapAndTemplate()
    {
        var setup = ServerSetup.Instance;

        Assert.NotNull(setup.MonsterTemplateByMapCache);
        Assert.True(setup.MonsterTemplateByMapCache.Count > 0);

        foreach (var kv in setup.MonsterTemplateByMapCache)
        {
            var mapId = kv.Key;
            var templates = kv.Value;

            if (templates is null || templates.Length == 0)
                continue;

            if (!setup.GlobalMapCache.TryGetValue(mapId, out var map) || map is null)
                continue;

            var template = templates.First();
            return (map, template);
        }

        throw new InvalidOperationException("No valid monster templates found for testing.");
    }

    private static Monster CreateMonsterOrThrow(MonsterTemplate template, Area map, int attempts = 25)
    {
        for (var i = 0; i < attempts; i++)
        {
            var monster = Monster.Create(template, map);
            if (monster is not null)
                return monster;

            // Tiny delay to get past any spawn cooldown/time gating
            Thread.Sleep(10);
        }

        throw new InvalidOperationException(
            $"Monster.Create returned null after {attempts} attempts. " +
            $"MonsterCreationScript='{ServerSetup.Instance.Config?.MonsterCreationScript}', " +
            $"Template='{template?.Name}', MapId={map?.ID}");
    }


    private static Vector2 PlaceAndAddObjectRandomly(Area map, Monster node, int attempts = 100)
    {
        for (var i = 0; i < attempts; i++)
        {
            var x = Generator.GenerateMapLocation(map.Height);
            var y = Generator.GenerateMapLocation(map.Width);

            if (map.IsWall(x, y))
                continue;

            node.Pos = new Vector2(x, y);
            ObjectManager.AddObject(node);

            return node.Pos;
        }

        throw new InvalidOperationException($"Could not place monster on map {map.ID} within {attempts} attempts.");
    }
}