using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Mileth;

[Script("Death Tree")]
public class DeathTree : MundaneScript
{
    private string _kill;

    public DeathTree(WorldServer server, Mundane mundane) : base(server, mundane) { }

    public override void OnClick(WorldClient client, uint serial)
    {
        base.OnClick(client, serial);
        TopMenu(client);
    }

    protected override void TopMenu(WorldClient client)
    {
        base.TopMenu(client);

        var options = new List<Dialog.OptionsDataItem>();

        if (client.Aisling.QuestManager.TagorDungeonAccess)
        {
            // Add logic for freeing Todesbaum -- continuation of storyline
        }
        else
        {
            if (client.Aisling.Stage is >= ClassStage.Dedicated and < ClassStage.Master && client.Aisling.HasItem("Necra Scribblings"))
                options.Add(new Dialog.OptionsDataItem(0x02, "Necra Scribblings"));
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.Level < 120
                ? "You don't seem capable, but looks can be deceiving"
                : "Aisling, what you seek is but a stones throw away", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;
        var randX1 = Generator.RandNumGen20();
        var randX2 = Generator.RandNumGen20();
        var randX3 = Generator.RandNumGen20();
        var randY1 = Generator.RandNumGen20();
        var randY2 = Generator.RandNumGen20();
        var randY3 = Generator.RandNumGen20();

        var randX4 = Generator.RandNumGen20();
        var randX5 = Generator.RandNumGen20();
        var randX6 = Generator.RandNumGen20();
        var randY4 = Generator.RandNumGen20();
        var randY5 = Generator.RandNumGen20();
        var randY6 = Generator.RandNumGen20();

        switch (responseID)
        {
            case 0x02:
                var options = new List<Dialog.OptionsDataItem>
                {
                    new (0x03, "*Throws book*"),
                    new (0x05, "Nah, I think I'll keep it")
                };

                client.SendOptionsDialog(Mundane, $"Throw me the book and I'll grant you access to {{=qTagor Dungeon", options.ToArray());
                break;
            case 0x03:
                var item = client.Aisling.HasItemReturnItem("Necra Scribblings");
                client.Aisling.Inventory.RemoveFromInventory(client, item);
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Shout, $"{Mundane.Name}: Ahh! Only a few more! And I can be...");
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(1, new Position(9, 8)));
                client.CloseDialog();

                #region Animation Show
                
                Task.Delay(350).ContinueWith(ct =>
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(50, null, Mundane.Serial));
                });
                Task.Delay(350).ContinueWith(ct =>
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(15, new Position(randX1, randY1)));
                });
                Task.Delay(650).ContinueWith(ct =>
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(15, new Position(randX2, randY2)));
                });
                Task.Delay(950).ContinueWith(ct =>
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(15, new Position(randX3, randY3)));
                });
                Task.Delay(1250).ContinueWith(ct =>
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(15, new Position(randX4, randY4)));
                });
                Task.Delay(1550).ContinueWith(ct =>
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(15, new Position(randX5, randY5)));
                });
                Task.Delay(1850).ContinueWith(ct =>
                {
                    client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(15, new Position(randX6, randY6)));
                });
                Task.Delay(3500).ContinueWith(ct =>
                {
                    client.SendPublicMessage(Mundane.Serial, PublicMessageType.Shout, $"{Mundane.Name}: ...whole again, one day");
                });

                #endregion
                
                client.Aisling.QuestManager.TagorDungeonAccess = true;
                client.SendServerMessage(ServerMessageType.ActiveMessage, $"You can now access {{=qTagor Dungeon");
                break;
            case 0x05:
                client.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: So be it, if that changes, I'll be here");
                client.CloseDialog();
                break;
        }
    }
}