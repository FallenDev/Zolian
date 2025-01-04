using Darkages.CommandSystem;
using Darkages.CommandSystem.CLI;
using Darkages.Database;
using Darkages.Models;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Data;
using System.Net;
using System.Reflection;
using Chaos.Common.Identity;
using Darkages.Network.Server.Abstractions;
using Microsoft.Data.SqlClient;
using RestSharp;
using Darkages.Sprites.Entity;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Darkages.Network.Server;

public class ServerSetup : IServerContext
{
    public static ServerSetup Instance { get; private set; }
    private static ILogger<ServerSetup> _eventsLogger;
    private static Logger _packetLogger;
    public static IOptions<ServerOptions> ServerOptions;
    public readonly RestClient RestClient;
    public readonly RestClient RestReport;

    public bool Running { get; set; }
    public SqlConnection ServerSaveConnection { get; set; }
    public IServerConstants Config { get; set; }
    public WorldServer Game { get; set; }
    public LoginServer LoginServer { get; set; }
    public LobbyServer LobbyServer { get; set; }
    public CommandParser Parser { get; set; }
    public string StoragePath { get; set; }
    public string MoonPhase { get; set; }
    public byte LightPhase { get; set; }
    public byte LightLevel { get; set; }
    public string KeyCode { get; set; }
    public string Unlock { get; set; }
    public IPAddress IpAddress { get; set; }
    public string[] GameMastersIPs { get; set; }
    public string InternalAddress { get; set; }

    // Templates
    public FrozenDictionary<int, WorldMapTemplate> GlobalWorldMapTemplateCache { get; set; }
    public Dictionary<int, WorldMapTemplate> TempGlobalWorldMapTemplateCache { get; set; } = [];
    public FrozenDictionary<int, WarpTemplate> GlobalWarpTemplateCache { get; set; }
    public Dictionary<int, WarpTemplate> TempGlobalWarpTemplateCache { get; set; } = [];
    public FrozenDictionary<string, SkillTemplate> GlobalSkillTemplateCache { get; set; }
    public Dictionary<string, SkillTemplate> TempGlobalSkillTemplateCache { get; set; } = [];
    public FrozenDictionary<string, SpellTemplate> GlobalSpellTemplateCache { get; set; }
    public Dictionary<string, SpellTemplate> TempGlobalSpellTemplateCache { get; set; } = [];
    public FrozenDictionary<string, ItemTemplate> GlobalItemTemplateCache { get; set; }
    public Dictionary<string, ItemTemplate> TempGlobalItemTemplateCache { get; set; } = [];
    public FrozenDictionary<string, NationTemplate> GlobalNationTemplateCache { get; set; }
    public Dictionary<string, NationTemplate> TempGlobalNationTemplateCache { get; set; } = [];
    public FrozenDictionary<string, MonsterTemplate> GlobalMonsterTemplateCache { get; set; }
    public Dictionary<string, MonsterTemplate> TempGlobalMonsterTemplateCache { get; set; } = [];
    public FrozenDictionary<string, MundaneTemplate> GlobalMundaneTemplateCache { get; set; }
    public Dictionary<string, MundaneTemplate> TempGlobalMundaneTemplateCache { get; set; } = [];
    public FrozenDictionary<uint, string> GlobalKnownGoodActorsCache { get; set; }
    public Dictionary<uint, string> TempGlobalKnownGoodActorsCache { get; set; } = [];

    // Frozen Live
    public FrozenDictionary<int, Area> GlobalMapCache { get; set; }
    public Dictionary<int, Area> TempGlobalMapCache { get; set; } = [];

    // Live
    public ConcurrentDictionary<string, Buff> GlobalBuffCache { get; set; } = [];
    public ConcurrentDictionary<string, Debuff> GlobalDeBuffCache { get; set; } = [];
    public ConcurrentDictionary<int, BoardTemplate> GlobalBoardPostCache { get; set; } = [];
    public ConcurrentDictionary<int, Party> GlobalGroupCache { get; set; } = [];
    public ConcurrentDictionary<uint, Mundane> GlobalMundaneCache { get; set; } = [];
    public ConcurrentDictionary<long, Item> GlobalSqlItemCache { get; set; } = [];
    public ConcurrentDictionary<long, Money> GlobalGroundMoneyCache { get; set; } = [];
    public ConcurrentDictionary<uint, Trap> Traps { get; set; } = [];
    public ConcurrentDictionary<long, ConcurrentDictionary<string, KillRecord>> GlobalKillRecordCache { get; set; } = [];
    public ConcurrentDictionary<IPAddress, IPAddress> GlobalLobbyConnection { get; set; } = [];
    public ConcurrentDictionary<IPAddress, IPAddress> GlobalLoginConnection { get; set; } = [];
    public ConcurrentDictionary<IPAddress, IPAddress> GlobalWorldConnection { get; set; } = [];
    public ConcurrentDictionary<IPAddress, byte> GlobalCreationCount { get; set; } = [];
    public ConcurrentDictionary<IPAddress, byte> GlobalPasswordAttempt { get; set; } = [];

