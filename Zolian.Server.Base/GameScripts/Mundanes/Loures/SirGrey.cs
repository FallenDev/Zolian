using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Loures;

[Script("Sir Grey")]
public class SirGrey(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (client.Aisling.JobClass == Job.Samurai)
        {
            options.Add(new(0x20, "Learn Samurai Skills"));
            options.Add(new(0x30, "Learn Samurai Spells"));
        }

        if (client.Aisling.JobClass != Job.Samurai
            && client.Aisling.ExpLevel >= 250
            && client.Aisling.QuestManager.LouresReputation >= 5
            && client.Aisling.QuestManager.MilethReputation >= 7
            && (client.Aisling.Path == Class.Defender || client.Aisling.PastClass == Class.Defender)
            && (client.Aisling.Path == Class.Monk || client.Aisling.PastClass == Class.Monk))
        {
            options.Add(new(0x06, "I would be eager to learn more"));
            client.SendOptionsDialog(Mundane, "Hmm, I can feel it. You have something about you.\n" +
                                              "How would you like to train to be a Samurai like me?", options.ToArray());
            return;
        }

        client.SendOptionsDialog(Mundane,
            client.Aisling.ExpLevel >= 250
                ? "Obstacles in life are meant to temper you. Never hold you down, but to push you to be more."
                : "Hail! How fares the kings lands?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseId, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseId)
        {
            case 0x00:
                {
                    client.CloseDialog();
                }
                break;
            case 0x01:
                {
                    var options = new List<Dialog.OptionsDataItem>
                    {
                        new(0x00, "On it")
                    };

                    //if (client.Aisling.HasItemReturnItem(""))

                    client.SendOptionsDialog(Mundane, "Great, then let's get started. First, craft me a piece of armor befitting a Samurai.\n" +
                                                      $"{{=qCraft a Monk or Defender Level 250 armor to the level of Moonstone", options.ToArray());
                }
                break;
        }
    }

    public override void OnItemDropped(WorldClient client, Item item)
    {
        if (client == null) return;
        if (item == null) return;
        if (item.Template.Flags.FlagIsSet(ItemFlags.Sellable))
        {
            OnResponse(client, 0x500, item.InventorySlot.ToString());
        }
    }

    public override void OnGossip(WorldClient client, string message) { }
}