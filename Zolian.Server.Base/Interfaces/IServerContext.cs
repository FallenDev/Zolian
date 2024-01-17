using Darkages.CommandSystem.CLI;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Net;

namespace Darkages.Interfaces;

public interface IServerContext
{
    void InitFromConfig(string storagePath, string serverIp);
    void Start(IServerConstants config, ILogger<ServerSetup> logger);
    void Startup();
    void LoadAndCacheStorage();
    void BindTemplates();
    void LoadExtensions();
    void CacheBuffs();
    void CacheDebuffs();
    void CommandHandler();
    void DatabaseSaveConnection();
    void SetGoodActors();
    FrozenDictionary<int, WorldMapTemplate> GlobalWorldMapTemplateCache { get; set; }
    Dictionary<int, WorldMapTemplate> TempGlobalWorldMapTemplateCache { get; set; }
    FrozenDictionary<int, WarpTemplate> GlobalWarpTemplateCache { get; set; }
    Dictionary<int, WarpTemplate> TempGlobalWarpTemplateCache { get; set; }
    FrozenDictionary<string, SkillTemplate> GlobalSkillTemplateCache { get; set; }
    Dictionary<string, SkillTemplate> TempGlobalSkillTemplateCache { get; set; }
    FrozenDictionary<string, SpellTemplate> GlobalSpellTemplateCache { get; set; }
    Dictionary<string, SpellTemplate> TempGlobalSpellTemplateCache { get; set; }
    FrozenDictionary<string, ItemTemplate> GlobalItemTemplateCache { get; set; }
    Dictionary<string, ItemTemplate> TempGlobalItemTemplateCache { get; set; }
    FrozenDictionary<string, NationTemplate> GlobalNationTemplateCache { get; set; }
    Dictionary<string, NationTemplate> TempGlobalNationTemplateCache { get; set; }
    FrozenDictionary<string, MonsterTemplate> GlobalMonsterTemplateCache { get; set; }
    Dictionary<string, MonsterTemplate> TempGlobalMonsterTemplateCache { get; set; }
    FrozenDictionary<string, MundaneTemplate> GlobalMundaneTemplateCache { get; set; }
    Dictionary<string, MundaneTemplate> TempGlobalMundaneTemplateCache { get; set; }
    FrozenDictionary<uint, string> GlobalKnownGoodActorsCache { get; set; }
    Dictionary<uint, string> TempGlobalKnownGoodActorsCache { get; set; }
    FrozenDictionary<int, Area> GlobalMapCache { get; set; }
    ConcurrentDictionary<int, Area> TempGlobalMapCache { get; set; }
    ConcurrentDictionary<string, Buff> GlobalBuffCache { get; set; }
    ConcurrentDictionary<string, Debuff> GlobalDeBuffCache { get; set; }
    ConcurrentDictionary<ushort, BoardTemplate> GlobalBoardPostCache { get; set; }
    ConcurrentDictionary<int, Party> GlobalGroupCache { get; set; }
    ConcurrentDictionary<uint, Monster> GlobalMonsterCache { get; set; }
    ConcurrentDictionary<uint, Mundane> GlobalMundaneCache { get; set; }
    ConcurrentDictionary<long, Item> GlobalGroundItemCache { get; set; }
    ConcurrentDictionary<long, Item> GlobalSqlItemCache { get; set; }
    ConcurrentDictionary<int, IDictionary<Type, object>> SpriteCollections { get; set; }
    ConcurrentDictionary<uint, Trap> Traps { get; set; }
    ConcurrentDictionary<long, ConcurrentDictionary<string, KillRecord>> GlobalKillRecordCache { get; set; }
    ConcurrentDictionary<IPAddress, IPAddress> GlobalLobbyConnection { get; set; }
    ConcurrentDictionary<IPAddress, IPAddress> GlobalLoginConnection { get; set; }
    ConcurrentDictionary<IPAddress, IPAddress> GlobalWorldConnection { get; set; }
    ConcurrentDictionary<IPAddress, byte> GlobalCreationCount { get; set; }
    bool Running { get; set; }
    SqlConnection ServerSaveConnection { get; set; }
    IServerConstants Config { get; set; }
    WorldServer Game { get; set; }
    LoginServer LoginServer { get; set; }
    LobbyServer LobbyServer { get; set; }
    public CommandParser Parser { get; set; }
    public string StoragePath { get; set; }
    public string MoonPhase { get; set; }
    public byte LightPhase { get; set; }
    public byte LightLevel { get; set; }
    public string KeyCode { get; set; }
    public string Unlock { get; set; }
    public IPAddress IpAddress { get; set; }
    public string InternalAddress { get; set; }
}