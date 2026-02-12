using Darkages.Database;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.Extensions.Logging;

using Gender = Darkages.Enums.Gender;

namespace Darkages.CommandSystem;

/// <summary>
/// Command wiring over the immutable parser.
/// </summary>
public static class Commander
{
    public static void CompileCommands()
    {
        ServerSetup.Commands = CommandSystemCore
            .CreateBuilder(prefix: "/", onError: (ctx, msg) =>
            {
                if (ctx is WorldClient c)
                    c.SendServerMessage(ServerMessageType.ActiveMessage, msg);

                ServerSetup.EventsLogger($"[Chat Parser] {msg}");
            })

        #region Server Maintenance

            .Add(new CommandDefinition(
                Name: "Restart",
                Aliases: ["restart"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: Restart,
                Description: "- Force restart and reload:"
            ))

            .Add(new CommandDefinition(
                Name: "Chaos",
                Aliases: ["chaos"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: Chaos,
                Description: "- Force shutdown:"
            ))

            .Add(new CommandDefinition(
                Name: "Cancel Chaos",
                Aliases: ["cc"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: CancelChaos,
                Description: "- Halt shutdown:"
            ))

            .Add(new CommandDefinition(
                Name: "Reload Maps",
                Aliases: ["rm"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: OnMapReload,
                Description: "- Reload all maps:"
            ))

            .Add(new CommandDefinition(
                Name: "Reset IP",
                Aliases: ["ip"],
                AccessLevel: 0,
                ArgSpecs: [],
                Handler: OnIpReset,
                Description: "- Resets restricted IPs"
            ))

        #endregion

        #region GM Control

            .Add(new CommandDefinition(
                Name: "Learn Spell",
                Aliases: ["spell"],
                AccessLevel: 0,
                ArgSpecs:
                [
                    new ArgSpec("name", ArgKind.String),
                    new ArgSpec("level", ArgKind.Int32, Optional: true, Default: "100"),
                ],
                Handler: OnLearnSpell
            ))

            .Add(new CommandDefinition(
                Name: "Learn Skill",
                Aliases: ["skill"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("name", ArgKind.String),
                    new ArgSpec("level", ArgKind.Int32, Optional: true, Default: "100"),
                ],
                Handler: OnLearnSkill
            ))

            .Add(new CommandDefinition(
                Name: "Teleport",
                Aliases: ["map", "t"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("t", ArgKind.String),
                    new ArgSpec("x", ArgKind.Int32),
                    new ArgSpec("y", ArgKind.Int32),
                ],
                Handler: OnTeleport
            ))

            .Add(new CommandDefinition(
                Name: "Create Item",
                Aliases: ["give"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("item", ArgKind.String),
                    new ArgSpec("amount", ArgKind.Int32, Optional: true, Default: "1"),
                ],
                Handler: OnItemCreate
            ))

        #endregion

        #region Interaction with Players

            .Add(new CommandDefinition(
                Name: "Group",
                Aliases: ["group", "party"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("who", ArgKind.String),
                ],
                Handler: OnRemoteGroup
            ))

            .Add(new CommandDefinition(
                Name: "Summon Player",
                Aliases: ["s"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("who", ArgKind.String),
                ],
                Handler: OnSummonPlayer
            ))

            .Add(new CommandDefinition(
                Name: "Teleport to Player",
                Aliases: ["p"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("who", ArgKind.String),
                ],
                Handler: OnPortToPlayer
            ))

            .Add(new CommandDefinition(
                Name: "Sex Change",
                Aliases: ["sex"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("who", ArgKind.String),
                    new ArgSpec("s", ArgKind.Int32),
                ],
                Handler: OnSexChange
            ))

            .Add(new CommandDefinition(
                Name: "Kill Player",
                Aliases: ["kill"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("who", ArgKind.String),
                ],
                Handler: OnKillCommand
            ))

            .Add(new CommandDefinition(
                Name: "Create Monster",
                Aliases: ["sm"],
                AccessLevel: 0,
                ArgSpecs: [
                    new ArgSpec("who", ArgKind.String),
                    new ArgSpec("amount", ArgKind.Int32, Optional: true, Default: "1"),
                ],
                Handler: OnMonsterSummon
            ))

        #endregion

            .Build();
    }

    public static void ParseChatMessage(WorldClient client, string message, int accessLevel = 0)
        => ServerSetup.Commands?.TryParseAndExecute(message, client, accessLevel);

    private static bool RequireClient(object? ctx, out WorldClient client)
    {
        if (ctx is WorldClient c)
        {
            client = c;
            return true;
        }

        client = null!;
        return false;
    }

    private static bool IsDeath(WorldClient client)
        => client.Aisling.Username.Equals("death", StringComparison.InvariantCultureIgnoreCase);

    private static bool RequireDeath(WorldClient client, string denialMessage)
    {
        if (IsDeath(client))
            return true;

        client.SendServerMessage(ServerMessageType.ActiveMessage, denialMessage);
        return false;
    }

    private static Aisling? FindPlayerByName(string who)
    {
        if (string.IsNullOrWhiteSpace(who))
            return null;

        var players = ServerSetup.Instance.Game.Aislings;
        return players.FirstOrDefault(i => i != null &&
                                           string.Equals(i.Username, who, StringComparison.CurrentCultureIgnoreCase));
    }

    // --- Commands ---

    private static void Chaos(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        if (!RequireDeath(client, "{=bRestricted GM Command - Shuts down server"))
            return;

        var players = ServerSetup.Instance.Game.Aislings.ToList();

        ServerSetup.EventsLogger("--------------------------------------------", LogLevel.Warning);
        ServerSetup.EventsLogger("", LogLevel.Warning);
        ServerSetup.EventsLogger("--------------- Server Chaos ---------------", LogLevel.Warning);

        foreach (var connected in players)
        {
            connected.Client.SendServerMessage(ServerMessageType.GroupChat,
                "{=qDeath{=g: {=bServer maintenance in 1 minute.");

            _ = ChaosPerPlayerTimersAsync(client, connected);
        }
    }

    private static async Task ChaosPerPlayerTimersAsync(WorldClient gmClient, Aisling connected)
    {
        try
        {
            await Task.Delay(15000).ConfigureAwait(false);
            connected.Client.SendServerMessage(ServerMessageType.GroupChat,
                "{=qDeath{=g: {=bServer maintenance in 45 seconds.");

            await Task.Delay(15000).ConfigureAwait(false);
            connected.Client.SendServerMessage(ServerMessageType.GroupChat,
                "{=qDeath{=g: {=bServer maintenance in 30 seconds.");

            await Task.Delay(15000).ConfigureAwait(false);
            connected.Client.SendServerMessage(ServerMessageType.GroupChat,
                "{=qDeath{=g: {=bServer maintenance in 15 seconds.");

            await Task.Delay(10000).ConfigureAwait(false);
            connected.Client.SendServerMessage(ServerMessageType.GroupChat,
                "{=qDeath{=g: {=bServer maintenance in 5 seconds.");

            await Task.Delay(3000).ConfigureAwait(false);

            if (gmClient.Aisling.GameMasterChaosCancel)
            {
                gmClient.Aisling.GameMasterChaosCancel = false;
                return;
            }

            ServerSetup.Instance.Running = false;

            connected.Client.SendServerMessage(ServerMessageType.GroupChat,
                "{=qDeath{=g: {=bInvokes Chaos to rise{=g. -Server Shutdown-");

            connected.Client.SendServerMessage(ServerMessageType.ScrollWindow,
                "{=bChaos has risen.\n\n {=a During chaos, various updates will be performed. This can last anywhere between 1 to 5 minutes depending on the complexity of the update.");

            await Task.Delay(2000).ConfigureAwait(false);

            if (!gmClient.Aisling.GameMasterChaosCancel)
            {
                connected.Client.CloseTransport();
            }
            else
            {
                connected.Client.SendServerMessage(ServerMessageType.GroupChat,
                    "{=qDeath{=g: {=bChaos has been cancelled.");
            }
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"Chaos timer error: {ex.GetType().Name}: {ex.Message}", LogLevel.Error);
        }
    }

    private static void CancelChaos(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        if (!RequireDeath(client, "{=bRestricted GM Command - Cancel Chaos"))
            return;

        ServerSetup.EventsLogger("Chaos Cancelled", LogLevel.Warning);
        client.Aisling.GameMasterChaosCancel = true;
    }

    private static void Restart(object? ctx, ParsedArgs args)
    {
        var players = ServerSetup.Instance.Game.Aislings.ToList();

        ServerSetup.EventsLogger("---------------------------------------------", LogLevel.Warning);
        ServerSetup.EventsLogger("", LogLevel.Warning);
        ServerSetup.EventsLogger("------------- Server Restart Initiated -------------", LogLevel.Warning);

        foreach (var connected in players)
            connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=g-Server Restart-");

        ServerSetup.Instance.Running = false;
        Environment.Exit(0);
    }

    private static void OnLearnSpell(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Spell- on character: {client.Aisling.Username}");

        var name = args.GetString(0);
        var level = args.GetInt32Or(1, 100);
        _ = level;

        var spell = Spell.GiveTo(client.Aisling, name);
        client.SystemMessage(spell ? $"Learned: {name}" : "Failed");
        client.LoadSpellBook();
    }

    private static void OnLearnSkill(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Skill- on character: {client.Aisling.Username}");

        var name = args.GetString(0);
        var level = args.GetInt32Or(1, 100);
        _ = level;

        var skill = Skill.GiveTo(client.Aisling, name);
        client.SystemMessage(skill ? $"Learned: {name}" : "Failed");
        client.LoadSkillBook();
    }

    private static void OnSummonPlayer(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        var who = args.GetString(0);
        var player = FindPlayerByName(who);
        if (player == null) return;

        if (!player.GameSettings.GMPort)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{player.Username}, has requested not to be summoned.");
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                $"GM {client.Aisling.Username} wished to summon you, but was told you were busy");
            return;
        }

        player.Client.TransitionToMap(client.Aisling.Map, client.Aisling.Position);
        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Summon- on character: {client.Aisling.Username}, Summoned: {player.Username}");
    }

    private static void OnPortToPlayer(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        var who = args.GetString(0);
        var player = FindPlayerByName(who);
        if (player == null) return;

        client.TransitionToMap(player.Map, player.Position);
        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Port- on character: {client.Aisling.Username}, Ported: {player.Username}");
    }

    private static void OnSexChange(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        var who = args.GetString(0);
        var sResult = args.GetInt32(1);

        var player = FindPlayerByName(who);
        if (player != null)
            player.Gender = (Gender)sResult;

        player.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.ClientRefreshed());
        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Sex Change- on character: {client.Aisling.Username}");
    }

