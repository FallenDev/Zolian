﻿using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Interfaces;

public interface IInventory
{
    byte FindEmpty();
    Item FindInSlot(int Slot);
    List<Item> HasMany(Predicate<Item> predicate);
    Item Has(Predicate<Item> predicate);
    int Has(Template templateContext);
    int HasCount(Template templateContext);
    void RemoveFromInventory(WorldClient client, Item item);
    void RemoveRange(WorldClient client, Item item, int range);
    void AddRange(WorldClient client, Item item, int range);
    void UpdateSlot(WorldClient client, Item item);
    void UpdatePlayersWeight(WorldClient client);
}