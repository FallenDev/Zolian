using Darkages.Common;
using Darkages.Enums;
using Darkages.Sprites;
using Darkages.Templates;
using NUnit.Framework;

namespace ZolianTest.Sprites;

public class ItemTest
{
    private Item variantItem;
    private Item nonVariantItem;
    private ItemTemplate _stickTemplate;
    private ItemTemplate _monsterDropTemplate;

    [SetUp]
    public void Setup()
    {
        variantItem = new Item
        {
            Serial = 0,
            X = 0,
            Y = 0,
            Pos = default,
            TileType = TileContent.Item,
            ItemId = Generator.RandNumGen100(),
            Template = _stickTemplate,
            Name = "Stick",
            ItemPane = Item.ItemPanes.Ground,
            Slot = 0,
            InventorySlot = 0,
            Color = 0,
            Cursed = false,
            Durability = 0,
            MaxDurability = 0,
            Identified = false,
            Stacks = 0,
            Enchantable = false,
            Tarnished = false,
            Scripts = null,
            WeaponScripts = null,
            Dropping = 0,
            Owner = 0,
            Image = 0,
            DisplayImage = 0
        };
    }

    [Test]
    public void ShouldGetDisplayName()
    {


    }
}