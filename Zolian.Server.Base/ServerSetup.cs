using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using Darkages.Database;
using Darkages.Interfaces;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Server;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Systems;
using Darkages.Systems.CLI;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Darkages;

public class ServerSetup : IServerContext
{
    public static ServerSetup Instance { get; private set; }
    public static readonly object SyncLock = new();
    public static readonly ConcurrentDictionary<int, string> Redirects = new();
    private static Board[] _huntingToL = new Board[1];
    private static Board[] _trashTalk = new Board[1];
    private static Board[] _arenaUpdates = new Board[1];
    public static Board[] PersonalBoards = new Board[3];
    private static Board[] _serverUpdates = new Board[1];
    private static ILogger<ServerSetup> _log;
    public static IOptions<ServerOptions> ServerOptions;
    private static LoginServer _lobby;

    #region Properties

    public bool Running { get; set; }
    public IServerConstants Config { get; set; }
    public GameServer Game { get; set; }
    public CommandParser Parser { get; set; }
    public string StoragePath { get; set; }
    public string MoonPhase { get; set; }
    public string KeyCode { get; set; }
    public string Unlock { get; set; }
    public IPAddress IpAddress { get; set; }
    public ConcurrentDictionary<int, byte> EncryptKeyConDict { get; set; }
    public List<Metafile> GlobalMetaCache { get; set; }

    // Template
    public ConcurrentDictionary<int, WorldMapTemplate> GlobalWorldMapTemplateCache { get; set; }
    public ConcurrentDictionary<int, WarpTemplate> GlobalWarpTemplateCache { get; set; }
    public ConcurrentDictionary<string, SkillTemplate> GlobalSkillTemplateCache { get; set; }
    public ConcurrentDictionary<string, SpellTemplate> GlobalSpellTemplateCache { get; set; }
    public ConcurrentDictionary<string, ItemTemplate> GlobalItemTemplateCache { get; set; }
    public ConcurrentDictionary<string, NationTemplate> GlobalNationTemplateCache { get; set; }
    public ConcurrentDictionary<string, MonsterTemplate> GlobalMonsterTemplateCache { get; set; }
    public ConcurrentDictionary<string, MundaneTemplate> GlobalMundaneTemplateCache { get; set; }

    // Live
    public ConcurrentDictionary<int, Area> GlobalMapCache { get; set; } = new();
    public ConcurrentDictionary<string, Buff> GlobalBuffCache { get; set; } = new();
    public ConcurrentDictionary<string, Debuff> GlobalDeBuffCache { get; set; } = new();
    public ConcurrentDictionary<string, List<Board>> GlobalBoardCache { get; set; } = new();
    public ConcurrentDictionary<int, Party> GlobalGroupCache { get; set; } = new();
    public ConcurrentDictionary<string, MonsterScript> GlobalMonsterScriptCache { get; set; } = new();
    public ConcurrentDictionary<int, Monster> GlobalMonsterCache { get; set; } = new();
    public ConcurrentDictionary<string, MundaneScript> GlobalMundaneScriptCache { get; set; } = new();
    public ConcurrentDictionary<int, Mundane> GlobalMundaneCache { get; set; } = new();
    public ConcurrentDictionary<int, IDictionary<Type, object>> SpriteCollections { get; set; } = new();

    #endregion

    public ServerSetup(IOptions<ServerOptions> options)
    {
        Instance = this;
        ServerOptions = options;
        StoragePath = ServerOptions.Value.Location;
        KeyCode = ServerOptions.Value.KeyCode;
        Unlock = ServerOptions.Value.Unlock;
    }

    public static void Logger(string logMessage, LogLevel logLevel = LogLevel.Information)
    {
        lock (SyncLock)
        {
            _log?.Log(logLevel, "{logMessage}", logMessage);
        }
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
        _log = logger;

        Commander.CompileCommands();

        Startup();
        CommandHandler();
    }

