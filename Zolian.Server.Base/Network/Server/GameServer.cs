namespace Darkages.Network.Server;

public class GameServer
{

    /// <summary>
    /// Request Bulletin Board
    /// </summary>
    //protected override void Format3BHandler(WorldClient client, ClientFormat3B format)
    //{
    //    if (!CanInteract(client, false, true, false)) return;

    //    if (format.Type == 0x01)
    //    {
    //        client.Send(new BoardList(ServerSetup.PersonalBoards));
    //        return;
    //    }

    //    if (format.Type == 0x02)
    //    {
    //        if (format.BoardIndex == 0)
    //        {
    //            var clone = ObjectHandlers.PersonalMailJsonConvert<Board>(ServerSetup.PersonalBoards[format.BoardIndex]);
    //            {
    //                clone.Client = client;
    //                client.Send(clone);
    //            }
    //            return;
    //        }

    //        var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
    //            .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
    //            .FirstOrDefault();

    //        if (boards != null)
    //            client.Send(boards);

    //        return;
    //    }

    //    if (format.Type == 0x03)
    //    {
    //        var index = format.TopicIndex - 1;

    //        var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
    //            .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
    //            .FirstOrDefault();

    //        if (boards != null &&
    //            boards.Posts.Count > index)
    //        {
    //            var post = boards.Posts[index];
    //            if (!post.Read)
    //            {
    //                post.Read = true;
    //            }

    //            client.Send(post);
    //            return;
    //        }

    //        client.Send(new ForumCallback("Unable to retrieve more.", 0x06, true));
    //        return;
    //    }

    //    var readyTime = DateTime.UtcNow;
    //    if (format.Type == 0x06)
    //    {
    //        var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
    //            .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
    //            .FirstOrDefault();

    //        if (boards == null) return;
    //        var np = new PostFormat(format.BoardIndex, format.TopicIndex)
    //        {
    //            DatePosted = readyTime,
    //            Message = format.Message,
    //            Subject = format.Title,
    //            Read = false,
    //            Sender = client.Aisling.Username,
    //            Recipient = format.To,
    //            PostId = (ushort)(boards.Posts.Count + 1)
    //        };

    //        np.Associate(client.Aisling.Username);
    //        boards.Posts.Add(np);
    //        ServerSetup.SaveCommunityAssets();
    //        client.Send(new ForumCallback("Message Delivered.", 0x06, true));
    //        var recipient = ObjectHandlers.GetAislingForMailDeliveryMessage(Convert.ToString(format.To));

    //        if (recipient == null) return;
    //        recipient.Client.SendStats(StatusFlags.UnreadMail);
    //        recipient.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYou have new mail.");
    //        return;
    //    }

    //    if (format.Type == 0x04)
    //    {
    //        var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
    //            .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
    //            .FirstOrDefault();

    //        if (boards == null) return;
    //        var np = new PostFormat(format.BoardIndex, format.TopicIndex)
    //        {
    //            DatePosted = readyTime,
    //            Message = format.Message,
    //            Subject = format.Title,
    //            Read = false,
    //            Sender = client.Aisling.Username,
    //            PostId = (ushort)(boards.Posts.Count + 1)
    //        };

    //        np.Associate(client.Aisling.Username);

    //        boards.Posts.Add(np);
    //        ServerSetup.SaveCommunityAssets();
    //        client.Send(new ForumCallback("Post Added.", 0x06, true));

    //        return;
    //    }

    //    if (format.Type == 0x05)
    //    {
    //        var community = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
    //            .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
    //            .FirstOrDefault();

    //        if (community == null || community.Posts.Count <= 0) return;
    //        try
    //        {
    //            if ((format.BoardIndex == 0
    //                    ? community.Posts[format.TopicIndex - 1].Recipient
    //                    : community.Posts[format.TopicIndex - 1].Sender
    //                ).Equals(client.Aisling.Username, StringComparison.OrdinalIgnoreCase) || client.Aisling.GameMaster)
    //            {
    //                client.Send(new ForumCallback("", 0x07, true));
    //                client.Send(new BoardList(ServerSetup.PersonalBoards));
    //                client.Send(new ForumCallback("Post Deleted.", 0x07, true));

    //                community.Posts.RemoveAt(format.TopicIndex - 1);
    //                ServerSetup.SaveCommunityAssets();

