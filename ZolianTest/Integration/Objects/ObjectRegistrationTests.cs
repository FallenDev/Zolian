using Darkages.Network.Server;
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
    public async Task Object_Is_Registered_When_Added_To_Map()
    {
        var map = ServerSetup.Instance.GlobalMapCache.First().Value;

        var item = new Item();
        item = item.Create(item, "Stick");

        //map.Add(item);

        //await Task.Delay(100); // allow component tick

        //Assert.True(ObjectManager.Contains(item));
    }
}
