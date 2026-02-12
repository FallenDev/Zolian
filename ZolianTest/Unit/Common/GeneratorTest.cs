using Darkages.Common;

using Xunit;

namespace ZolianTest.Unit.Common;

public sealed class GeneratorTest
{
    [Fact]
    public void ShouldGenerateDeterminedNumberRange()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.GenerateDeterminedNumberRange(15, 28);
            Assert.InRange(result, 15, 28);
        }
    }

    [Fact]
    public void ShouldGenerateMapLocation()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.GenerateMapLocation(50);
            Assert.InRange(result, 0, 50);
        }
    }

    [Fact]
    public void ShouldRandNumGen3()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen3();
            Assert.InRange(result, 0, 3);
        }
    }

    [Fact]
    public void ShouldRandNumGen10()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen10();
            Assert.InRange(result, 0, 10);
        }
    }

    [Fact]
    public void ShouldRandNumGen20()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen20();
            Assert.InRange(result, 0, 20);
        }
    }

    [Fact]
    public void ShouldRandNumGen100()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen100();
            Assert.InRange(result, 0, 100);
        }
    }

    [Fact]
    public void ShouldRandomNumPercentGen()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandomPercentPrecise();
            Assert.InRange(result, 0.0, 1.0);
        }
    }

    [Fact]
    public void ShouldGenerateRandomMonsterStatVariance()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandomMonsterStatVariance(300);
            Assert.InRange(result, 301, 354); // method uses > 300 and <= 354
        }
    }
}
