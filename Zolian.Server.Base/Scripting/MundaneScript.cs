using Chaos.Common.Definitions;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Scripting;

public abstract class MundaneScript : ObjectManager, IScriptBase
{
    private long _onClickCheck;

    protected MundaneScript(WorldServer server, Mundane mundane)
    {
        Server = server;
        Mundane = mundane;
    }

    protected Mundane Mundane { get; init; }
    protected WorldServer Server { get; }

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

    public virtual void OnGossip(WorldClient client, string message) { }
    public virtual void TargetAcquired(Sprite target) { }
    public virtual void OnItemDropped(WorldClient client, Item item) { }

    public virtual void OnGoldDropped(WorldClient client, uint gold)
    {
        client.SendServerMessage(ServerMessageType.ActiveMessage, "What's this for? Thank you.");
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