    public void Startup()
    {
        try
        {
            LoadAndCacheStorage();
            StartServers();
        }
        catch (Exception ex)
        {
            Logger(ex.Message, LogLevel.Error);
            Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }
    }

    public void LoadAndCacheStorage(bool contentOnly = false)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;

        lock (SyncLock)
        {
            EmptyCacheCollectors();
            AreaStorage.Instance.CacheFromDatabase();
            StorageManager.NationBucket.CacheFromDatabase(new NationTemplate());
            StorageManager.SkillBucket.CacheFromDatabase(new SkillTemplate());
            StorageManager.SpellBucket.CacheFromDatabase(new SpellTemplate());
            StorageManager.ItemBucket.CacheFromDatabase(new ItemTemplate());
            StorageManager.MonsterBucket.CacheFromDatabase(new MonsterTemplate());
            StorageManager.MundaneBucket.CacheFromDatabase(new MundaneTemplate());
            StorageManager.WarpBucket.CacheFromDatabase(new WarpTemplate());

            LoadWorldMapTemplates();
            CacheCommunityAssets();

            if (contentOnly) return;

            BindTemplates();
            // If decompiling templates, comment out LoadMetaDatabase();
            LoadMetaDatabase();
            //MetafileManager.DecompileTemplates();
            LoadExtensions();
        }
    }

    public void EmptyCacheCollectors()
    {
        GlobalMapCache = new ConcurrentDictionary<int, Area>();
        GlobalMetaCache = new List<Metafile>();
        GlobalItemTemplateCache = new ConcurrentDictionary<string, ItemTemplate>();
        GlobalNationTemplateCache = new ConcurrentDictionary<string, NationTemplate>();
        GlobalMonsterTemplateCache = new ConcurrentDictionary<string, MonsterTemplate>();
        GlobalMonsterCache = new ConcurrentDictionary<int, Monster>();
        GlobalMundaneTemplateCache = new ConcurrentDictionary<string, MundaneTemplate>();
        GlobalSkillTemplateCache = new ConcurrentDictionary<string, SkillTemplate>();
        GlobalSpellTemplateCache = new ConcurrentDictionary<string, SpellTemplate>();
        GlobalWarpTemplateCache = new ConcurrentDictionary<int, WarpTemplate>();
        GlobalWorldMapTemplateCache = new ConcurrentDictionary<int, WorldMapTemplate>();
        GlobalBuffCache = new ConcurrentDictionary<string, Buff>();
        GlobalDeBuffCache = new ConcurrentDictionary<string, Debuff>();
        GlobalBoardCache = new ConcurrentDictionary<string, List<Board>>();
    }

    #region Template Building

    public void LoadWorldMapTemplates()
    {
        StorageManager.WorldMapBucket.CacheFromStorage();
        Logger($"World Map Templates: {GlobalWorldMapTemplateCache.Count}");
    }

    public void BindTemplates()
    {
        foreach (var spell in GlobalSpellTemplateCache.Values)
            spell.Prerequisites?.AssociatedWith(spell);
        foreach (var skill in GlobalSkillTemplateCache.Values)
            skill.Prerequisites?.AssociatedWith(skill);
    }

    #endregion

    public void CacheCommunityAssets()
    {
        if (PersonalBoards == null) return;
        var dirs = Directory.GetDirectories(Path.Combine(StoragePath, "Community\\Boards"));
        var tmpBoards = new Dictionary<string, List<Board>>();

        foreach (var dir in dirs.Select(i => new DirectoryInfo(i)))
        {
            var boards = Board.CacheFromStorage(dir.FullName);

            if (boards == null) continue;

            if (dir.Name == "Personal")
                if (boards.Find(i => i.Index == 0) == null)
                    boards.Add(new Board("Mail", 0, true));

            if (!tmpBoards.ContainsKey(dir.Name)) tmpBoards[dir.Name] = new List<Board>();

            tmpBoards[dir.Name].AddRange(boards);
        }

        PersonalBoards = tmpBoards["Personal"].OrderBy(i => i.Index).ToArray();
        _huntingToL = tmpBoards["Hunting"].OrderBy(i => i.Index).ToArray();
        _arenaUpdates = tmpBoards["Arena Updates"].OrderBy(i => i.Index).ToArray();
        _trashTalk = tmpBoards["Trash Talk"].OrderBy(i => i.Index).ToArray();
        _serverUpdates = tmpBoards["Server Updates"].OrderBy(i => i.Index).ToArray();

        foreach (var (key, value) in tmpBoards)
        {
            if (!GlobalBoardCache.ContainsKey(key)) GlobalBoardCache[key] = new List<Board>();

            GlobalBoardCache[key].AddRange(value);
        }
    }

    public void LoadMetaDatabase()
    {
        try
        {
            var files = MetafileManager.GetMetaFiles();
            if (files.Any()) GlobalMetaCache.AddRange(files);
        }
        catch (Exception ex)
        {
            Logger(ex.Message, LogLevel.Error);
            Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }
    }

    public void LoadExtensions()
    {
        CacheBuffs();
        Logger($"Buff Cache: {GlobalBuffCache.Count}");
        CacheDebuffs();
        Logger($"Debuff Cache: {GlobalDeBuffCache.Count}");
    }

    public void CacheBuffs()
    {
        var listOfBuffs = from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
            from assemblyType in domainAssembly.GetTypes()
            where typeof(Buff).IsAssignableFrom(assemblyType)
            select assemblyType;

        foreach (var buff in listOfBuffs)
        {
            if (GlobalBuffCache != null)
                GlobalBuffCache[buff.Name] = Activator.CreateInstance(buff) as Buff;
        }
    }

    public void CacheDebuffs()
    {
        var listOfDebuffs = from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
            from assemblyType in domainAssembly.GetTypes()
            where typeof(Debuff).IsAssignableFrom(assemblyType)
            select assemblyType;

        foreach (var debuff in listOfDebuffs)
        {
            if (GlobalDeBuffCache != null)
                GlobalDeBuffCache[debuff.Name] = Activator.CreateInstance(debuff) as Debuff;
        }
    }

    /// <summary>
    /// Game.Start starts the game server from GameServer.cs it then calls the Start method from
    /// NetworkServer to enable the socket. _lobby.Start starts the lobby server from directly
    /// calling NetworkServer to enable the socket.
    /// </summary>
    public void StartServers()
    {
        try
        {
            Game = new GameServer(Config.ConnectionCapacity);
            Game.Start(Config.SERVER_PORT);
            _lobby = new LoginServer();
            _lobby.Start(Config.LOGIN_PORT);

            Console.ForegroundColor = ConsoleColor.Green;
            Logger("Server is now online.");
        }
        catch (SocketException ex)
        {
            Logger(ex.Message, LogLevel.Error);
            Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
        }
    }

    public void CommandHandler()
    {
        Console.WriteLine("\n");
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine("GM Commands");

        foreach (var command in Parser.Commands)
        {
            Logger(command.ShowHelp(), LogLevel.Warning);
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
    }

    public static void SaveCommunityAssets()
    {
        lock (SyncLock)
        {
            var tmp = new List<Board>(_arenaUpdates);
            var tmp1 = new List<Board>(_huntingToL);
            var tmp2 = new List<Board>(PersonalBoards);
            var tmp3 = new List<Board>(_serverUpdates);
            var tmp4 = new List<Board>(_trashTalk);

            foreach (var asset in tmp)
            {
                asset.Save("Arena Updates");
            }

            foreach (var asset in tmp1)
            {
                asset.Save("Hunting");
            }

            foreach (var asset in tmp2)
            {
                asset.Save("Personal");
            }

            foreach (var asset in tmp3)
            {
                asset.Save("Server Updates");
            }

            foreach (var asset in tmp4)
            {
                asset.Save("Trash Talk");
            }
        }
    }
}