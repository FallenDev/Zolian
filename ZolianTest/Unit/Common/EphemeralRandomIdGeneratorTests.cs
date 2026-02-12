using System.Collections.Generic;

using Darkages.Common;

using Xunit;

namespace ZolianTest.Unit.Common;

public sealed class EphemeralRandomIdGeneratorTests
{
    [Fact]
    public void NextId_IsUniqueWithinHistoryWindow_ForInt32()
    {
        var gen = new EphemeralRandomIdGenerator<int>();

        // HISTORY_SIZE is 255; validate a decent chunk within that.
        var set = new HashSet<int>();

        for (var i = 0; i < 200; i++)
        {
            var id = gen.NextId;
            Assert.DoesNotContain(id, set);
            set.Add(id);
        }
    }

    [Fact]
    public void Shared_ReturnsSingletonInstance()
    {
        var a = EphemeralRandomIdGenerator<int>.Shared;
        var b = EphemeralRandomIdGenerator<int>.Shared;
        Assert.Same(a, b);
    }
}
