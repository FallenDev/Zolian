using System.Collections.Generic;
using System.Numerics;
using Darkages.Common;
using Darkages.Enums;

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
            Assert.IsTrue(inRange);
        }
    }

    [Test]
    public void ShouldGenerateMapLocation()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.GenerateMapLocation(50);
            var inRange = result is >= 0 and <= 50;
            Assert.IsTrue(inRange);
        }
    }

    [Test]
    public void ShouldRandNumGen3()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen3();
            var inRange = result is >= 0 and <= 3;
            Assert.IsTrue(inRange);
        }
    }

    [Test]
    public void ShouldRandNumGen10()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen10();
            var inRange = result is >= 0 and <= 10;
            Assert.IsTrue(inRange);
        }
    }

    [Test]
    public void ShouldRandNumGen20()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen20();
            var inRange = result is >= 0 and <= 20;
            Assert.IsTrue(inRange);
        }
    }

    [Test]
    public void ShouldRandNumGen100()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandNumGen100();
            var inRange = result is >= 0 and <= 100;
            Assert.IsTrue(inRange);
        }
    }

    [Test]
    public void ShouldRandomNumPercentGen()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandomNumPercentGen();
            var inRange = result is >= 0.00 and <= 1.0;
            Assert.IsTrue(inRange);
        }
    }

    [Test]
    public void ShouldRandomEnumValue()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandomEnumValue<ItemFlags>();
            Assert.NotNull(result);
        }
    }

    [Test]
    public void ShouldRandomIEnum()
    {
        var _dojoSpots = new List<Vector2>
        {
            new(12, 22),
            new(13, 21),
            new(14, 22),
            new(13, 23),
            new(12, 13),
            new(13, 12),
            new(14, 13),
            new(13, 14),
            new(22, 14),
            new(21, 13),
            new(23, 13),
            new(22, 13),
            new(22, 21),
            new(23, 22),
            new(22, 23),
            new(21, 22)
        };

        for (var i = 0; i < 1000; i++)
        {
            var result = _dojoSpots.RandomIEnum();
            Assert.Contains(result, _dojoSpots);
        }
    }

    [Test]
    public void ShouldGenerateRandomMonsterStatVariance()
    {
        for (var i = 0; i < 1000; i++)
        {
            var result = Generator.RandomMonsterStatVariance(300);
            var inRange = result is > 300 and <= 354;
            Assert.IsTrue(inRange);
        }
    }
}