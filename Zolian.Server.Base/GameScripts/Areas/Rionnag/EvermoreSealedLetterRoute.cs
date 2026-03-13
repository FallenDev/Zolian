using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Rionnag;

[Script("Rionnag Sealed")]
public class EvermoreSealedLetterRoute(Area area) : AreaScript(area)
{
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        TryGiveSealedLetter(client,
            "A courier seal glints from the roadside mud. You recover a Sealed Letter before the rain ruins it.");
    }

    public override void OnMapExit(WorldClient client) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }

    private static void TryGiveSealedLetter(WorldClient client, string message)
    {
        var quests = client.Aisling.QuestManager;

        if (client.Aisling.RionnagSatchel || !quests.EvermoreWhispersStarted || quests.EvermoreAssassinsSigilAttuned || quests.AssassinsGuildReputation >= 2)
            return;

        if (client.Aisling.HasStacks("Sealed Letter", 2))
            return;

        if (!client.TryGiveQuantity(client.Aisling, "Sealed Letter", 1))
        {
            message = "Your pack is too full to tuck away the Sealed Letter.";
        }
        else
            client.Aisling.RionnagSatchel = true;

        client.SendServerMessage(ServerMessageType.Whisper, message);
    }
}
