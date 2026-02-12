using Darkages.Network.Server;

using Xunit;

using ZolianTest.Integration.Fixtures;

namespace ZolianTest.Integration.Boot;

[Collection("Integration")]
public sealed class BootContractTests
{
    private readonly ZolianHostFixture _fx;

    public BootContractTests(ZolianHostFixture fx) => _fx = fx;

    [Fact]
    public void Host_is_running() => Assert.True(ServerSetup.Instance.Running);
}
