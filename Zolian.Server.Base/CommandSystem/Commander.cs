using System.Collections.Concurrent;
using Chaos.Common.Definitions;
using Darkages.CommandSystem.CLI;
using Darkages.Database;
using Darkages.GameScripts.Formulas;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;
using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.Logging;
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
        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Group", "group")
            .AddAlias("party")
            .SetAction(OnRemoteGroup)
            .AddArgument(Argument.Create("name"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Create Item", "give")
            .SetAction(OnItemCreate)
            .AddArgument(Argument.Create("item"))
            .AddArgument(Argument.Create("amount").MakeOptional().SetDefault(1))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Teleport", "map")
            .AddAlias("t")
            .SetAction(OnTeleport)
            .AddArgument(Argument.Create("t"))
            .AddArgument(Argument.Create("x"))
            .AddArgument(Argument.Create("y"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Summon Player", "s")
            .SetAction(OnSummonPlayer)
            .AddArgument(Argument.Create("who"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Teleport to Player", "p")
            .SetAction(OnPortToPlayer)
            .AddArgument(Argument.Create("who"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Sex Change", "sex")
            .SetAction(OnSexChange)
            .AddArgument(Argument.Create("who"))
            .AddArgument(Argument.Create("s"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Learn Spell", "spell")
            .SetAction(OnLearnSpell)
            .AddArgument(Argument.Create("name"))
            .AddArgument(Argument.Create("level").MakeOptional().SetDefault(100))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Learn Skill", "skill")
            .SetAction(OnLearnSkill)
            .AddArgument(Argument.Create("name"))
            .AddArgument(Argument.Create("level").MakeOptional().SetDefault(100))
        );
        
        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Restart", "restart", "- Force restart and reload:")
            .SetAction(Restart)
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Chaos", "chaos", "- Force shutdown:")
            .SetAction(Chaos)
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Reload Maps", "rm", "- Reload all maps:")
            .SetAction(OnMapReload)
        );
    }

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
        ServerSetup.Logger("--------------------------------------------", LogLevel.Warning);
        ServerSetup.Logger("", LogLevel.Warning);
        ServerSetup.Logger("--------------- Server Chaos ---------------", LogLevel.Warning);

        foreach (var connected in players)
        {
            _ = StorageManager.AislingBucket.QuickSave(connected);
            _ = connected.Client.Save();
            connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bInvokes Chaos to rise{=g. -Server Shutdown-");
            connected.Client.SendServerMessage(ServerMessageType.ScrollWindow, "{=bChaos has risen.\n\n {=a During chaos, various updates will be performed. This can last anywhere between 1 to 5 minutes depending on the complexity of the update.");
            Task.Delay(500).ContinueWith(ct =>
            {
                connected.Client.Disconnect();
            });
        }

        ServerSetup.Instance.Running = false;
    }
    
    public static void Restart(Argument[] args, object arg)
    {
        var players = ServerSetup.Instance.Game.Aislings;
        ServerSetup.Logger("---------------------------------------------", LogLevel.Warning);
        ServerSetup.Logger("", LogLevel.Warning);
        ServerSetup.Logger("------------- Server Restart Initiated -------------", LogLevel.Warning);

        foreach (var connected in players)
        {
            _ = StorageManager.AislingBucket.QuickSave(connected);
            _ = connected.Client.Save();
            connected.Client.SendServerMessage(ServerMessageType.GroupChat, "{=qDeath{=g: {=bInvokes Order {=g. -Server Restart-");
        }

        ServerSetup.Instance.Running = false;
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
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Spell- on character: {client.Aisling.Username}");
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
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Skill- on character: {client.Aisling.Username}");
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
        player?.Client.TransitionToMap(client.Aisling.Map, client.Aisling.Position);
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Summon- on character: {client.Aisling.Username}, Summoned: {player?.Username}");
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
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Port- on character: {client.Aisling.Username}, Ported: {player?.Username}");
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
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Sex Change- on character: {client.Aisling.Username}");
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

        Analytics.TrackEvent($"{client.RemoteIp} remotely grouped character: {client.Aisling.Username}");
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
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Port- on character: {client.Aisling.Username}");
    }

    /// <summary>
    /// In Game Usage : /rm
    /// Reloads all maps
    /// </summary>
    private static void OnMapReload(Argument[] args, object arg)
    {
        var client = (WorldClient)arg;
        if (client == null) return;
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Reload Maps- on character: {client.Aisling.Username}");
        var players = ServerSetup.Instance.Game.Aislings;
        ServerSetup.Logger("---------------------------------------------", LogLevel.Warning);
        ServerSetup.Logger("------------- Maps Reloaded -------------", LogLevel.Warning);

        // Wipe
        ServerSetup.Instance.GlobalMapCache = new ConcurrentDictionary<int, Area>();
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
            connected.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qDeath Invokes Reload Maps");
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
        Analytics.TrackEvent($"{client.RemoteIp} used GM Command -Item Create- on character: {client.Aisling.Username}");

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
                if (!item.CanCarry(client.Aisling)) continue;
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
                if (!item.CanCarry(client.Aisling)) return;
                item.GiveTo(client.Aisling);
            }
        }
        else
        {
            for (var i = 0; i < quantity; i++)
            {
                var quality = ItemQualityVariance.DetermineHighQuality();
                var variance = ItemQualityVariance.DetermineVariance();
                var wVariance = ItemQualityVariance.DetermineWeaponVariance();
                var item = new Item();
                if (client.Aisling.Inventory.IsFull)
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYour inventory is full");
                    return;
                }
                item = item.Create(client.Aisling, template, quality, variance, wVariance);
                ItemDura(item, quality, client);
                if (!item.CanCarry(client.Aisling)) continue;
                item.GiveTo(client.Aisling);
            }
        }
    }

    private static void ItemDura(Item item, Item.Quality quality, WorldClient client)
    {
        var temp = item.Template.MaxDurability;
        switch (quality)
        {
            case Item.Quality.Damaged:
                item.MaxDurability = (uint)(temp / 1.4);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Common:
                item.MaxDurability = temp / 1;
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Uncommon:
                item.MaxDurability = (uint)(temp / 0.9);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Rare:
                item.MaxDurability = (uint)(temp / 0.8);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Epic:
                item.MaxDurability = (uint)(temp / 0.7);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Legendary:
                item.MaxDurability = (uint)(temp / 0.6);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Forsaken:
                item.MaxDurability = (uint)(temp / 0.5);
                item.Durability = item.MaxDurability;
                break;
            case Item.Quality.Mythic:
                item.MaxDurability = (uint)(temp / 0.3);
                item.Durability = item.MaxDurability;
                break;
        }
    }

    public static void ParseChatMessage(WorldClient client, string message) => ServerSetup.Instance.Parser?.Parse(message, client);

    private static void OnParseError(object obj, string command) => ServerSetup.Logger($"[Chat Parser] Error: {command}");
}