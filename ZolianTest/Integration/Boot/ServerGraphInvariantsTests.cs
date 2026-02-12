using Xunit;

using ZolianTest.Integration.Fixtures;

namespace ZolianTest.Integration.Boot;

[Collection("Integration")]
public sealed class ServerGraphInvariantsTests
{
    private readonly ZolianHostFixture _fx;

    public ServerGraphInvariantsTests(ZolianHostFixture fx) => _fx = fx;

    [Fact]
    public void Core_servers_are_initialized_on_setup()
    {
        // These should be assigned by orchestrator / startup.
        Assert.NotNull(_fx.Setup.Game);
        Assert.NotNull(_fx.Setup.LoginServer);
        Assert.NotNull(_fx.Setup.LobbyServer);
    }

    [Fact]
    public void Setup_can_resolve_world_server_from_di()
    {
        Assert.NotNull(_fx.WorldServer);
    }
}
