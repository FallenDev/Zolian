using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Areas.Abel;

[Script("Abel Sealed")]
public class EvermoreSealedLetterRoute(Area area) : AreaScript(area)
{
    public override void Update(TimeSpan elapsedTime) { }

    public override void OnMapEnter(WorldClient client)
    {
        TryGiveSealedLetter(client,
            "A split satchel lies in the brush. Inside, you find a Sealed Letter addressed to Evermore.");
    }

    public override void OnMapExit(WorldClient client) { }
    public override void OnPlayerWalk(WorldClient client, Position oldLocation, Position newLocation) { }
    public override void OnItemDropped(WorldClient client, Item itemDropped, Position locationDropped) { }
    public override void OnGossip(WorldClient client, string message) { }

    private static void TryGiveSealedLetter(WorldClient client, string message)
    {
        var quests = client.Aisling.QuestManager;

        if (client.Aisling.AbelSatchel || !quests.EvermoreWhispersStarted || quests.EvermoreAssassinsSigilAttuned || quests.AssassinsGuildReputation >= 2)
            return;

        if (client.Aisling.HasStacks("Sealed Letter", 2))
            return;

        if (!client.TryGiveQuantity(client.Aisling, "Sealed Letter", 1))
        {
            message = "Your pack is too full to tuck away the Sealed Letter.";
        }
        else
            client.Aisling.AbelSatchel = true;

        client.SendServerMessage(ServerMessageType.Whisper, message);
    }
}
