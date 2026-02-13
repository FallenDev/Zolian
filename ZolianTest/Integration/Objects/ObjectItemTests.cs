using System.Numerics;

using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Types;

using Xunit;

namespace ZolianTest.Integration.Objects;

[Collection("Integration")]
public sealed class ObjectItemTests
{
    [Fact]
    public Task Create_item_from_template_key()
    {
        // Item creation
        var item = new Item();
        item = item.Create(item, "Stick");                
        Assert.NotNull(item);        
        var name = item.GetDisplayName();
        Assert.False(string.IsNullOrWhiteSpace(name));
        return Task.CompletedTask;
    }

    [Fact]
    public Task Add_and_query_item_on_map()
    {
        // Fetch map
        var map = ServerSetup.Instance.GlobalMapCache.First().Value;
        Assert.NotNull(map);

        // Item creation
        var item = new Item();
        item = item.Create(item, "Stick");
        item.CurrentMapId = map.ID;
        var pos = PlaceAndAddObjectRandomly(map, item);
        var name = item.NoColorGetDisplayName();
        Assert.False(string.IsNullOrWhiteSpace(name));

        // Query item & validation
        var found = ObjectManager.GetObject(map, s => ReferenceEquals(s, item), ObjectManager.Get.Items);
        Assert.NotNull(found);
        Assert.Equal(pos.X, found.Pos.X);
        Assert.Equal(pos.Y, found.Pos.Y);

        return Task.CompletedTask;
    }

    [Fact]
    public Task Remove_item_and_confirm_not_queryable()
    {
        // Fetch map
        var map = ServerSetup.Instance.GlobalMapCache.First().Value;
        Assert.NotNull(map);

        // Item creation
        var item = new Item();
        item = item.Create(item, "Stick");
        item.CurrentMapId = map.ID;
        PlaceAndAddObjectRandomly(map, item);

        // Item deletion
        ObjectManager.DelObject(item);
        var found = ObjectManager.GetObject(map, s => ReferenceEquals(s, item), ObjectManager.Get.Items);
        Assert.Null(found);

        return Task.CompletedTask;
    }

    private static Vector2 PlaceAndAddObjectRandomly(Area map, Item node, int attempts = 100)
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

        throw new InvalidOperationException($"Could not place item on map {map.ID} within {attempts} attempts.");
    }
}
