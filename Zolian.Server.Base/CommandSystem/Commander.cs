using Chaos.Common.Definitions;

using Darkages.CommandSystem.CLI;
using Darkages.Database;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;

using Gender = Darkages.Enums.Gender;

namespace Darkages.CommandSystem;

public static class Commander
{
    static Commander()
    {
        ServerSetup.Instance.Parser = CommandParser.CreateNew().UsePrefix().OnError(OnParseError);
    }

    public static void CompileCommands()
    {
        #region Server Maintenance

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Restart", "restart", "- Force restart and reload:")
            .SetAction(Restart));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Chaos", "chaos", "- Force shutdown:")
            .SetAction(Chaos));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Cancel Chaos", "cc", "- Halt shutdown:")
            .SetAction(CancelChaos));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Reload Maps", "rm", "- Reload all maps:")
            .SetAction(OnMapReload));

        #endregion

        #region GM Control

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Learn Spell", "spell")
            .SetAction(OnLearnSpell)
            .AddArgument(Argument.Create("name"))
            .AddArgument(Argument.Create("level").MakeOptional().SetDefault(100)));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Learn Skill", "skill")
            .SetAction(OnLearnSkill)
            .AddArgument(Argument.Create("name"))
            .AddArgument(Argument.Create("level").MakeOptional().SetDefault(100)));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Teleport", "map")
            .AddAlias("t")
            .SetAction(OnTeleport)
            .AddArgument(Argument.Create("t"))
            .AddArgument(Argument.Create("x"))
            .AddArgument(Argument.Create("y")));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Create Item", "give")
            .SetAction(OnItemCreate)
            .AddArgument(Argument.Create("item"))
            .AddArgument(Argument.Create("amount").MakeOptional().SetDefault(1)));

        #endregion

        #region Interaction with Players

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Group", "group")
            .AddAlias("party")
            .SetAction(OnRemoteGroup)
            .AddArgument(Argument.Create("name")));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Summon Player", "s")
            .SetAction(OnSummonPlayer)
            .AddArgument(Argument.Create("who")));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Teleport to Player", "p")
            .SetAction(OnPortToPlayer)
            .AddArgument(Argument.Create("who")));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Sex Change", "sex")
            .SetAction(OnSexChange)
            .AddArgument(Argument.Create("who"))
            .AddArgument(Argument.Create("s")));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Kill Player", "kill")
            .SetAction(OnKillCommand)
            .AddArgument(Argument.Create("who")));

        #endregion
    }

    /// <summary>
    /// Freezes server routine for updates and maintenance
    /// </summary>
    private static void Chaos(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var death = client.Aisling.Username.Equals("death", StringComparison.InvariantCultureIgnoreCase);
        if (!death)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bRestricted GM Command - Shuts down server");
            return;
        }

        var players = ServerSetup.Instance.Game.Aislings;
        var playersList = players.ToList();
        ServerSetup.EventsLogger("--------------------------------------------", LogLevel.Warning);
        ServerSetup.EventsLogger("", LogLevel.Warning);
        ServerSetup.EventsLogger("--------------- Server Chaos ---------------", LogLevel.Warning);

        foreach (var connected in playersList)
        {
            connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bServer maintenance in 1 minute.");

            Task.Delay(15000).ContinueWith(ct =>
            {
                connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bServer maintenance in 45 seconds.");
            });
            Task.Delay(30000).ContinueWith(ct =>
            {
                connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bServer maintenance in 30 seconds.");
            });

            Task.Delay(45000).ContinueWith(ct =>
            {
                connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bServer maintenance in 15 seconds.");
            });

            Task.Delay(55000).ContinueWith(ct =>
            {
                connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bServer maintenance in 5 seconds.");
            });

            Task.Delay(58000).ContinueWith(ct =>
            {
                if (client.Aisling.GameMasterChaosCancel) return;
                connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bInvokes Chaos to rise{=g. -Server Shutdown-");
                connected.Client.SendServerMessage(ServerMessageType.ScrollWindow, "{=bChaos has risen.\n\n {=a During chaos, various updates will be performed. This can last anywhere between 1 to 5 minutes depending on the complexity of the update.");
            });

            Task.Delay(60000).ContinueWith(ct =>
            {
                if (!client.Aisling.GameMasterChaosCancel)
                    connected.Client.Disconnect();

                connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bChaos has been cancelled.");
            });
        }

        if (!client.Aisling.GameMasterChaosCancel)
            ServerSetup.Instance.Running = false;
    }

    /// <summary>
    /// Cancels chaos before it completes
    /// </summary>
    private static void CancelChaos(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var death = client.Aisling.Username.Equals("death", StringComparison.InvariantCultureIgnoreCase);
        if (!death)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bRestricted GM Command - Cancel Chaos");
            return;
        }

        ServerSetup.EventsLogger("Chaos Cancelled", LogLevel.Warning);
        client.Aisling.GameMasterChaosCancel = !client.Aisling.GameMasterChaosCancel;
    }


    /// <summary>
    /// Restarts server by forcing an Exit Program command
    /// </summary>
    public static void Restart(Argument[] args, object arg)
    {
        var players = ServerSetup.Instance.Game.Aislings.ToList();
        ServerSetup.EventsLogger("---------------------------------------------", LogLevel.Warning);
        ServerSetup.EventsLogger("", LogLevel.Warning);
        ServerSetup.EventsLogger("------------- Server Restart Initiated -------------", LogLevel.Warning);

        // Announce to all players
        foreach (var connected in players)
        {
            connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=g-Server Restart-");
        }

        // Halts all components and routines
        ServerSetup.Instance.Running = false;

        // Exit program so Daemon can reboot it
        Environment.Exit(0);
    }

    /// <summary>
    /// In Game Usage : /spell "Spell Name" 100
    /// Learns a spell
    /// </summary>
    private static void OnLearnSpell(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Spell- on character: {client.Aisling.Username}");
        var name = args.FromName("name").Replace("\"", "");
        var level = args.FromName("level");

        if (int.TryParse(level, out var spellLevel))
        {
            var spell = Spell.GiveTo(client.Aisling, name, spellLevel);
            client.SystemMessage(spell ? $"Learned: {name}" : "Failed");
        }
        else
        {
            client.SystemMessage("Failed");
        }

        client.LoadSpellBook();
    }

    /// <summary>
    /// In Game Usage : /skill "Skill Name" 100
    /// Learns a skill
    /// </summary>
    private static void OnLearnSkill(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Skill- on character: {client.Aisling.Username}");
        var name = args.FromName("name").Replace("\"", "");
        var level = args.FromName("level");

        if (int.TryParse(level, out var skillLevel))
        {
            var skill = Skill.GiveTo(client.Aisling, name, skillLevel);
            client.SystemMessage(skill ? $"Learned: {name}" : "Failed");
        }
        else
        {
            client.SystemMessage("Failed");
        }

        client.LoadSkillBook();
    }

    /// <summary>
    /// InGame Usage : /s "playerName" - Summons a player
    /// </summary>
    private static void OnSummonPlayer(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var who = args.FromName("who").Replace("\"", "");
        if (string.IsNullOrEmpty(who)) return;
        var players = ServerSetup.Instance.Game.Aislings;
        var player = players.FirstOrDefault(i => i != null && string.Equals(i.Username, who, StringComparison.CurrentCultureIgnoreCase));
        if (player == null) return;
        if (!player.GameSettings.GMPort)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{player.Username}, has requested not to be summoned.");
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"GM {client.Aisling.Username} wished to summon you, but was told you were busy");
            return;
        }
        player.Client.TransitionToMap(client.Aisling.Map, client.Aisling.Position);
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Summon- on character: {client.Aisling.Username}, Summoned: {player?.Username}");
    }

    /// <summary>
    /// InGame Usage : /p "playerName" - Ports to a player
    /// </summary>
    private static void OnPortToPlayer(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var who = args.FromName("who").Replace("\"", "");
        if (string.IsNullOrEmpty(who)) return;
        var players = ServerSetup.Instance.Game.Aislings;
        var player = players.FirstOrDefault(i => i != null && string.Equals(i.Username, who, StringComparison.CurrentCultureIgnoreCase));
        if (player != null) client.TransitionToMap(player.Map, player.Position);
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Port- on character: {client.Aisling.Username}, Ported: {player?.Username}");
    }

    /// <summary>
    /// InGame Usage : /sex "playerName" 1 
    /// </summary>
    private static void OnSexChange(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var who = args.FromName("who").Replace("\"", "");
        if (!int.TryParse(args.FromName("s"), out var sResult)) return;
        if (string.IsNullOrEmpty(who)) return;
        var players = ServerSetup.Instance.Game.Aislings;
        var player = players.FirstOrDefault(i => i != null && string.Equals(i.Username, who, StringComparison.CurrentCultureIgnoreCase));
        if (player != null) player.Gender = (Gender)sResult;
        client.ClientRefreshed();
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Sex Change- on character: {client.Aisling.Username}");
    }

    /// <summary>
    /// InGame Usage : /kill "playerName"
    /// </summary>
    private static void OnKillCommand(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var who = args.FromName("who").Replace("\"", "");
        if (string.IsNullOrEmpty(who)) return;
        var players = ServerSetup.Instance.Game.Aislings;
        var player = players.FirstOrDefault(i => i != null && string.Equals(i.Username, who, StringComparison.CurrentCultureIgnoreCase));
        client.KillPlayer(client.Aisling.Map, player?.Username);
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Kill- on character: {client.Aisling.Username}");
    }

    /// <summary>
    /// InGame Usage : /group "playerName"  
    /// </summary>
    private static void OnRemoteGroup(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var who = args.FromName("who").Replace("\"", "");
        if (string.IsNullOrEmpty(who)) return;
        var players = ServerSetup.Instance.Game.Aislings;
        var player = players.FirstOrDefault(i => i != null && string.Equals(i.Username, who, StringComparison.CurrentCultureIgnoreCase));
        if (player != null)
        {
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
        }

        SentrySdk.CaptureMessage($"{client.RemoteIp} remotely grouped character: {client.Aisling.Username}");
    }

    /// <summary>
    /// InGame Usage : /t or /map "Abel Dungeon 2-1" 35 36
    /// </summary>
    private static void OnTeleport(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        var mapName = args.FromName("t").Replace("\"", "");
        if (!int.TryParse(args.FromName("x"), out var x) || !int.TryParse(args.FromName("y"), out var y)) return;
        var (_, area) = ServerSetup.Instance.GlobalMapCache.FirstOrDefault(i => i.Value.Name == mapName);
        if (area == null) return;
        client.TransitionToMap(area, new Position(x, y));
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Port- on character: {client.Aisling.Username}");
    }

    /// <summary>
    /// In Game Usage : /rm
    /// Reloads all maps
    /// </summary>
    private static void OnMapReload(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Reload Maps- on character: {client.Aisling.Username}");
        var players = ServerSetup.Instance.Game.Aislings;
        ServerSetup.EventsLogger("---------------------------------------------", LogLevel.Warning);
        ServerSetup.EventsLogger("------------- Maps Reloaded -------------", LogLevel.Warning);

        // Wipe
        ServerSetup.Instance.TempGlobalMapCache = new Dictionary<int, Area>();
        ServerSetup.Instance.TempGlobalWarpTemplateCache = new();

        foreach (var mon in ServerSetup.Instance.GlobalMonsterCache.Values)
        {
            ServerSetup.Instance.Game.ObjectHandlers.DelObject(mon);
        }
        ServerSetup.Instance.GlobalMonsterCache = new ConcurrentDictionary<uint, Monster>();

        foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
        {
            ServerSetup.Instance.Game.ObjectHandlers.DelObject(npc);
        }
        ServerSetup.Instance.GlobalMundaneCache = new ConcurrentDictionary<uint, Mundane>();

        // Reload
        AreaStorage.Instance.CacheFromDatabase();
        DatabaseLoad.CacheFromDatabase(new WarpTemplate());

        foreach (var connected in players)
        {
            connected.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=q{client.Aisling.Username} Invokes Reload Maps");
            connected.Client.ClientRefreshed();
        }
    }

    /// <summary>
    /// InGame Usage : /give "Dark Belt" 3
    /// InGame Usage : /give "Raw Beryl" 33
    /// InGame Usage : /give "Hy-Brasyl Battle Axe" 
    /// </summary>
    private static void OnItemCreate(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        SentrySdk.CaptureMessage($"{client.RemoteIp} used GM Command -Item Create- on character: {client.Aisling.Username}");

        var name = args.FromName("item").Replace("\"", "");
        if (!int.TryParse(args.FromName("amount"), out var quantity)) return;
        if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(name)) return;
        var template = ServerSetup.Instance.GlobalItemTemplateCache[name];
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
            {
                var item = new Item();
                if (client.Aisling.Inventory.IsFull)
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYour inventory is full");
                    return;
                }
                item = item.Create(client.Aisling, template);
                item.Stacks = (ushort)remaining;
                item.GiveTo(client.Aisling);
            }
        }
        else
        {
            var item = new Item();
            var quality = Item.Quality.Common;
            var variance = Item.Variance.None;
            var wVariance = Item.WeaponVariance.None;
            
            for (var i = 0; i < quantity; i++)
            {
                if (client.Aisling.Inventory.IsFull)
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYour inventory is full");
                    return;
                }

                if (template.Enchantable)
                {
                    quality = ItemQualityVariance.DetermineHighQuality();
                    variance = ItemQualityVariance.DetermineVariance();
                    wVariance = ItemQualityVariance.DetermineWeaponVariance();
                }

                item = item.Create(client.Aisling, template, quality, variance, wVariance);
                ItemQualityVariance.ItemDurability(item, quality);
                item.GiveTo(client.Aisling);
            }
        }
    }
    
    public static void ParseChatMessage(WorldClient client, string message) => ServerSetup.Instance.Parser?.Parse(message, client);

    private static void OnParseError(object obj, string command) => ServerSetup.EventsLogger($"[Chat Parser] Error: {command}");
}