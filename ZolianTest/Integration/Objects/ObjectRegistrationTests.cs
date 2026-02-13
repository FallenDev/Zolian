using System.Numerics;

using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

using Xunit;

using ZolianTest.Integration.Fixtures;

namespace ZolianTest.Integration.Objects;

[Collection("Integration")]
public sealed class ObjectRegistrationTests
{
    private readonly ZolianHostFixture _fixture;

    public ObjectRegistrationTests(ZolianHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public Task Object_Is_Registered_When_Added_To_Map()
    {
        // Access first map in cache
        var map = ServerSetup.Instance.GlobalMapCache.First().Value;
        Assert.NotNull(map);

        // Item creation
        var item = new Item();
        item = item.Create(item, "Stick");
        Assert.NotNull(item);

        // Pick random position on map
        var random = new Random();
        int x = random.Next(0, map.Width);
        int y = random.Next(0, map.Height);

        item.Pos = new Vector2(x, y);
        item.CurrentMapId = map.ID;

        ObjectManager.AddObject(item);

        // Assert item exists on map
        var found = ObjectManager.GetObject(map, s => s == item, ObjectManager.Get.Items);
        Assert.NotNull(found);
        Assert.Equal(x, found.Pos.X);
        Assert.Equal(y, found.Pos.Y);

        return Task.CompletedTask;
    }
}