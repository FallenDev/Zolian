using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.CthonicRemains;

[Script("Advent Guild Officer")]
public class Anna(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
{
    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);
        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.QuestManager.AdventuresGuildReputation >= 5)
            options.Add(new Dialog.OptionsDataItem(0x00, "Special Ops Supply Run"));

        client.SendOptionsDialog(Mundane, "Welcome, have you seen my dear lil' Fillipe? He was to make the journey down sometime ago.", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                client.CloseDialog();
                break;

        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}