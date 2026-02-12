using Darkages.Common;

using Xunit;

namespace ZolianTest.Unit.Common;

public sealed class ExtensionsLegacyTests
{
    [Fact]
    public void ShouldClampMinimumValue()
    {
        var result = 20.IntClamp(50, 6000);
        Assert.Equal(50, result);
    }

    [Fact]
    public void ShouldClampMaximumValue()
    {
        var result = 6500.IntClamp(50, 6000);
        Assert.Equal(6000, result);
    }

    [Fact]
    public void ShouldNotIsWithinMinimumValue()
    {
        var result = 20.IntIsWithin(50, 6000);
        Assert.False(result);
    }

    [Fact]
    public void ShouldIsWithinMinimumValue()
    {
        var result = 150.IntIsWithin(50, 6000);
        Assert.True(result);
    }

    [Fact]
    public void ShouldNotIsWithinMaximumValue()
    {
        var result = 9000.IntIsWithin(50, 6000);
        Assert.False(result);
    }

    [Fact]
    public void ShouldIsWithinMaximumValue()
    {
        var result = 5500.IntIsWithin(50, 6000);
        Assert.True(result);
    }
}