    //                client.Send(new ForumCallback("Post Deleted.", 0x07, true));
    //            }
    //            else
    //            {
    //                client.Send(new ForumCallback(ServerSetup.Instance.Config.CantDoThat, 0x07, true));
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            ServerSetup.Logger(ex.Message, LogLevel.Error);
    //            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
    //            Crashes.TrackError(ex);
    //            client.Send(new ForumCallback(ServerSetup.Instance.Config.CantDoThat, 0x07, true));
    //        }
    //    }

    //    if (format.Type != 0x07) return;
    //    {
    //        client.Send(client.Aisling.GameMaster == false
    //            ? new ForumCallback("You cannot perform this action.", 0x07, true)
    //            : new ForumCallback("Action completed.", 0x07, true));

    //        if (format.BoardIndex == 0)
    //        {
    //            var clone = ObjectHandlers.PersonalMailJsonConvert<Board>(ServerSetup.PersonalBoards[format.BoardIndex]);
    //            {
    //                clone.Client = client;
    //                client.Send(clone);
    //            }
    //            return;
    //        }

    //        var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
    //            .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
    //            .FirstOrDefault();

    //        if (!client.Aisling.GameMaster) return;
    //        if (boards == null) return;

    //        foreach (var ind in boards.Posts.Where(ind => ind.PostId == format.TopicIndex))
    //        {
    //            if (ind.HighLighted)
    //            {
    //                ind.HighLighted = false;
    //                client.SendMessage(0x08, $"Removed Highlight: {ind.Subject}");
    //            }
    //            else
    //            {
    //                ind.HighLighted = true;
    //                client.SendMessage(0x08, $"Highlighted: {ind.Subject}");
    //            }
    //        }

    //        client.Send(boards);
    //    }
    //}


    /// <summary>
    /// Client Trading
    /// </summary>
    //protected override void Format4AHandler(WorldClient client, ClientFormat4A format)
    //{
    //    if (format == null) return;
    //    if (!CanInteract(client, false, true, false)) return;

    //    var trader = ObjectHandlers.GetObject<Aisling>(client.Aisling.Map, i => i.Serial.Equals((int)format.Id));
    //    var player = client.Aisling;

    //    if (player == null || trader == null) return;
    //    if (!player.WithinRangeOf(trader)) return;

    //    switch (format.Type)
    //    {
    //        case 0x00:
    //            {
    //                if (player.ThrewHealingPot) break;

    //                player.Exchange = new ExchangeSession(trader);
    //                trader.Exchange = new ExchangeSession(player);

    //                var packet = new NetworkPacketWriter();
    //                packet.Write((byte)0x42);
    //                packet.Write((byte)0x00);
    //                packet.Write((byte)0x00);
    //                packet.Write((uint)trader.Serial);
    //                packet.WriteStringA(trader.Username);
    //                client.Send(packet);

    //                packet = new NetworkPacketWriter();
    //                packet.Write((byte)0x42);
    //                packet.Write((byte)0x00);
    //                packet.Write((byte)0x00);
    //                packet.Write((uint)player.Serial);
    //                packet.WriteStringA(player.Username);
    //                trader.Client.Send(packet);
    //            }
    //            break;
    //        case 0x01:
    //            {
    //                if (player.ThrewHealingPot)
    //                {
    //                    player.ThrewHealingPot = false;
    //                    break;
    //                }

    //                var item = client.Aisling.Inventory.Items[format.ItemSlot];

    //                if (!item.Template.Flags.FlagIsSet(ItemFlags.Tradeable))
    //                {
    //                    player.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "That item is not tradeable");
    //                    return;
    //                }

    //                if (player.Exchange == null) return;
    //                if (trader.Exchange == null) return;
    //                if (player.Exchange.Trader != trader) return;
    //                if (trader.Exchange.Trader != player) return;
    //                if (player.Exchange.Confirmed) return;
    //                if (item?.Template == null) return;

    //                if (trader.CurrentWeight + item.Template.CarryWeight < trader.MaximumWeight)
    //                {
    //                    if (player.EquipmentManager.RemoveFromInventory(item, true))
    //                    {
    //                        player.Exchange.Items.Add(item);
    //                        player.Exchange.Weight += item.Template.CarryWeight;
    //                    }

    //                    var packet = new NetworkPacketWriter();
    //                    packet.Write((byte)0x42);
    //                    packet.Write((byte)0x00);

