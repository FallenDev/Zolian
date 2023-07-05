using System.Net;

using Darkages.GameScripts.Formulas;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Systems.CLI;
using Darkages.Types;

using Microsoft.AppCenter.Analytics;
using Microsoft.Extensions.Logging;

namespace Darkages.Systems;

public static class Commander
{
    static Commander()
    {
        ServerSetup.Instance.Parser = CommandParser.CreateNew().UsePrefix().OnError(OnParseError);
    }

    public static void CompileCommands()
    {
        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Create Item")
            .AddAlias("give")
            .SetAction(OnItemCreate)
            .AddArgument(Argument.Create("item"))
            .AddArgument(Argument.Create("amount").MakeOptional().SetDefault(1))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Teleport")
            .AddAlias("map")
            .SetAction(OnTeleport)
            .AddArgument(Argument.Create("map"))
            .AddArgument(Argument.Create("x"))
            .AddArgument(Argument.Create("y"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Summon Player")
            .AddAlias("s")
            .SetAction(OnSummonPlayer)
            .AddArgument(Argument.Create("who"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Port to Player")
            .AddAlias("p")
            .SetAction(OnPortToPlayer)
            .AddArgument(Argument.Create("who"))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Learn Spell")
            .AddAlias("spell")
            .SetAction(OnLearnSpell)
            .AddArgument(Argument.Create("name"))
            .AddArgument(Argument.Create("level").MakeOptional().SetDefault(100))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Learn Skill")
            .AddAlias("skill")
            .SetAction(OnLearnSkill)
            .AddArgument(Argument.Create("name"))
            .AddArgument(Argument.Create("level").MakeOptional().SetDefault(100))
        );

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Learn All")
            .AddAlias("learn")
            .SetAction(LearnAll));

        ServerSetup.Instance.Parser.AddCommand(Command
            .Create("Restart")
            .AddAlias("restart")
            .SetAction(Restart));
    }

    private static void LearnAll(Argument[] args, object arg)
    {
        var client = (GameClient)arg;
        if (client == null) return;
        var ip = client.Socket.RemoteEndPoint as IPEndPoint;

        client.LearnEverything();

        Analytics.TrackEvent($"{ip!.Address} used GM Command -Learn- on character: {client.Aisling.Username}");
    }

    public static void Chaos()
    {
        var clients = ServerSetup.Instance.Game.Clients.Values;

        foreach (var connected in clients)
        {
            connected.SendMessage(0x0C, "{=bChaos is rising in {=q5 {=bminutes{=g.");
        }

        Task.Delay(60000).ContinueWith(ct =>
        {
            foreach (var connected in clients)
            {
                connected.SendMessage(0x0C, "{=bChaos is nearing in {=q4 {=bminutes{=g.");
            }
        });

        Task.Delay(120000).ContinueWith(ct =>
        {
            foreach (var connected in clients)
            {
                connected.SendMessage(0x0C, "{=bChaos is nearing in {=q3 {=bminutes{=g.");
            }
        });

        Task.Delay(180000).ContinueWith(ct =>
        {
            foreach (var connected in clients)
            {
                connected.SendMessage(0x0C, "{=bChaos is nearing in {=q2 {=bminutes{=g.");
            }
        });

        Task.Delay(240000).ContinueWith(ct =>
        {
            foreach (var connected in clients)
            {
                connected.SendMessage(0x0C, "{=bDeath Knell will be rung in {=q1 {=bminute{=g.");
            }
        });

        Task.Delay(300000).ContinueWith(ct =>
        {
            foreach (var connected in clients)
            {
                connected.SendMessage(0x0C, "{=bChaos has risen{=g. -{=qPlease re{=g-{=qlogin{=g-");
            }

            ServerSetup.Logger("Chaos has risen.", LogLevel.Critical);
            ServerSetup.Instance.Running = false;
            Environment.Exit(0);
        });
    }

    public static void Restart(Argument[] args, object arg)
    {
        var clients = ServerSetup.Instance.Game.Clients.Values;
        ServerSetup.Logger("---------------------------------------------", LogLevel.Warning);
        ServerSetup.Logger("", LogLevel.Warning);
        ServerSetup.Logger("------------- Server Restart Initiated -------------", LogLevel.Warning);

        foreach (var connected in clients)
        {
            connected.SendMessage(0x0C, "{=qDeath{=g: {=bInvokes Chaos to rise{=g. -Server Restart-");
            connected.SendMessage(0x08, "{=bChaos has risen.\n\n {=a During chaos, various updates will be performed. This can last anywhere between 1 to 5 minutes depending on the complexity of the update.");
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
        var client = (GameClient)arg;
        if (client == null) return;
        var ip = client.Socket.RemoteEndPoint as IPEndPoint;

        Analytics.TrackEvent($"{ip!.Address} used GM Command -Spell- on character: {client.Aisling.Username}");

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
        var client = (GameClient)arg;
        if (client == null) return;
        var ip = client.Socket.RemoteEndPoint as IPEndPoint;

        Analytics.TrackEvent($"{ip!.Address} used GM Command -Skill- on character: {client.Aisling.Username}");

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
    /// InGame Usage : /sp "Wren"
    /// </summary>
    private static void OnSummonPlayer(Argument[] args, object arg)
    {
        var client = (GameClient)arg;
        if (client == null) return;

        var who = args.FromName("who").Replace("\"", "");

        if (string.IsNullOrEmpty(who)) return;

        var player = client.Server.Clients.Values.FirstOrDefault(i =>
            i?.Aisling != null && string.Equals(i.Aisling.Username, who, StringComparison.CurrentCultureIgnoreCase));

        //summon player to my map and position.
        player?.TransitionToMap(client.Aisling.Map, client.Aisling.Position);
        var ip = client.Socket.RemoteEndPoint as IPEndPoint;

        Analytics.TrackEvent($"{ip!.Address} used GM Command -Summon- on character: {client.Aisling.Username}, Summoned: {player?.Aisling.Username}");
    }

    /// <summary>
    /// InGame Usage : /pt "Wren"
    /// </summary>
    private static void OnPortToPlayer(Argument[] args, object arg)
    {
        var client = (GameClient)arg;

        if (client == null) return;
        var who = args.FromName("who").Replace("\"", "");

        if (string.IsNullOrEmpty(who))
            return;

        var player = client.Server.Clients.Values.FirstOrDefault(i =>
            i?.Aisling != null && string.Equals(i.Aisling.Username, who, StringComparison.CurrentCultureIgnoreCase));

        //summon myself to players area and position.
        if (player != null)
            client.TransitionToMap(player.Aisling.Map, player.Aisling.Position);
        var ip = client.Socket.RemoteEndPoint as IPEndPoint;

        Analytics.TrackEvent($"{ip!.Address} used GM Command -Port- on character: {client.Aisling.Username}, Ported: {player?.Aisling.Username}");
    }

    /// <summary>
    /// InGame Usage : /tp "Abel Dungeon 2-1" 35 36
    /// </summary>
    private static void OnTeleport(Argument[] args, object arg)
    {
        var client = (GameClient)arg;

        if (client == null) return;
        var mapName = args.FromName("map").Replace("\"", "");

        if (!int.TryParse(args.FromName("x"), out var x) ||
            !int.TryParse(args.FromName("y"), out var y)) return;

        var (_, area) = ServerSetup.Instance.GlobalMapCache.FirstOrDefault(i => i.Value.Name == mapName);

        if (area == null) return;
        client.TransitionToMap(area, new Position(x, y));
        var ip = client.Socket.RemoteEndPoint as IPEndPoint;

        Analytics.TrackEvent($"{ip!.Address} used GM Command -Port- on character: {client.Aisling.Username}");
    }

    /// <summary>
    /// InGame Usage : /give "Dark Belt" 3
    /// InGame Usage : /give "Raw Beryl" 33
    /// InGame Usage : /give "Hy-Brasyl Battle Axe" 
    /// </summary>
    private static void OnItemCreate(Argument[] args, object arg)
    {
        var client = (GameClient)arg;
        if (client == null) return;
        var ip = client.Socket.RemoteEndPoint as IPEndPoint;

        Analytics.TrackEvent($"{ip!.Address} used GM Command -Item Create- on character: {client.Aisling.Username}");

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
                var quality = ItemQualityVariance.DetermineQuality();
                var variance = ItemQualityVariance.DetermineVariance();
                var wVariance = ItemQualityVariance.DetermineWeaponVariance();
                var item = new Item();

                item = item.Create(client.Aisling, template, quality, variance, wVariance);
                ItemDura(item, quality, client);
                if (!item.CanCarry(client.Aisling)) continue;
                item.GiveTo(client.Aisling);
            }
        }
    }

    private static void ItemDura(Item item, Item.Quality quality, IGameClient client)
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

    public static void ParseChatMessage(IGameClient client, string message) => ServerSetup.Instance.Parser?.Parse(message, client);

    private static void OnParseError(object obj, string command) =>
        ServerSetup.Logger($"[Chat Parser] Error: {command}");
}