using Darkages.Common;

using NUnit.Framework;

namespace ZolianTest.Common;

internal class GeneratorTest
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void ShouldGenerateDeterminedNumberRange()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.GenerateDeterminedNumberRange(15, 28);
            var inRange = result is >= 15 and <= 28;
            Assert.Equals(inRange, true);
        }
    }

    [Test]
    public void ShouldGenerateMapLocation()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.GenerateMapLocation(50);
            var inRange = result is >= 0 and <= 50;
            Assert.Equals(inRange, true);
        }
    }

    [Test]
    public void ShouldRandNumGen3()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen3();
            var inRange = result is >= 0 and <= 3;
            Assert.Equals(inRange, true);
        }
    }

    [Test]
    public void ShouldRandNumGen10()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen10();
            var inRange = result is >= 0 and <= 10;
            Assert.Equals(inRange, true);
        }
    }

    [Test]
    public void ShouldRandNumGen20()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen20();
            var inRange = result is >= 0 and <= 20;
            Assert.Equals(inRange, true);
        }
    }

    [Test]
    public void ShouldRandNumGen100()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen100();
            var inRange = result is >= 0 and <= 100;
            Assert.Equals(inRange, true);
        }
    }

    [Test]
    public void ShouldRandomNumPercentGen()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandomNumPercentGen();
            var inRange = result is >= 0.00 and <= 1.0;
            Assert.Equals(inRange, true);
        }
    }

    [Test]
    public void ShouldGenerateRandomMonsterStatVariance()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandomMonsterStatVariance(300);
            var inRange = result is > 300 and <= 354;
            Assert.Equals(inRange, true);
        }
    }
}