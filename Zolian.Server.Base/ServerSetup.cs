using System.Collections.Concurrent;
using System.Net;
using System.Reflection;

using Darkages.CommandSystem;
using Darkages.CommandSystem.CLI;
using Darkages.Database;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Server;
using Darkages.Sprites;
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
    private static readonly object LogLock = new();
    private static Board[] _huntingToL = new Board[1];
    private static Board[] _trashTalk = new Board[1];
    private static Board[] _arenaUpdates = new Board[1];
    public static Board[] PersonalBoards = new Board[3];
    private static Board[] _serverUpdates = new Board[1];
    private static ILogger<ServerSetup> _log;
    public static IOptions<ServerOptions> ServerOptions;

    #region Properties

    public bool Running { get; set; }
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
    public string InternalAddress { get; set; }

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
    public ConcurrentDictionary<uint, Monster> GlobalMonsterCache { get; set; } = new();
    public ConcurrentDictionary<uint, Mundane> GlobalMundaneCache { get; set; } = new();
    public ConcurrentDictionary<long, Item> GlobalGroundItemCache { get; set; } = new();
    public ConcurrentDictionary<int, IDictionary<Type, object>> SpriteCollections { get; set; } = new();
    public ConcurrentDictionary<uint, Trap> Traps { get; set; } = new();
    public ConcurrentDictionary<IPAddress, IPAddress> GlobalLobbyConnection { get; set; } = new();
    public ConcurrentDictionary<IPAddress, IPAddress> GlobalLoginConnection { get; set; } = new();
    public ConcurrentDictionary<IPAddress, IPAddress> GlobalWorldConnection { get; set; } = new();

    #endregion

    public ServerSetup(IOptions<ServerOptions> options)
    {
        Instance = this;
        ServerOptions = options;
        StoragePath = ServerOptions.Value.Location;
        KeyCode = ServerOptions.Value.KeyCode;
        Unlock = ServerOptions.Value.Unlock;
        InternalAddress = ServerOptions.Value.InternalIp;
    }

    public static void Logger(string logMessage, LogLevel logLevel = LogLevel.Information)
    {
        lock (LogLock)
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
            DatabaseLoad.CacheFromDatabase(new NationTemplate());
            DatabaseLoad.CacheFromDatabase(new SkillTemplate());
            DatabaseLoad.CacheFromDatabase(new SpellTemplate());
            DatabaseLoad.CacheFromDatabase(new ItemTemplate());
            DatabaseLoad.CacheFromDatabase(new MonsterTemplate());
            DatabaseLoad.CacheFromDatabase(new MundaneTemplate());
            DatabaseLoad.CacheFromDatabase(new WarpTemplate());
            DatabaseLoad.CacheFromDatabase(new WorldMapTemplate());

            CacheCommunityAssets();

            if (contentOnly) return;

            BindTemplates();
            // ToDo: If decompiling templates, comment out LoadMetaDatabase();
            //MetafileManager.DecompileTemplates();
            LoadExtensions();
        }
    }

    public void EmptyCacheCollectors()
    {
        GlobalMapCache = new ConcurrentDictionary<int, Area>();
        GlobalItemTemplateCache = new ConcurrentDictionary<string, ItemTemplate>();
        GlobalNationTemplateCache = new ConcurrentDictionary<string, NationTemplate>();
        GlobalMonsterTemplateCache = new ConcurrentDictionary<string, MonsterTemplate>();
        GlobalMonsterCache = new ConcurrentDictionary<uint, Monster>();
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

    public void LoadExtensions()
    {
        CacheBuffs();
        Logger($"Buff Cache: {GlobalBuffCache.Count}");
        CacheDebuffs();
        Logger($"Debuff Cache: {GlobalDeBuffCache.Count}");
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