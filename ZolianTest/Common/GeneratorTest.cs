using Darkages.Common;
using Darkages.Enums;

using NUnit.Framework;

namespace ZolianTest.Common
{
    internal class GeneratorTest
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void ShouldGenerateNumber()
        {
            var result = Generator.GenerateNumber();
            Assert.NotNull(result);
        }

        [Test]
        public void ShouldGenerateString()
        {
            const int testInt = 37;
            var result = Generator.GenerateString(testInt);
            var length = result.Length;
            Assert.AreEqual(testInt, length);
        }

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
        public void ShouldRandNumGen()
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
}