    //                    packet.Write((byte)0x02);
    //                    packet.Write((byte)0x00);
    //                    packet.Write((byte)player.Exchange.Items.Count);
    //                    packet.Write(item.DisplayImage);
    //                    packet.Write(item.Color);
    //                    packet.WriteStringA(item.NoColorDisplayName);
    //                    client.Send(packet);

    //                    packet = new NetworkPacketWriter();
    //                    packet.Write((byte)0x42);
    //                    packet.Write((byte)0x00);

    //                    packet.Write((byte)0x02);
    //                    packet.Write((byte)0x01);
    //                    packet.Write((byte)player.Exchange.Items.Count);
    //                    packet.Write(item.DisplayImage);
    //                    packet.Write(item.Color);
    //                    packet.WriteStringA(item.NoColorDisplayName);
    //                    trader.Client.Send(packet);
    //                }
    //                else
    //                {
    //                    var packet = new NetworkPacketWriter();
    //                    packet.Write((byte)0x42);
    //                    packet.Write((byte)0x00);

    //                    packet.Write((byte)0x04);
    //                    packet.Write((byte)0x00);
    //                    packet.WriteStringA("They can't seem to lift that. The trade has been cancelled.");
    //                    client.Send(packet);

    //                    packet = new NetworkPacketWriter();
    //                    packet.Write((byte)0x42);
    //                    packet.Write((byte)0x00);

    //                    packet.Write((byte)0x04);
    //                    packet.Write((byte)0x01);
    //                    packet.WriteStringA("That item seems to be too heavy. The trade has been cancelled.");
    //                    trader.Client.Send(packet);
    //                    player.CancelExchange();
    //                }
    //            }
    //            break;
    //        case 0x02:
    //            break;
    //        case 0x03:
    //            {
    //                if (player.Exchange == null) return;
    //                if (trader.Exchange == null) return;
    //                if (player.Exchange.Trader != trader) return;
    //                if (trader.Exchange.Trader != player) return;
    //                if (player.Exchange.Confirmed) return;
    //                if (player.Exchange.Gold != 0) return;

    //                var gold = format.Gold;

    //                if (gold > player.GoldPoints)
    //                {
    //                    player.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You don't have that much.");
    //                    return;
    //                }

    //                if (trader.GoldPoints + gold > ServerSetup.Instance.Config.MaxCarryGold)
    //                {
    //                    player.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Player cannot hold that amount.");
    //                    return;
    //                }

    //                player.GoldPoints -= gold;
    //                player.Exchange.Gold = gold;
    //                player.Client.SendStats(StatusFlags.StructC);

    //                var packet = new NetworkPacketWriter();
    //                packet.Write((byte)0x42);
    //                packet.Write((byte)0x00);

    //                packet.Write((byte)0x03);
    //                packet.Write((byte)0x00);
    //                packet.Write(gold);
    //                client.Send(packet);

    //                packet = new NetworkPacketWriter();
    //                packet.Write((byte)0x42);
    //                packet.Write((byte)0x00);

    //                packet.Write((byte)0x03);
    //                packet.Write((byte)0x01);
    //                packet.Write(gold);
    //                trader.Client.Send(packet);
    //            }
    //            break;
    //        case 0x04:
    //            {
    //                if (player.Exchange == null) return;
    //                if (trader.Exchange == null) return;
    //                if (player.Exchange.Trader != trader) return;
    //                if (trader.Exchange.Trader != player) return;

    //                player.CancelExchange();
    //            }
    //            break;

    //        case 0x05:
    //            {
    //                if (player.Exchange == null) return;
    //                if (trader.Exchange == null) return;
    //                if (player.Exchange.Trader != trader) return;
    //                if (trader.Exchange.Trader != player) return;
    //                if (player.Exchange.Confirmed) return;

    //                player.Exchange.Confirmed = true;

    //                if (trader.Exchange.Confirmed)
    //                    player.FinishExchange();

    //                var packet = new NetworkPacketWriter();
    //                packet.Write((byte)0x42);
    //                packet.Write((byte)0x00);

    //                packet.Write((byte)0x05);
    //                packet.Write((byte)0x00);
    //                packet.WriteStringA("Trade was completed.");
    //                client.Send(packet);

    //                packet = new NetworkPacketWriter();
    //                packet.Write((byte)0x42);
    //                packet.Write((byte)0x00);

    //                packet.Write((byte)0x05);
    //                packet.Write((byte)0x01);
    //                packet.WriteStringA("Trade was completed.");
    //                trader.Client.Send(packet);
    //            }
    //            break;
    //    }
    //}
}