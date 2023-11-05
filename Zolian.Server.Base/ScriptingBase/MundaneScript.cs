using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Templates;

using System.Security.Cryptography;

namespace Darkages.ScriptingBase;

public abstract class MundaneScript(WorldServer server, Mundane mundane) : ObjectManager, IScriptBase
{
    private long _onClickCheck;
    private static string[] Messages => ServerSetup.Instance.Config.NpcInteraction.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private static int Count => Messages.Length;

    protected Mundane Mundane { get; init; } = mundane;
    protected WorldServer Server { get; } = server;

    public abstract void OnResponse(WorldClient client, ushort responseId, string args);

    public virtual void OnClick(WorldClient client, uint serial)
    {
        // Entry to this method, generate a random key
        _onClickCheck = Random.Shared.NextInt64();

        // Check if user is within the appropriate distance to the npc
        if (!Mundane.WithinEarShotOf(client.Aisling))
        {
            client.CloseDialog();
            return;
        }

        // Obtain serial from packet entry ClientFormat43
        client.EntryCheck = serial;
    }

    protected virtual void TopMenu(WorldClient client)
    {
        client.PendingItemSessions = null;
        client.PendingBuySessions = null;
        if (Mundane.Serial != client.EntryCheck) client.CloseDialog();
    }

    protected bool AuthenticateUser(WorldClient client)
    {
        // Ensure the user did not spoof their way to the OnResponse
        if (Mundane.Serial != client.EntryCheck) return false;
        if (Mundane.Bypass) _onClickCheck = 1;

        // If user is not on the same map, disconnect them
        if (client.Aisling.Map.ID != Mundane.Map.ID)
        {
            client.Dispose();
            return false;
        }

        // If the user is not longer within distance of the npc, return
        if (_onClickCheck != 0) return Mundane.WithinEarShotOf(client.Aisling);

        // Otherwise disconnect the client
        client.Dispose();
        return false;
    }

    public virtual void OnGossip(WorldClient client, string message)
    {
        var randomInteract = Generator.RandomNumPercentGen();

        switch (randomInteract)
        {
            case >= 0 and <= .92:
                break;
            default:
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendPublicMessage(Mundane.Serial, PublicMessageType.Normal, $"{Mundane.Name}: {Messages[RandomNumberGenerator.GetInt32(Count + 1) % Messages.Length]}"));
                break;
        }
    }

    public virtual void OnItemDropped(WorldClient client, Item item)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, "What's this for? Thank you.");
    }

    public virtual void OnGoldDropped(WorldClient client, uint gold)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, "What's this for? Thank you.");
    }

    public virtual void OnBack(Aisling aisling)
    {

    }

    public virtual void OnNext(Aisling aisling)
    {

    }

    public virtual void OnClose(Aisling aisling)
    {
        aisling.Client.CloseDialog();
    }

    /// <summary>
    /// Skills NPC can teach
    /// </summary>
    protected List<SkillTemplate> ObtainSkillList()
    {
        var skills = ServerSetup.Instance.GlobalSkillTemplateCache.Where(i => i.Value.NpcKey.ToLowerInvariant().Equals(Mundane.Template.Name.ToLowerInvariant())).ToArray();
        var possibleSkillTemplates = new List<SkillTemplate>();

        foreach (var (_, value) in skills)
        {
            possibleSkillTemplates.Add(value);
        }

        return possibleSkillTemplates;
    }

    /// <summary>
    /// Spells NPC can teach
    /// </summary>
    protected List<SpellTemplate> ObtainSpellList()
    {
        var spells = ServerSetup.Instance.GlobalSpellTemplateCache.Where(i => i.Value.NpcKey.ToLowerInvariant().Equals(Mundane.Template.Name.ToLowerInvariant())).ToArray();
        var possibleSpellTemplates = new List<SpellTemplate>();

        foreach (var (_, value) in spells)
        {
            possibleSpellTemplates.Add(value);
        }

        return possibleSpellTemplates;
    }
}