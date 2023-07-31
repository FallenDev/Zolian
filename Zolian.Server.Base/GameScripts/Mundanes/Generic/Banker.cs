using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Generic;

[Script("Banker")]
public class Banker : MundaneScript
{
    private readonly Bank _bank = new();

    public Banker(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        //client.Aisling.Client.LoadBank();
        //Refresh(client);

        //var options = new List<Dialog.OptionsDataItem>
        //{
        //    new (0x11, "Deposit Item")
        //};

        //if (client.Aisling.BankManager.Items.Count > 0)
        //{
        //    options.Add(new Dialog.OptionsDataItem(0x06, "Withdraw Item"));
        //}

        //if (client.Aisling.GoldPoints > 0)
        //{
        //    options.Add(new Dialog.OptionsDataItem(0x07, "Deposit Gold"));
        //}

        //if (client.Aisling.BankedGold > 0)
        //{
        //    options.Add(new Dialog.OptionsDataItem(0x08, "Withdraw Gold"));
        //}

        //client.SendOptionsDialog(Mundane, "We'll take real good care of your possessions.", options.ToArray());
        client.SendOptionsDialog(Mundane, "Sorry, our location is currently closed.");
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {

    }

    public override void OnGoldDropped(WorldClient client, uint money)
    {

    }

    public override async void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;
    }

    private void CompleteTrade(WorldClient client, uint cost)
    {
        client.Aisling.GoldPoints -= cost;
        //client.Aisling.BankManager.UpdatePlayersWeight(client);
        client.SendAttributes(StatUpdateType.WeightGold);
        OnClick(client, Mundane.Serial);
    }

    private void DepositMenu(WorldClient client)
    {

    }

    private void WithDrawMenu(WorldClient client)
    {

    }
}