    public ServerSetup(IOptions<ServerOptions> options)
    {
        Instance = this;
        ServerOptions = options;
        StoragePath = ServerOptions.Value.Location;
        KeyCode = ServerOptions.Value.KeyCode;
        Unlock = ServerOptions.Value.Unlock;
        InternalAddress = ServerOptions.Value.InternalIp;
        GameMastersIPs = ServerOptions.Value.GameMastersIPs;
        var restSettings = SetupRestClients();
        RestClient = new RestClient(restSettings.Item1);
        RestReport = new RestClient(restSettings.Item2);
        BadActor.StartProcessingQueue();

        const string logTemplate = "[{Timestamp:MMM-dd HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}";
        _packetLogger = new LoggerConfiguration()
            .WriteTo.File("_Zolian_packets_.txt", LogEventLevel.Verbose, logTemplate, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static void ConnectionLogger(string logMessage, LogLevel logLevel = LogLevel.Information)
    {
        _eventsLogger?.Log(logLevel, "{logMessage}", logMessage);
    }

    public static void PacketLogger(string logMessage, LogLevel logLevel = LogLevel.Critical)
    {
        _packetLogger.Write(LogEventLevel.Error, logMessage);
    }

    public static void EventsLogger(string logMessage, LogLevel logLevel = LogLevel.Information)
    {
        _eventsLogger?.Log(logLevel, "{logMessage}", logMessage);
    }

    private static (RestClientOptions, RestClientOptions) SetupRestClients()
    {
        var optionsCheck = new RestClientOptions("https://api.abuseipdb.com/api/v2/check")
        {
            ThrowOnAnyError = true,
            Timeout = new TimeSpan(0, 0, 0, 5)
        };
        var optionsReport = new RestClientOptions("https://api.abuseipdb.com/api/v2/report")
        {
            ThrowOnAnyError = true,
            Timeout = new TimeSpan(0, 0, 0, 5)
        };

        return (optionsCheck, optionsReport);
    }

    public void InitFromConfig(string storagePath, string ipAddress)
    {
        IpAddress = IPAddress.Parse(ipAddress);
        StoragePath = storagePath;

        if (StoragePath != null && !Directory.Exists(StoragePath))
            Directory.CreateDirectory(StoragePath);
    }

    public void Start(IServerConstants config, ILogger<ServerSetup> logger)
    {
        Config = config;
        _eventsLogger = logger;
        Commander.CompileCommands();
        Startup();
        CommandHandler();
        DatabaseSaveConnection();
    }

    public void Startup()
    {
        try
        {
            LoadAndCacheStorage();
        }
        catch (Exception ex)
        {
            EventsLogger(ex.Message);
            EventsLogger(ex.StackTrace);
        }
    }

    public void LoadAndCacheStorage()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        AreaStorage.Instance.CacheFromDatabase();
        DatabaseLoad.CacheFromDatabase(new WorldMapTemplate());
        DatabaseLoad.CacheFromDatabase(new WarpTemplate());
        DatabaseLoad.CacheFromDatabase(new SkillTemplate());
        DatabaseLoad.CacheFromDatabase(new SpellTemplate());
        DatabaseLoad.CacheFromDatabase(new ItemTemplate());
        DatabaseLoad.CacheFromDatabase(new NationTemplate());
        DatabaseLoad.CacheFromDatabase(new MonsterTemplate());
        DatabaseLoad.CacheFromDatabase(new MundaneTemplate());
        DatabaseLoad.CacheFromDatabase(new BoardTemplate());
        BindTemplates();
        // ToDo: If decompiling templates, comment out LoadMetaDatabase();
        //MetafileManager.DecompileTemplates();
        LoadExtensions();
    }

    public void BindTemplates()
    {
        foreach (var spell in GlobalSpellTemplateCache.Values)
            spell.Prerequisites?.AssociatedWith(spell);
        foreach (var skill in GlobalSkillTemplateCache.Values)
            skill.Prerequisites?.AssociatedWith(skill);
    }

    public void LoadExtensions()
    {
        CacheBuffs();
        EventsLogger($"Buff Cache: {GlobalBuffCache.Count}");
        CacheDebuffs();
        EventsLogger($"Debuff Cache: {GlobalDeBuffCache.Count}");
    }

    public void CacheBuffs()
    {
        var buffType = typeof(Buff);
        var listOfBuffs = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => buffType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var buff in listOfBuffs)
        {
            if (buff.Name == "Buff") continue;
            if (GlobalBuffCache == null) continue;
            if (Activator.CreateInstance(buff) is Buff buffInstance)
            {
                GlobalBuffCache[buff.Name] = buffInstance;
            }
        }
    }

    public void CacheDebuffs()
    {
        var debuffType = typeof(Debuff);
        var listOfDebuffs = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => debuffType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var debuff in listOfDebuffs)
        {
            if (debuff.Name == "Debuff") continue;
            if (GlobalDeBuffCache == null) continue;
            if (Activator.CreateInstance(debuff) is Debuff debuffInstance)
            {
                GlobalDeBuffCache[debuff.Name] = debuffInstance;
            }
        }
    }

    public void CommandHandler()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine("GM Commands");
        Console.ForegroundColor = ConsoleColor.Magenta;

        foreach (var command in Parser.Commands)
        {
            Console.WriteLine(command.ShowHelp(), LogLevel.Debug);
        }
    }

    public void DatabaseSaveConnection()
    {
        ServerSaveConnection = new SqlConnection(AislingStorage.ConnectionString);
        ServerSaveConnection.Open();

        if (ServerSaveConnection.State == ConnectionState.Open)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Player Save-State Connected");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Issue connecting to database");
        }

        SetGoodActors();
    }

    public void SetGoodActors()
    {
        const string sql = "SELECT LastIP FROM ZolianPlayers.dbo.Players";
        var cmd = new SqlCommand(sql, ServerSaveConnection);
        cmd.CommandTimeout = 5;
        var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var iP = reader["LastIP"].ToString();
            TempGlobalKnownGoodActorsCache.TryAdd(EphemeralRandomIdGenerator<uint>.Shared.NextId, iP);
        }

        GlobalKnownGoodActorsCache = TempGlobalKnownGoodActorsCache.ToFrozenDictionary();
        TempGlobalKnownGoodActorsCache.Clear();
    }
}