using System.Net;

using Darkages.Network.Server;

using Xunit;

using ZolianTest.Integration.Fixtures;

namespace ZolianTest.Integration.Boot;

[Collection("Integration")]
public sealed class StartupInvariantsTests
{
    private readonly ZolianHostFixture _fx;

    public StartupInvariantsTests(ZolianHostFixture fx) => _fx = fx;

    [Fact]
    public void Running_is_true()
    {
        Assert.True(ServerSetup.Instance.Running);
    }

    [Fact]
    public void StoragePath_is_set_and_exists()
    {
        Assert.False(string.IsNullOrWhiteSpace(_fx.Setup.StoragePath));
        Assert.True(Directory.Exists(_fx.Setup.StoragePath));
    }

    [Fact]
    public void Config_is_loaded()
    {
        Assert.NotNull(_fx.Setup.Config);
    }

    [Fact]
    public void IpAddress_is_set()
    {
        Assert.NotNull(_fx.Setup.IpAddress);
        Assert.NotEqual(IPAddress.None, _fx.Setup.IpAddress);
    }
}
