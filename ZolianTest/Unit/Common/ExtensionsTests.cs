using Darkages.Common;

using Xunit;

namespace ZolianTest.Unit.Common;

public sealed class ExtensionsTests
{
    [Theory]
    [InlineData(-5, 0, 10, 0)]
    [InlineData(0, 0, 10, 0)]
    [InlineData(5, 0, 10, 5)]
    [InlineData(11, 0, 10, 10)]
    public void IntClamp_ClampsCorrectly(int value, int min, int max, int expected)
    {
        Assert.Equal(expected, value.IntClamp(min, max));
    }

    [Theory]
    [InlineData(-5L, 0L, 10L, 0L)]
    [InlineData(0L, 0L, 10L, 0L)]
    [InlineData(5L, 0L, 10L, 5L)]
    [InlineData(11L, 0L, 10L, 10L)]
    public void LongClamp_ClampsCorrectly(long value, long min, long max, long expected)
    {
        Assert.Equal(expected, value.LongClamp(min, max));
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(0, 2)]
    public void ReverseDirection_WrapsCorrectly(byte input, byte expected)
    {
        Assert.Equal(expected, input.ReverseDirection());
    }

    [Fact]
    public void NextHighest_ReturnsNearestHigherValue()
    {
        int[] values = [5, 10, 20, 30];
        Assert.Equal(10, values.NextHighest(6));
        Assert.Equal(20, values.NextHighest(10)); // seed itself, should pick next higher
        Assert.Equal(30, values.NextHighest(21));
    }

    [Fact]
    public void NextLowest_ReturnsNearestLowerValue()
    {
        int[] values = [5, 10, 20, 30];
        Assert.Equal(5, values.NextLowest(6));
        Assert.Equal(10, values.NextLowest(20)); // seed itself, should pick next lower
        Assert.Equal(20, values.NextLowest(29));
    }

    [Fact]
    public void TryGetValue_ByIndex_DoesNotThrowAndReturnsFalse()
    {
        int[] values = [1, 2, 3];

        Assert.True(values.TryGetValue(0, out var v0));
        Assert.Equal(1, v0);

        Assert.False(values.TryGetValue(5, out var vBad));
        Assert.Equal(0, vBad);
    }

    [Fact]
    public void TryGetValue_ByPredicate_ReturnsFirstMatch()
    {
        int[] values = [2, 4, 6, 8];

        Assert.True(values.TryGetValue(x => x % 3 == 0, out var match));
        Assert.Equal(6, match);

        Assert.False(values.TryGetValue(x => x == 7, out var none));
        Assert.Equal(0, none);
    }
}
