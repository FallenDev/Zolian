using Darkages.Sprites;

namespace Darkages.Dialogs.Abstractions;

public interface ISellShopSource : IDialogSourceEntity
{
    /// <summary>
    ///     A collection of item names (DisplayName) that the merchant will buy
    /// </summary>
    ICollection<Item> ItemsToBuy { get; }

    /// <summary>
    ///     Determines if the merchant will buy the specified item
    /// </summary>
    /// <param name="item">The item to check</param>
    /// <returns><c>true</c> if the merchant will buy the item, otherwise <c>false</c></returns>
    bool IsBuying(Item item);
}