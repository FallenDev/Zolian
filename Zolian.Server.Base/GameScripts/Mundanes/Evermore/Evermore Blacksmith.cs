using Darkages.Common;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites.Entity;
using Darkages.Types;

using Gender = Darkages.Enums.Gender;

namespace Darkages.GameScripts.Mundanes.Evermore;

[Script("Evermore Blacksmith")]
public class EvermoreBlacksmith(WorldServer server, Mundane mundane) : MundaneScript(server, mundane)
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

        if (!client.Aisling.QuestManager.EvermoreAssassinsSigilAttuned)
        {
            client.SendOptionsDialog(Mundane, "I do not know you.");
            return;
        }

        switch (client.Aisling.QuestManager.ArmorSmithingTier)
        {
            case "Novice":
            case "Apprentice": // 110
            case "Journeyman": // 180
                options.Add(new(0x06, "Craft Guild Armor"));
                break;
            case "Expert" when client.Aisling.QuestManager.AssassinsGuildReputation > 4: // 250
            case "Artisan" when client.Aisling.QuestManager.AssassinsGuildReputation > 4:
                options.Add(new(0x07, "Craft Adv. Guild Armor"));
                break;
        }

        client.SendOptionsDialog(Mundane, $"*nods* {client.Aisling.Username}, you need my services?", options.ToArray());
    }

    public override void OnResponse(WorldClient client, ushort responseID, string args)
    {
        if (!AuthenticateUser(client)) return;

        switch (responseID)
        {
            case 0x06:
                if (client.Aisling.QuestManager.AssassinsGuildReputation < 2) break;
                CraftGuildArmor(client);
                break;
            case 0x07:
                if (client.Aisling.QuestManager.AssassinsGuildReputation < 4) break;
                CraftAdvancedGuildArmor(client);
                break;
        }
    }

    private void CraftGuildArmor(WorldClient client)
    {
        var armor = new Item();
        armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Gents Guild Regalia" : "Ladies Guild Regalia");
        armor.GiveTo(client.Aisling);
        client.SendOptionsDialog(Mundane, $"Wear it with pride");
    }

    private void CraftAdvancedGuildArmor(WorldClient client)
    {
        var armor = new Item();
        armor = armor.Create(client.Aisling, client.Aisling.Gender == Gender.Male ? "Gents Adv. Guild Regalia" : "Ladies Adv. Guild Regalia");
        armor.GiveTo(client.Aisling);
        client.SendOptionsDialog(Mundane, $"Wear it with pride");
    }
}