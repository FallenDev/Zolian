using Darkages.Sprites;
using Darkages.Templates;
using Moq;
using NUnit.Framework;

namespace ZolianTest.Sprites;

public class ItemTest
{
    private Item variantItem;
    private Item nonVariantItem;

    [SetUp]
    public void Setup()
    {
        variantItem = Mock.Of<Item>();
        nonVariantItem = Mock.Of<Item>();
        var templateOne = Mock.Of<ItemTemplate>();
    }

    [Test]
    public void ShouldGetDisplayName()
    {

    }
}