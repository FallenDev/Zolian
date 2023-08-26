using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using Darkages.Network.Server;
using System.Net;
using Darkages.CommandSystem.CLI;

namespace Darkages.Interfaces;

public interface IServerContext
{
    void InitFromConfig(string storagePath, string serverIp);
    void Start(IServerConstants config, ILogger<ServerSetup> logger);
    void Startup();
    void LoadAndCacheStorage(bool contentOnly);
    void EmptyCacheCollectors();
    void BindTemplates();
    void CacheCommunityAssets();
    void LoadExtensions();
    void CacheBuffs();
    void CacheDebuffs();
    void CommandHandler();
    ConcurrentDictionary<int, WorldMapTemplate> GlobalWorldMapTemplateCache { get; set; }
    ConcurrentDictionary<int, WarpTemplate> GlobalWarpTemplateCache { get; set; }
    ConcurrentDictionary<string, SkillTemplate> GlobalSkillTemplateCache { get; set; }
    ConcurrentDictionary<string, SpellTemplate> GlobalSpellTemplateCache { get; set; }
    ConcurrentDictionary<string, ItemTemplate> GlobalItemTemplateCache { get; set; }
    ConcurrentDictionary<string, NationTemplate> GlobalNationTemplateCache { get; set; }
    ConcurrentDictionary<string, MonsterTemplate> GlobalMonsterTemplateCache { get; set; }
    ConcurrentDictionary<string, MundaneTemplate> GlobalMundaneTemplateCache { get; set; }
    ConcurrentDictionary<int, Area> GlobalMapCache { get; set; }
    ConcurrentDictionary<string, Buff> GlobalBuffCache { get; set; }
    ConcurrentDictionary<string, Debuff> GlobalDeBuffCache { get; set; }
    ConcurrentDictionary<string, List<Board>> GlobalBoardCache { get; set; }
    ConcurrentDictionary<int, Party> GlobalGroupCache { get; set; }
    ConcurrentDictionary<uint, Monster> GlobalMonsterCache { get; set; }
    ConcurrentDictionary<uint, Mundane> GlobalMundaneCache { get; set; }
    ConcurrentDictionary<int, IDictionary<Type, object>> SpriteCollections { get; set; }
    bool Running { get; set; }
    IServerConstants Config { get; set; }
    WorldServer Game { get; set; }
    LoginServer LoginServer { get; set; }
    LobbyServer LobbyServer { get; set; }
    public CommandParser Parser { get; set; }
    public string StoragePath { get; set; }
    public string MoonPhase { get; set; }
    public byte LightLevel { get; set; }
    public string KeyCode { get; set; }
    public string Unlock { get; set; }
    public IPAddress IpAddress { get; set; }
    public string InternalAddress { get; set; }
}