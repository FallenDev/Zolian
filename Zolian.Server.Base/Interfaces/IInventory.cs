using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Interfaces;

public interface IInventory
{
    bool CanPickup(Aisling player, Item LpItem);
    byte FindEmpty();
    Item FindInSlot(int Slot);
    List<Item> HasMany(Predicate<Item> predicate);
    Item Has(Predicate<Item> predicate);
    int Has(Template templateContext);
    int HasCount(Template templateContext);
    void Remove(GameClient client, Item item);
    Item Remove(byte movingFrom);
    void RemoveFromInventory(GameClient client, Item item);
    void RemoveRange(GameClient client, Item item, int range);
    void AddRange(GameClient client, Item item, int range);
    void Set(Item s);
    void UpdateSlot(GameClient client, Item item);
    void UpdatePlayersWeight(GameClient client);
}