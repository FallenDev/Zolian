using System.Text;
using Darkages.Common;
using NUnit.Framework;

namespace ZolianTest.Common;

internal class ExtensionsTest
{
    [SetUp]
    public void Setup() { }

    [Test]
    public void ShouldClampMinimumValue()
    {
        var result = 20.IntClamp(50, 6000);
        Assert.Equals(result, 50);
    }

    [Test]
    public void ShouldClampMaximumValue()
    {
        var result = 6500.IntClamp(50, 6000);
        Assert.Equals(result, 6000);
    }

    [Test]
    public void ShouldNotIsWithinMinimumValue()
    {
        var result = 20.IntIsWithin(50, 6000);
        Assert.Equals(result, false);
    }

    [Test]
    public void ShouldIsWithinMinimumValue()
    {
        var result = 150.IntIsWithin(50, 6000);
        Assert.Equals(result, true);
    }

    [Test]
    public void ShouldNotIsWithinMaximumValue()
    {
        var result = 9000.IntIsWithin(50, 6000);
        Assert.Equals(result, false);
    }

    [Test]
    public void ShouldIsWithinMaximumValue()
    {
        var result = 5500.IntIsWithin(50, 6000);
        Assert.Equals(result, true);
    }

    [Test]
    public void ShouldEncodeToByteArray()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var result = "String to Encode".ToByteArray();
        const string temp = "String to Encode";
        var encoding = Encoding.GetEncoding(949);
        var converted = encoding.GetBytes(temp);
        Assert.Equals(result, converted);
    }
}