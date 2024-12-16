using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Gift Wrapping Snowman")]
public class GiftWrappingSnowman(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        client.PendingItemSessions = null;
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var opts = new List<Dialog.OptionsDataItem>
        {
            new (0x02, "Wrap a present")
        };

        client.SendOptionsDialog(Mundane, "Take a look, see what we have in stock.", opts.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        switch (responseID)
        {
            case 0x00:
                {
                    if (string.IsNullOrEmpty(args)) return;
                    _ = ushort.TryParse(args, out var slot);

                    if (slot == 0)
                    {
                        client.SendOptionsDialog(mundane, "Let's try that again!");
                        return;
                    }

                    client.Aisling.Inventory.Items.TryGetValue(Convert.ToInt32(slot), out var itemFromSlot);

                    if (itemFromSlot == null)
                    {
                        client.SendOptionsDialog(mundane, "Let's try that again!");
                        return;
                    }

                    itemFromSlot.GiftWrapped = "Snowman";
                    itemFromSlot.DisplayImage = 2301;
                    client.Aisling.Inventory.UpdateSlot(client, itemFromSlot);
                    client.CloseDialog();
                }
                break;
            case 0x02:
                client.SendItemSellDialog(Mundane, "So, what item would you like to box?", NpcShopExtensions.GetCharacterGiftBoxInventoryByteList(client));
                break;
        }
    }
}