    private static void OnKillCommand(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        if (!RequireDeath(client, "{=bRestricted GM Command - Kill Player"))
            return;

        var who = args.GetString(0);
        var player = FindPlayerByName(who);

        WorldClient.KillPlayer(client.Aisling.Map, player?.Username);
        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Kill- on character: {client.Aisling.Username}");
    }

    private static void OnMonsterSummon(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        if (!RequireDeath(client, "{=bRestricted GM Command - Summon Monster"))
            return;

        var who = args.GetString(0);
        var amount = args.GetInt32Or(1, 1);
        if (amount <= 0) amount = 1;
        var summoned = 0;

        for (var i = 0; i < amount; i++)
        {
            if (!ServerSetup.Instance.GlobalMonsterTemplateCache.TryGetValue(who, out var summon) || summon is null)
                return;

            var mob = Monster.Create(summon, client.Aisling.Map);
            if (mob != null)
            {
                ObjectManager.AddObject(mob);
                summoned++;
            }
        }

        if (summoned == 0)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"Failed to summon monster: {who}");
            return;
        }

        client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"Death Summoned: {who} x{summoned}"));
        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Summon Monster- {who} x{summoned}");
    }

    private static void OnRemoteGroup(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        if (!RequireDeath(client, "{=bRestricted GM Command - Group Player"))
            return;

        var who = args.GetString(0);
        var player = FindPlayerByName(who);
        if (player == null) return;

        if (player.GroupParty != null)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "This player already belongs to a group");
            return;
        }

        if (player.Map.ID is 23352 or 7000 or 7001 or 7002 or 3029 or 720 or 393)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "You can't seem to connect with them");
            return;
        }

        Party.AddPartyMember(client.Aisling, player);
        ServerSetup.EventsLogger($"{client.RemoteIp} remotely grouped character: {client.Aisling.Username}");
    }

    private static void OnTeleport(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        var mapName = args.GetString(0);
        var x = args.GetInt32(1);
        var y = args.GetInt32(2);

        var (_, area) = ServerSetup.Instance.GlobalMapCache.FirstOrDefault(i => i.Value.Name == mapName);
        if (area == null) return;

        client.TransitionToMap(area, new Position(x, y));
        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Port- on character: {client.Aisling.Username}");
    }

    private static void OnIpReset(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        if (!RequireDeath(client, "{=bRestricted GM Command - Reset Restrictions"))
            return;

        ServerSetup.Instance.GlobalPasswordAttempt.Clear();
        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Ip Reset- on character: {client.Aisling.Username}");
    }

    private static void OnMapReload(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Reload Maps- on character: {client.Aisling.Username}");

        var players = ServerSetup.Instance.Game.Aislings;

        ServerSetup.EventsLogger("---------------------------------------------", LogLevel.Warning);
        ServerSetup.EventsLogger("------------- Maps Reloaded -------------", LogLevel.Warning);

        ServerSetup.Instance.TempGlobalMapCache = [];
        ServerSetup.Instance.TempGlobalWarpTemplateCache = [];

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
            ObjectManager.DelObject(npc);

        ServerSetup.Instance.GlobalMundaneCache = [];
        MapActivityGate.ClearAll();
        AreaStorage.Instance.CacheFromDatabase();
        DatabaseLoad.CacheFromDatabase(new WarpTemplate());

        foreach (var connected in players)
        {
            connected.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                $"{{=q{client.Aisling.Username} Invokes Reload Maps");
            connected.Client.ClientRefreshed();
        }
    }

    private static void OnItemCreate(object? ctx, ParsedArgs args)
    {
        if (!RequireClient(ctx, out var client))
            return;

        if (!RequireDeath(client, "{=bRestricted GM Command - Item Creation"))
            return;

        ServerSetup.EventsLogger($"{client.RemoteIp} used GM Command -Item Create- on character: {client.Aisling.Username}");

        var name = args.GetString(0);
        var quantity = args.GetInt32Or(1, 1);
        if (quantity <= 0) return;

        if (!ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(name, out var template))
            return;

        if (template.CanStack)
        {
            var stacks = quantity / template.MaxStack;
            var remaining = quantity % template.MaxStack;

            for (var i = 0; i < stacks; i++)
            {
                var item = new Item();
                item = item.Create(client.Aisling, template);
                item.Stacks = template.MaxStack;
                item.GiveTo(client.Aisling);
            }

            if (remaining <= 0) return;

            if (client.Aisling.Inventory.IsFull)
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYour inventory is full");
                return;
            }

            {
                var item = new Item();
                item = item.Create(client.Aisling, template);
                item.Stacks = (ushort)remaining;
                item.GiveTo(client.Aisling);
            }

            return;
        }

        var quality = Item.Quality.Common;
        var variance = Item.Variance.None;
        var wVariance = Item.WeaponVariance.None;

        for (var i = 0; i < quantity; i++)
        {
            if (client.Aisling.Inventory.IsFull)
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, "{=cYour inventory is full");
                return;
            }

            if (template.Enchantable)
            {
                quality = ItemQualityVariance.DetermineHighQuality();
                variance = ItemQualityVariance.DetermineVariance();
                wVariance = ItemQualityVariance.DetermineWeaponVariance();
            }

            var item = new Item();
            item = item.Create(client.Aisling, template, quality, variance, wVariance);
            ItemQualityVariance.ItemDurability(item, quality);
            item.GiveTo(client.Aisling);
        }
    }
}