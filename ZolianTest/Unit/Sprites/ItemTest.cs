using Darkages.Common;
using Darkages.Enums;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Templates;

using Xunit;

namespace ZolianTest.Unit.Sprites;

/// <summary>
/// NOTE: These tests require an initialized ObjectService/world context.
/// Right now we keep them as skipped placeholders until the test harness
/// for maps/areas/object manager is wired up inside ZolianTest.
/// </summary>
public sealed class ItemTest
{
    private static Item CreateGroundItem(ItemTemplate template)
        => new()
        {
            Serial = 0,
            X = 0,
            Y = 0,
            Pos = default,
            TileType = TileContent.Item,
            ItemId = Generator.RandNumGen100(),
            Template = template,
            Name = template?.Name ?? "Stick",
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
            Script = null,
            WeaponScript = null,
            Dropping = 0,
            Image = 0,
            DisplayImage = 0
        };

    [Fact(Skip = "Requires ObjectService + Map initialization harness")]
    public void AddItemToGameWorld()
    {
        var template = new ItemTemplate { Name = "Stick" };
        var item = CreateGroundItem(template);

        ObjectService.AddGameObject(item);

        var found = ObjectService.Query<Item>(item.Map, i => i.Serial == item.Serial);
        Assert.Same(item, found);
    }

    [Fact(Skip = "Requires ObjectService + Map initialization harness")]
    public void RemoveItemFromGameWorld()
    {
        var template = new ItemTemplate { Name = "Stick" };
        var item = CreateGroundItem(template);

        ObjectService.AddGameObject(item);
        ObjectService.RemoveGameObject(item);

        var found = ObjectService.Query<Item>(item.Map, i => i.Serial == item.Serial);
        Assert.Null(found);
    }
}
