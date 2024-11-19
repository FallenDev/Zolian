using Chaos.Common.Identity;
using Chaos.Cryptography;
using Chaos.Extensions.Common;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Darkages.CommandSystem;
using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Components;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;
using Microsoft.Extensions.Logging;
using RestSharp;

using ServiceStack;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Chaos.Networking.Abstractions.Definitions;
using Darkages.Managers;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Redirect = Chaos.Networking.Entities.Redirect;
using ServerOptions = Chaos.Networking.Options.ServerOptions;
using IWorldClient = Darkages.Network.Client.Abstractions.IWorldClient;
using MapFlags = Darkages.Enums.MapFlags;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Server;

[UsedImplicitly]
public sealed class WorldServer : ServerBase<IWorldClient>, IWorldServer<IWorldClient>
{
    private readonly IClientFactory<WorldClient> _clientProvider;
    private readonly MServerTable _serverTable;
    private const string InternalIP = "192.168.50.1"; // Cannot use ServerConfig due to value needing to be constant
    private static readonly string[] GameMastersIPs = ServerSetup.Instance.GameMastersIPs;
    private ConcurrentDictionary<Type, WorldServerComponent> _serverComponents;
    public static FrozenDictionary<(Race race, Class path, Class pastClass), string> SkillMap;
    private readonly WorldServerTimer _trapTimer = new(TimeSpan.FromSeconds(1));
    private const int GameSpeed = 50;
    private Task _componentRunTask;
    private Task _updateMundanessTask;
    private Task _updateMonstersTask;
    private Task _updateGroundItemsTask;
    private Task _updateGroundMoneyTask;
    private Task _updateMapsTask;
    private Task _updateTrapsTasks;
    private Task _updateClientsTask;
    private readonly Dictionary<string, Stopwatch> _componentStopwatches = new()
    {
        { "DayLight", new Stopwatch() },
        { "BankInterest", new Stopwatch() },
        { "MessageClear", new Stopwatch() },
        { "Monolith", new Stopwatch() },
        { "Mundane", new Stopwatch() },
        { "Object", new Stopwatch() },
        { "Ping", new Stopwatch() },
        { "PlayerRegen", new Stopwatch() },
        { "PlayerSave", new Stopwatch() },
        { "PlayerStatus", new Stopwatch() },
        { "PlayerSkillSpell", new Stopwatch() },
        { "MoonPhase", new Stopwatch() },
        { "Creation", new Stopwatch() }
    };

    public IEnumerable<Aisling> Aislings => ClientRegistry
        .Where(c => c is { Aisling.LoggedIn: true }).Select(c => c.Aisling);

    public WorldServer(
        IClientRegistry<IWorldClient> clientRegistry,
        IClientFactory<WorldClient> clientProvider,
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        ILogger<WorldServer> logger
    )
        : base(
            redirectManager,
            packetSerializer,
            clientRegistry,
            Microsoft.Extensions.Options.Options.Create(new ServerOptions
            {
                Address = ServerSetup.Instance.IpAddress,
                Port = ServerSetup.Instance.Config.SERVER_PORT
            }),
            logger)
    {
        ServerSetup.Instance.Game = this;
        _serverTable = MServerTable.FromFile("MServerTable.xml");
        _clientProvider = clientProvider;
        IndexHandlers();
        SkillMapper();
        RegisterServerComponents();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Server is now Online\n");
    }

    #region Server Init

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            ServerSetup.Instance.Running = true;
            _componentRunTask = Task.Run(UpdateComponentsRoutine, stoppingToken);
            _updateMundanessTask = Task.Run(UpdateMundanesRoutine, stoppingToken);
            _updateMonstersTask = Task.Run(UpdateMonstersRoutine, stoppingToken);
            _updateGroundItemsTask = Task.Run(UpdateGroundItemsRoutine, stoppingToken);
            _updateGroundMoneyTask = Task.Run(UpdateGroundMoneyRoutine, stoppingToken);
            _updateMapsTask = Task.Run(UpdateMapsRoutine, stoppingToken);
            _updateTrapsTasks = Task.Run(UpdateTrapsRoutine, stoppingToken);
            _updateClientsTask = Task.Run(UpdateClients, stoppingToken);
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger(ex.Message, LogLevel.Error);
            ServerSetup.ConnectionLogger(ex.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(ex);
        }

        return base.ExecuteAsync(stoppingToken);
    }

    private void RegisterServerComponents()
    {
        _serverComponents = new ConcurrentDictionary<Type, WorldServerComponent>
        {
            [typeof(DayLightComponent)] = new DayLightComponent(this),
            [typeof(BankInterestComponent)] = new BankInterestComponent(this),
            [typeof(MessageClearComponent)] = new MessageClearComponent(this),
            [typeof(MonolithComponent)] = new MonolithComponent(this),
            [typeof(MundaneComponent)] = new MundaneComponent(this),
            [typeof(ObjectComponent)] = new ObjectComponent(this),
            [typeof(PingComponent)] = new PingComponent(this),
            [typeof(PlayerRegenerationComponent)] = new PlayerRegenerationComponent(this),
            [typeof(PlayerSaveComponent)] = new PlayerSaveComponent(this),
            [typeof(PlayerStatusBarAndThreatComponent)] = new PlayerStatusBarAndThreatComponent(this),
            [typeof(PlayerSkillSpellCooldownComponent)] = new PlayerSkillSpellCooldownComponent(this),
            [typeof(MoonPhaseComponent)] = new MoonPhaseComponent(this),
            [typeof(ClientCreationLimit)] = new ClientCreationLimit(this)
        };

        Console.WriteLine();
        ServerSetup.ConnectionLogger($"Server Components Loaded: {_serverComponents.Count}");
    }

    private static void SkillMapper()
    {
        // Pre-allocation to a prime number
        var skillMap = new Dictionary<(Race race, Class path, Class pastClass), string>(397)
        {
            {(Race.Human, Class.Berserker, Class.Berserker), "SClass1"},
            {(Race.Human, Class.Berserker, Class.Defender), "SClass2"},
            {(Race.Human, Class.Berserker, Class.Assassin), "SClass3"},
            {(Race.Human, Class.Berserker, Class.Cleric), "SClass4"},
            {(Race.Human, Class.Berserker, Class.Arcanus), "SClass5"},
            {(Race.Human, Class.Berserker, Class.Monk), "SClass6"},
            {(Race.Human, Class.Defender, Class.Berserker), "SClass7"},
            {(Race.Human, Class.Defender, Class.Defender), "SClass8"},
            {(Race.Human, Class.Defender, Class.Assassin), "SClass9"},
            {(Race.Human, Class.Defender, Class.Cleric), "SClass10"},
            {(Race.Human, Class.Defender, Class.Arcanus), "SClass11"},
            {(Race.Human, Class.Defender, Class.Monk), "SClass12"},
            {(Race.Human, Class.Assassin, Class.Berserker), "SClass13"},
            {(Race.Human, Class.Assassin, Class.Defender), "SClass14"},
            {(Race.Human, Class.Assassin, Class.Assassin), "SClass15"},
            {(Race.Human, Class.Assassin, Class.Cleric), "SClass16"},
            {(Race.Human, Class.Assassin, Class.Arcanus), "SClass17"},
            {(Race.Human, Class.Assassin, Class.Monk), "SClass18"},
            {(Race.Human, Class.Cleric, Class.Berserker), "SClass19"},
            {(Race.Human, Class.Cleric, Class.Defender), "SClass20"},
            {(Race.Human, Class.Cleric, Class.Assassin), "SClass21"},
            {(Race.Human, Class.Cleric, Class.Cleric), "SClass22"},
            {(Race.Human, Class.Cleric, Class.Arcanus), "SClass23"},
            {(Race.Human, Class.Cleric, Class.Monk), "SClass24"},
            {(Race.Human, Class.Arcanus, Class.Berserker), "SClass25"},
            {(Race.Human, Class.Arcanus, Class.Defender), "SClass26"},
            {(Race.Human, Class.Arcanus, Class.Assassin), "SClass27"},
            {(Race.Human, Class.Arcanus, Class.Cleric), "SClass28"},
            {(Race.Human, Class.Arcanus, Class.Arcanus), "SClass29"},
            {(Race.Human, Class.Arcanus, Class.Monk), "SClass30"},
            {(Race.Human, Class.Monk, Class.Berserker), "SClass31"},
            {(Race.Human, Class.Monk, Class.Defender), "SClass32"},
            {(Race.Human, Class.Monk, Class.Assassin), "SClass33"},
            {(Race.Human, Class.Monk, Class.Cleric), "SClass34"},
            {(Race.Human, Class.Monk, Class.Arcanus), "SClass35"},
            {(Race.Human, Class.Monk, Class.Monk), "SClass36"},
            {(Race.HalfElf, Class.Berserker, Class.Berserker), "SClass37"},
            {(Race.HalfElf, Class.Berserker, Class.Defender), "SClass38"},
            {(Race.HalfElf, Class.Berserker, Class.Assassin), "SClass39"},
            {(Race.HalfElf, Class.Berserker, Class.Cleric), "SClass40"},
            {(Race.HalfElf, Class.Berserker, Class.Arcanus), "SClass41"},
            {(Race.HalfElf, Class.Berserker, Class.Monk), "SClass42"},
            {(Race.HalfElf, Class.Defender, Class.Berserker), "SClass43"},
            {(Race.HalfElf, Class.Defender, Class.Defender), "SClass44"},
            {(Race.HalfElf, Class.Defender, Class.Assassin), "SClass45"},
            {(Race.HalfElf, Class.Defender, Class.Cleric), "SClass46"},
            {(Race.HalfElf, Class.Defender, Class.Arcanus), "SClass47"},
            {(Race.HalfElf, Class.Defender, Class.Monk), "SClass48"},
            {(Race.HalfElf, Class.Assassin, Class.Berserker), "SClass49"},
            {(Race.HalfElf, Class.Assassin, Class.Defender), "SClass50"},
            {(Race.HalfElf, Class.Assassin, Class.Assassin), "SClass51"},
            {(Race.HalfElf, Class.Assassin, Class.Cleric), "SClass52"},
            {(Race.HalfElf, Class.Assassin, Class.Arcanus), "SClass53"},
            {(Race.HalfElf, Class.Assassin, Class.Monk), "SClass54"},
            {(Race.HalfElf, Class.Cleric, Class.Berserker), "SClass55"},
            {(Race.HalfElf, Class.Cleric, Class.Defender), "SClass56"},
            {(Race.HalfElf, Class.Cleric, Class.Assassin), "SClass57"},
            {(Race.HalfElf, Class.Cleric, Class.Cleric), "SClass58"},
            {(Race.HalfElf, Class.Cleric, Class.Arcanus), "SClass59"},
            {(Race.HalfElf, Class.Cleric, Class.Monk), "SClass60"},
            {(Race.HalfElf, Class.Arcanus, Class.Berserker), "SClass61"},
            {(Race.HalfElf, Class.Arcanus, Class.Defender), "SClass62"},
            {(Race.HalfElf, Class.Arcanus, Class.Assassin), "SClass63"},
            {(Race.HalfElf, Class.Arcanus, Class.Cleric), "SClass64"},
            {(Race.HalfElf, Class.Arcanus, Class.Arcanus), "SClass65"},
            {(Race.HalfElf, Class.Arcanus, Class.Monk), "SClass66"},
            {(Race.HalfElf, Class.Monk, Class.Berserker), "SClass67"},
            {(Race.HalfElf, Class.Monk, Class.Defender), "SClass68"},
            {(Race.HalfElf, Class.Monk, Class.Assassin), "SClass69"},
            {(Race.HalfElf, Class.Monk, Class.Cleric), "SClass70"},
            {(Race.HalfElf, Class.Monk, Class.Arcanus), "SClass71"},
            {(Race.HalfElf, Class.Monk, Class.Monk), "SClass72"},
            {(Race.HighElf, Class.Berserker, Class.Berserker), "SClass73"},
            {(Race.HighElf, Class.Berserker, Class.Defender), "SClass74"},
            {(Race.HighElf, Class.Berserker, Class.Assassin), "SClass75"},
            {(Race.HighElf, Class.Berserker, Class.Cleric), "SClass76"},
            {(Race.HighElf, Class.Berserker, Class.Arcanus), "SClass77"},
            {(Race.HighElf, Class.Berserker, Class.Monk), "SClass78"},
            {(Race.HighElf, Class.Defender, Class.Berserker), "SClass79"},
            {(Race.HighElf, Class.Defender, Class.Defender), "SClass80"},
            {(Race.HighElf, Class.Defender, Class.Assassin), "SClass81"},
            {(Race.HighElf, Class.Defender, Class.Cleric), "SClass82"},
            {(Race.HighElf, Class.Defender, Class.Arcanus), "SClass83"},
            {(Race.HighElf, Class.Defender, Class.Monk), "SClass84"},
            {(Race.HighElf, Class.Assassin, Class.Berserker), "SClass85"},
            {(Race.HighElf, Class.Assassin, Class.Defender), "SClass86"},
            {(Race.HighElf, Class.Assassin, Class.Assassin), "SClass87"},
            {(Race.HighElf, Class.Assassin, Class.Cleric), "SClass88"},
            {(Race.HighElf, Class.Assassin, Class.Arcanus), "SClass89"},
            {(Race.HighElf, Class.Assassin, Class.Monk), "SClass90"},
            {(Race.HighElf, Class.Cleric, Class.Berserker), "SClass91"},
            {(Race.HighElf, Class.Cleric, Class.Defender), "SClass92"},
            {(Race.HighElf, Class.Cleric, Class.Assassin), "SClass93"},
            {(Race.HighElf, Class.Cleric, Class.Cleric), "SClass94"},
            {(Race.HighElf, Class.Cleric, Class.Arcanus), "SClass95"},
            {(Race.HighElf, Class.Cleric, Class.Monk), "SClass96"},
            {(Race.HighElf, Class.Arcanus, Class.Berserker), "SClass97"},
            {(Race.HighElf, Class.Arcanus, Class.Defender), "SClass98"},
            {(Race.HighElf, Class.Arcanus, Class.Assassin), "SClass99"},
            {(Race.HighElf, Class.Arcanus, Class.Cleric), "SClass100"},
            {(Race.HighElf, Class.Arcanus, Class.Arcanus), "SClass101"},
            {(Race.HighElf, Class.Arcanus, Class.Monk), "SClass102"},
            {(Race.HighElf, Class.Monk, Class.Berserker), "SClass103"},
            {(Race.HighElf, Class.Monk, Class.Defender), "SClass104"},
            {(Race.HighElf, Class.Monk, Class.Assassin), "SClass105"},
            {(Race.HighElf, Class.Monk, Class.Cleric), "SClass106"},
            {(Race.HighElf, Class.Monk, Class.Arcanus), "SClass107"},
            {(Race.HighElf, Class.Monk, Class.Monk), "SClass108"},
            {(Race.DarkElf, Class.Berserker, Class.Berserker), "SClass109"},
            {(Race.DarkElf, Class.Berserker, Class.Defender), "SClass110"},
            {(Race.DarkElf, Class.Berserker, Class.Assassin), "SClass111"},
            {(Race.DarkElf, Class.Berserker, Class.Cleric), "SClass112"},
            {(Race.DarkElf, Class.Berserker, Class.Arcanus), "SClass113"},
            {(Race.DarkElf, Class.Berserker, Class.Monk), "SClass114"},
            {(Race.DarkElf, Class.Defender, Class.Berserker), "SClass115"},
            {(Race.DarkElf, Class.Defender, Class.Defender), "SClass116"},
            {(Race.DarkElf, Class.Defender, Class.Assassin), "SClass117"},
            {(Race.DarkElf, Class.Defender, Class.Cleric), "SClass118"},
            {(Race.DarkElf, Class.Defender, Class.Arcanus), "SClass119"},
            {(Race.DarkElf, Class.Defender, Class.Monk), "SClass120"},
            {(Race.DarkElf, Class.Assassin, Class.Berserker), "SClass121"},
            {(Race.DarkElf, Class.Assassin, Class.Defender), "SClass122"},
            {(Race.DarkElf, Class.Assassin, Class.Assassin), "SClass123"},
            {(Race.DarkElf, Class.Assassin, Class.Cleric), "SClass124"},
            {(Race.DarkElf, Class.Assassin, Class.Arcanus), "SClass125"},
            {(Race.DarkElf, Class.Assassin, Class.Monk), "SClass126"},
            {(Race.DarkElf, Class.Cleric, Class.Berserker), "SClass127"},
            {(Race.DarkElf, Class.Cleric, Class.Defender), "SClass128"},
            {(Race.DarkElf, Class.Cleric, Class.Assassin), "SClass129"},
            {(Race.DarkElf, Class.Cleric, Class.Cleric), "SClass130"},
            {(Race.DarkElf, Class.Cleric, Class.Arcanus), "SClass131"},
            {(Race.DarkElf, Class.Cleric, Class.Monk), "SClass132"},
            {(Race.DarkElf, Class.Arcanus, Class.Berserker), "SClass133"},
            {(Race.DarkElf, Class.Arcanus, Class.Defender), "SClass134"},
            {(Race.DarkElf, Class.Arcanus, Class.Assassin), "SClass135"},
            {(Race.DarkElf, Class.Arcanus, Class.Cleric), "SClass136"},
            {(Race.DarkElf, Class.Arcanus, Class.Arcanus), "SClass137"},
            {(Race.DarkElf, Class.Arcanus, Class.Monk), "SClass138"},
            {(Race.DarkElf, Class.Monk, Class.Berserker), "SClass139"},
            {(Race.DarkElf, Class.Monk, Class.Defender), "SClass140"},
            {(Race.DarkElf, Class.Monk, Class.Assassin), "SClass141"},
            {(Race.DarkElf, Class.Monk, Class.Cleric), "SClass142"},
            {(Race.DarkElf, Class.Monk, Class.Arcanus), "SClass143"},
            {(Race.DarkElf, Class.Monk, Class.Monk), "SClass144"},
            {(Race.WoodElf, Class.Berserker, Class.Berserker), "SClass145"},
            {(Race.WoodElf, Class.Berserker, Class.Defender), "SClass146"},
            {(Race.WoodElf, Class.Berserker, Class.Assassin), "SClass147"},
            {(Race.WoodElf, Class.Berserker, Class.Cleric), "SClass148"},
            {(Race.WoodElf, Class.Berserker, Class.Arcanus), "SClass149"},
            {(Race.WoodElf, Class.Berserker, Class.Monk), "SClass150"},
            {(Race.WoodElf, Class.Defender, Class.Berserker), "SClass151"},
            {(Race.WoodElf, Class.Defender, Class.Defender), "SClass152"},
            {(Race.WoodElf, Class.Defender, Class.Assassin), "SClass153"},
            {(Race.WoodElf, Class.Defender, Class.Cleric), "SClass154"},
            {(Race.WoodElf, Class.Defender, Class.Arcanus), "SClass155"},
            {(Race.WoodElf, Class.Defender, Class.Monk), "SClass156"},
            {(Race.WoodElf, Class.Assassin, Class.Berserker), "SClass157"},
            {(Race.WoodElf, Class.Assassin, Class.Defender), "SClass158"},
            {(Race.WoodElf, Class.Assassin, Class.Assassin), "SClass159"},
            {(Race.WoodElf, Class.Assassin, Class.Cleric), "SClass160"},
            {(Race.WoodElf, Class.Assassin, Class.Arcanus), "SClass161"},
            {(Race.WoodElf, Class.Assassin, Class.Monk), "SClass162"},
            {(Race.WoodElf, Class.Cleric, Class.Berserker), "SClass163"},
            {(Race.WoodElf, Class.Cleric, Class.Defender), "SClass164"},
            {(Race.WoodElf, Class.Cleric, Class.Assassin), "SClass165"},
            {(Race.WoodElf, Class.Cleric, Class.Cleric), "SClass166"},
            {(Race.WoodElf, Class.Cleric, Class.Arcanus), "SClass167"},
            {(Race.WoodElf, Class.Cleric, Class.Monk), "SClass168"},
            {(Race.WoodElf, Class.Arcanus, Class.Berserker), "SClass169"},
            {(Race.WoodElf, Class.Arcanus, Class.Defender), "SClass170"},
            {(Race.WoodElf, Class.Arcanus, Class.Assassin), "SClass171"},
            {(Race.WoodElf, Class.Arcanus, Class.Cleric), "SClass172"},
            {(Race.WoodElf, Class.Arcanus, Class.Arcanus), "SClass173"},
            {(Race.WoodElf, Class.Arcanus, Class.Monk), "SClass174"},
            {(Race.WoodElf, Class.Monk, Class.Berserker), "SClass175"},
            {(Race.WoodElf, Class.Monk, Class.Defender), "SClass176"},
            {(Race.WoodElf, Class.Monk, Class.Assassin), "SClass177"},
            {(Race.WoodElf, Class.Monk, Class.Cleric), "SClass178"},
            {(Race.WoodElf, Class.Monk, Class.Arcanus), "SClass179"},
            {(Race.WoodElf, Class.Monk, Class.Monk), "SClass180"},
            {(Race.Orc, Class.Berserker, Class.Berserker), "SClass181"},
            {(Race.Orc, Class.Berserker, Class.Defender), "SClass182"},
            {(Race.Orc, Class.Berserker, Class.Assassin), "SClass183"},
            {(Race.Orc, Class.Berserker, Class.Cleric), "SClass184"},
            {(Race.Orc, Class.Berserker, Class.Arcanus), "SClass185"},
            {(Race.Orc, Class.Berserker, Class.Monk), "SClass186"},
            {(Race.Orc, Class.Defender, Class.Berserker), "SClass187"},
            {(Race.Orc, Class.Defender, Class.Defender), "SClass188"},
            {(Race.Orc, Class.Defender, Class.Assassin), "SClass189"},
            {(Race.Orc, Class.Defender, Class.Cleric), "SClass190"},
            {(Race.Orc, Class.Defender, Class.Arcanus), "SClass191"},
            {(Race.Orc, Class.Defender, Class.Monk), "SClass192"},
            {(Race.Orc, Class.Assassin, Class.Berserker), "SClass193"},
            {(Race.Orc, Class.Assassin, Class.Defender), "SClass194"},
            {(Race.Orc, Class.Assassin, Class.Assassin), "SClass195"},
            {(Race.Orc, Class.Assassin, Class.Cleric), "SClass196"},
            {(Race.Orc, Class.Assassin, Class.Arcanus), "SClass197"},
            {(Race.Orc, Class.Assassin, Class.Monk), "SClass198"},
            {(Race.Orc, Class.Cleric, Class.Berserker), "SClass199"},
            {(Race.Orc, Class.Cleric, Class.Defender), "SClass200"},
            {(Race.Orc, Class.Cleric, Class.Assassin), "SClass201"},
            {(Race.Orc, Class.Cleric, Class.Cleric), "SClass202"},
            {(Race.Orc, Class.Cleric, Class.Arcanus), "SClass203"},
            {(Race.Orc, Class.Cleric, Class.Monk), "SClass204"},
            {(Race.Orc, Class.Arcanus, Class.Berserker), "SClass205"},
            {(Race.Orc, Class.Arcanus, Class.Defender), "SClass206"},
            {(Race.Orc, Class.Arcanus, Class.Assassin), "SClass207"},
            {(Race.Orc, Class.Arcanus, Class.Cleric), "SClass208"},
            {(Race.Orc, Class.Arcanus, Class.Arcanus), "SClass209"},
            {(Race.Orc, Class.Arcanus, Class.Monk), "SClass210"},
            {(Race.Orc, Class.Monk, Class.Berserker), "SClass211"},
            {(Race.Orc, Class.Monk, Class.Defender), "SClass212"},
            {(Race.Orc, Class.Monk, Class.Assassin), "SClass213"},
            {(Race.Orc, Class.Monk, Class.Cleric), "SClass214"},
            {(Race.Orc, Class.Monk, Class.Arcanus), "SClass215"},
            {(Race.Orc, Class.Monk, Class.Monk), "SClass216"},
            {(Race.Dwarf, Class.Berserker, Class.Berserker), "SClass217"},
            {(Race.Dwarf, Class.Berserker, Class.Defender), "SClass218"},
            {(Race.Dwarf, Class.Berserker, Class.Assassin), "SClass219"},
            {(Race.Dwarf, Class.Berserker, Class.Cleric), "SClass220"},
            {(Race.Dwarf, Class.Berserker, Class.Arcanus), "SClass221"},
            {(Race.Dwarf, Class.Berserker, Class.Monk), "SClass222"},
            {(Race.Dwarf, Class.Defender, Class.Berserker), "SClass223"},
            {(Race.Dwarf, Class.Defender, Class.Defender), "SClass224"},
            {(Race.Dwarf, Class.Defender, Class.Assassin), "SClass225"},
            {(Race.Dwarf, Class.Defender, Class.Cleric), "SClass226"},
            {(Race.Dwarf, Class.Defender, Class.Arcanus), "SClass227"},
            {(Race.Dwarf, Class.Defender, Class.Monk), "SClass228"},
            {(Race.Dwarf, Class.Assassin, Class.Berserker), "SClass229"},
            {(Race.Dwarf, Class.Assassin, Class.Defender), "SClass230"},
            {(Race.Dwarf, Class.Assassin, Class.Assassin), "SClass231"},
            {(Race.Dwarf, Class.Assassin, Class.Cleric), "SClass232"},
            {(Race.Dwarf, Class.Assassin, Class.Arcanus), "SClass233"},
            {(Race.Dwarf, Class.Assassin, Class.Monk), "SClass234"},
            {(Race.Dwarf, Class.Cleric, Class.Berserker), "SClass235"},
            {(Race.Dwarf, Class.Cleric, Class.Defender), "SClass236"},
            {(Race.Dwarf, Class.Cleric, Class.Assassin), "SClass237"},
            {(Race.Dwarf, Class.Cleric, Class.Cleric), "SClass238"},
            {(Race.Dwarf, Class.Cleric, Class.Arcanus), "SClass239"},
            {(Race.Dwarf, Class.Cleric, Class.Monk), "SClass240"},
            {(Race.Dwarf, Class.Arcanus, Class.Berserker), "SClass241"},
            {(Race.Dwarf, Class.Arcanus, Class.Defender), "SClass242"},
            {(Race.Dwarf, Class.Arcanus, Class.Assassin), "SClass243"},
            {(Race.Dwarf, Class.Arcanus, Class.Cleric), "SClass244"},
            {(Race.Dwarf, Class.Arcanus, Class.Arcanus), "SClass245"},
            {(Race.Dwarf, Class.Arcanus, Class.Monk), "SClass246"},
            {(Race.Dwarf, Class.Monk, Class.Berserker), "SClass247"},
            {(Race.Dwarf, Class.Monk, Class.Defender), "SClass248"},
            {(Race.Dwarf, Class.Monk, Class.Assassin), "SClass249"},
            {(Race.Dwarf, Class.Monk, Class.Cleric), "SClass250"},
            {(Race.Dwarf, Class.Monk, Class.Arcanus), "SClass251"},
            {(Race.Dwarf, Class.Monk, Class.Monk), "SClass252"},
            {(Race.Halfling, Class.Berserker, Class.Berserker), "SClass253"},
            {(Race.Halfling, Class.Berserker, Class.Defender), "SClass254"},
            {(Race.Halfling, Class.Berserker, Class.Assassin), "SClass255"},
            {(Race.Halfling, Class.Berserker, Class.Cleric), "SClass256"},
            {(Race.Halfling, Class.Berserker, Class.Arcanus), "SClass257"},
            {(Race.Halfling, Class.Berserker, Class.Monk), "SClass258"},
            {(Race.Halfling, Class.Defender, Class.Berserker), "SClass259"},
            {(Race.Halfling, Class.Defender, Class.Defender), "SClass260"},
            {(Race.Halfling, Class.Defender, Class.Assassin), "SClass261"},
            {(Race.Halfling, Class.Defender, Class.Cleric), "SClass262"},
            {(Race.Halfling, Class.Defender, Class.Arcanus), "SClass263"},
            {(Race.Halfling, Class.Defender, Class.Monk), "SClass264"},
            {(Race.Halfling, Class.Assassin, Class.Berserker), "SClass265"},
            {(Race.Halfling, Class.Assassin, Class.Defender), "SClass266"},
            {(Race.Halfling, Class.Assassin, Class.Assassin), "SClass267"},
            {(Race.Halfling, Class.Assassin, Class.Cleric), "SClass268"},
            {(Race.Halfling, Class.Assassin, Class.Arcanus), "SClass269"},
            {(Race.Halfling, Class.Assassin, Class.Monk), "SClass270"},
            {(Race.Halfling, Class.Cleric, Class.Berserker), "SClass271"},
            {(Race.Halfling, Class.Cleric, Class.Defender), "SClass272"},
            {(Race.Halfling, Class.Cleric, Class.Assassin), "SClass273"},
            {(Race.Halfling, Class.Cleric, Class.Cleric), "SClass274"},
            {(Race.Halfling, Class.Cleric, Class.Arcanus), "SClass275"},
            {(Race.Halfling, Class.Cleric, Class.Monk), "SClass276"},
            {(Race.Halfling, Class.Arcanus, Class.Berserker), "SClass277"},
            {(Race.Halfling, Class.Arcanus, Class.Defender), "SClass278"},
            {(Race.Halfling, Class.Arcanus, Class.Assassin), "SClass279"},
            {(Race.Halfling, Class.Arcanus, Class.Cleric), "SClass280"},
            {(Race.Halfling, Class.Arcanus, Class.Arcanus), "SClass281"},
            {(Race.Halfling, Class.Arcanus, Class.Monk), "SClass282"},
            {(Race.Halfling, Class.Monk, Class.Berserker), "SClass283"},
            {(Race.Halfling, Class.Monk, Class.Defender), "SClass284"},
            {(Race.Halfling, Class.Monk, Class.Assassin), "SClass285"},
            {(Race.Halfling, Class.Monk, Class.Cleric), "SClass286"},
            {(Race.Halfling, Class.Monk, Class.Arcanus), "SClass287"},
            {(Race.Halfling, Class.Monk, Class.Monk), "SClass288"},
            {(Race.Dragonkin, Class.Berserker, Class.Berserker), "SClass289"},
            {(Race.Dragonkin, Class.Berserker, Class.Defender), "SClass290"},
            {(Race.Dragonkin, Class.Berserker, Class.Assassin), "SClass291"},
            {(Race.Dragonkin, Class.Berserker, Class.Cleric), "SClass292"},
            {(Race.Dragonkin, Class.Berserker, Class.Arcanus), "SClass293"},
            {(Race.Dragonkin, Class.Berserker, Class.Monk), "SClass294"},
            {(Race.Dragonkin, Class.Defender, Class.Berserker), "SClass295"},
            {(Race.Dragonkin, Class.Defender, Class.Defender), "SClass296"},
            {(Race.Dragonkin, Class.Defender, Class.Assassin), "SClass297"},
            {(Race.Dragonkin, Class.Defender, Class.Cleric), "SClass298"},
            {(Race.Dragonkin, Class.Defender, Class.Arcanus), "SClass299"},
            {(Race.Dragonkin, Class.Defender, Class.Monk), "SClass300"},
            {(Race.Dragonkin, Class.Assassin, Class.Berserker), "SClass301"},
            {(Race.Dragonkin, Class.Assassin, Class.Defender), "SClass302"},
            {(Race.Dragonkin, Class.Assassin, Class.Assassin), "SClass303"},
            {(Race.Dragonkin, Class.Assassin, Class.Cleric), "SClass304"},
            {(Race.Dragonkin, Class.Assassin, Class.Arcanus), "SClass305"},
            {(Race.Dragonkin, Class.Assassin, Class.Monk), "SClass306"},
            {(Race.Dragonkin, Class.Cleric, Class.Berserker), "SClass307"},
            {(Race.Dragonkin, Class.Cleric, Class.Defender), "SClass308"},
            {(Race.Dragonkin, Class.Cleric, Class.Assassin), "SClass309"},
            {(Race.Dragonkin, Class.Cleric, Class.Cleric), "SClass310"},
            {(Race.Dragonkin, Class.Cleric, Class.Arcanus), "SClass311"},
            {(Race.Dragonkin, Class.Cleric, Class.Monk), "SClass312"},
            {(Race.Dragonkin, Class.Arcanus, Class.Berserker), "SClass313"},
            {(Race.Dragonkin, Class.Arcanus, Class.Defender), "SClass314"},
            {(Race.Dragonkin, Class.Arcanus, Class.Assassin), "SClass315"},
            {(Race.Dragonkin, Class.Arcanus, Class.Cleric), "SClass316"},
            {(Race.Dragonkin, Class.Arcanus, Class.Arcanus), "SClass317"},
            {(Race.Dragonkin, Class.Arcanus, Class.Monk), "SClass318"},
            {(Race.Dragonkin, Class.Monk, Class.Berserker), "SClass319"},
            {(Race.Dragonkin, Class.Monk, Class.Defender), "SClass320"},
            {(Race.Dragonkin, Class.Monk, Class.Assassin), "SClass321"},
            {(Race.Dragonkin, Class.Monk, Class.Cleric), "SClass322"},
            {(Race.Dragonkin, Class.Monk, Class.Arcanus), "SClass323"},
            {(Race.Dragonkin, Class.Monk, Class.Monk), "SClass324"},
            {(Race.HalfBeast, Class.Berserker, Class.Berserker), "SClass325"},
            {(Race.HalfBeast, Class.Berserker, Class.Defender), "SClass326"},
            {(Race.HalfBeast, Class.Berserker, Class.Assassin), "SClass327"},
            {(Race.HalfBeast, Class.Berserker, Class.Cleric), "SClass328"},
            {(Race.HalfBeast, Class.Berserker, Class.Arcanus), "SClass329"},
            {(Race.HalfBeast, Class.Berserker, Class.Monk), "SClass330"},
            {(Race.HalfBeast, Class.Defender, Class.Berserker), "SClass331"},
            {(Race.HalfBeast, Class.Defender, Class.Defender), "SClass332"},
            {(Race.HalfBeast, Class.Defender, Class.Assassin), "SClass333"},
            {(Race.HalfBeast, Class.Defender, Class.Cleric), "SClass334"},
            {(Race.HalfBeast, Class.Defender, Class.Arcanus), "SClass335"},
            {(Race.HalfBeast, Class.Defender, Class.Monk), "SClass336"},
            {(Race.HalfBeast, Class.Assassin, Class.Berserker), "SClass337"},
            {(Race.HalfBeast, Class.Assassin, Class.Defender), "SClass338"},
            {(Race.HalfBeast, Class.Assassin, Class.Assassin), "SClass339"},
            {(Race.HalfBeast, Class.Assassin, Class.Cleric), "SClass340"},
            {(Race.HalfBeast, Class.Assassin, Class.Arcanus), "SClass341"},
            {(Race.HalfBeast, Class.Assassin, Class.Monk), "SClass342"},
            {(Race.HalfBeast, Class.Cleric, Class.Berserker), "SClass343"},
            {(Race.HalfBeast, Class.Cleric, Class.Defender), "SClass344"},
            {(Race.HalfBeast, Class.Cleric, Class.Assassin), "SClass345"},
            {(Race.HalfBeast, Class.Cleric, Class.Cleric), "SClass346"},
            {(Race.HalfBeast, Class.Cleric, Class.Arcanus), "SClass347"},
            {(Race.HalfBeast, Class.Cleric, Class.Monk), "SClass348"},
            {(Race.HalfBeast, Class.Arcanus, Class.Berserker), "SClass349"},
            {(Race.HalfBeast, Class.Arcanus, Class.Defender), "SClass350"},
            {(Race.HalfBeast, Class.Arcanus, Class.Assassin), "SClass351"},
            {(Race.HalfBeast, Class.Arcanus, Class.Cleric), "SClass352"},
            {(Race.HalfBeast, Class.Arcanus, Class.Arcanus), "SClass353"},
            {(Race.HalfBeast, Class.Arcanus, Class.Monk), "SClass354"},
            {(Race.HalfBeast, Class.Monk, Class.Berserker), "SClass355"},
            {(Race.HalfBeast, Class.Monk, Class.Defender), "SClass356"},
            {(Race.HalfBeast, Class.Monk, Class.Assassin), "SClass357"},
            {(Race.HalfBeast, Class.Monk, Class.Cleric), "SClass358"},
            {(Race.HalfBeast, Class.Monk, Class.Arcanus), "SClass359"},
            {(Race.HalfBeast, Class.Monk, Class.Monk), "SClass360"},
            {(Race.Merfolk, Class.Berserker, Class.Berserker), "SClass361"},
            {(Race.Merfolk, Class.Berserker, Class.Defender), "SClass362"},
            {(Race.Merfolk, Class.Berserker, Class.Assassin), "SClass363"},
            {(Race.Merfolk, Class.Berserker, Class.Cleric), "SClass364"},
            {(Race.Merfolk, Class.Berserker, Class.Arcanus), "SClass365"},
            {(Race.Merfolk, Class.Berserker, Class.Monk), "SClass366"},
            {(Race.Merfolk, Class.Defender, Class.Berserker), "SClass367"},
            {(Race.Merfolk, Class.Defender, Class.Defender), "SClass368"},
            {(Race.Merfolk, Class.Defender, Class.Assassin), "SClass369"},
            {(Race.Merfolk, Class.Defender, Class.Cleric), "SClass370"},
            {(Race.Merfolk, Class.Defender, Class.Arcanus), "SClass371"},
            {(Race.Merfolk, Class.Defender, Class.Monk), "SClass372"},
            {(Race.Merfolk, Class.Assassin, Class.Berserker), "SClass373"},
            {(Race.Merfolk, Class.Assassin, Class.Defender), "SClass374"},
            {(Race.Merfolk, Class.Assassin, Class.Assassin), "SClass375"},
            {(Race.Merfolk, Class.Assassin, Class.Cleric), "SClass376"},
            {(Race.Merfolk, Class.Assassin, Class.Arcanus), "SClass377"},
            {(Race.Merfolk, Class.Assassin, Class.Monk), "SClass378"},
            {(Race.Merfolk, Class.Cleric, Class.Berserker), "SClass379"},
            {(Race.Merfolk, Class.Cleric, Class.Defender), "SClass380"},
            {(Race.Merfolk, Class.Cleric, Class.Assassin), "SClass381"},
            {(Race.Merfolk, Class.Cleric, Class.Cleric), "SClass382"},
            {(Race.Merfolk, Class.Cleric, Class.Arcanus), "SClass383"},
            {(Race.Merfolk, Class.Cleric, Class.Monk), "SClass384"},
            {(Race.Merfolk, Class.Arcanus, Class.Berserker), "SClass385"},
            {(Race.Merfolk, Class.Arcanus, Class.Defender), "SClass386"},
            {(Race.Merfolk, Class.Arcanus, Class.Assassin), "SClass387"},
            {(Race.Merfolk, Class.Arcanus, Class.Cleric), "SClass388"},
            {(Race.Merfolk, Class.Arcanus, Class.Arcanus), "SClass389"},
            {(Race.Merfolk, Class.Arcanus, Class.Monk), "SClass390"},
            {(Race.Merfolk, Class.Monk, Class.Berserker), "SClass391"},
            {(Race.Merfolk, Class.Monk, Class.Defender), "SClass392"},
            {(Race.Merfolk, Class.Monk, Class.Assassin), "SClass393"},
            {(Race.Merfolk, Class.Monk, Class.Cleric), "SClass394"},
            {(Race.Merfolk, Class.Monk, Class.Arcanus), "SClass395"},
            {(Race.Merfolk, Class.Monk, Class.Monk), "SClass396"}
        };

        // Set frozen dict then cleanup unused dict
        SkillMap = skillMap.ToFrozenDictionary();
    }

    #endregion

    #region Server Loop

    private async Task UpdateComponentsRoutine()
    {
        foreach (var stopwatch in _componentStopwatches.Values)
            stopwatch.Start();

        while (ServerSetup.Instance.Running)
        {
            var elapsed = new Dictionary<string, TimeSpan>();
            foreach (var kvp in _componentStopwatches)
                elapsed[kvp.Key] = kvp.Value.Elapsed;

            Parallel.ForEach(_serverComponents.Values, component =>
            {
                switch (component)
                {
                    case ObjectComponent objectComponent:
                        if (elapsed["Object"].TotalMilliseconds < GameSpeed) break;
                        objectComponent.Update(elapsed["Object"]);
                        _componentStopwatches["Object"].Restart();
                        break;
                    case PlayerSkillSpellCooldownComponent skillSpellCooldownComponent:
                        if (elapsed["PlayerSkillSpell"].TotalMilliseconds < 100) break;
                        skillSpellCooldownComponent.Update(elapsed["PlayerSkillSpell"]);
                        _componentStopwatches["PlayerSkillSpell"].Restart();
                        break;
                    case PlayerStatusBarAndThreatComponent statusBarAndThreatComponent:
                        if (elapsed["PlayerStatus"].TotalMilliseconds < 100) break;
                        statusBarAndThreatComponent.Update(elapsed["PlayerStatus"]);
                        _componentStopwatches["PlayerStatus"].Restart();
                        break;
                    case PlayerRegenerationComponent playerRegenerationComponent:
                        if (elapsed["PlayerRegen"].TotalSeconds < 1) break;
                        playerRegenerationComponent.Update(elapsed["PlayerRegen"]);
                        _componentStopwatches["PlayerRegen"].Restart();
                        break;
                    case MonolithComponent monolithComponent:
                        if (elapsed["Monolith"].TotalSeconds < 3) break;
                        monolithComponent.Update(elapsed["Monolith"]);
                        _componentStopwatches["Monolith"].Restart();
                        break;
                    case PingComponent pingComponent:
                        if (elapsed["Ping"].TotalSeconds < 7) break;
                        pingComponent.Update(elapsed["Ping"]);
                        _componentStopwatches["Ping"].Restart();
                        break;
                    case MundaneComponent mundaneComponent:
                        if (elapsed["Mundane"].TotalSeconds < 10) break;
                        mundaneComponent.Update(elapsed["Mundane"]);
                        _componentStopwatches["Mundane"].Restart();
                        break;
                    case DayLightComponent dayLightComponent:
                        if (elapsed["DayLight"].TotalSeconds < 15) break;
                        dayLightComponent.Update(elapsed["DayLight"]);
                        _componentStopwatches["DayLight"].Restart();
                        break;
                    case PlayerSaveComponent playerSaveComponent:
                        if (elapsed["PlayerSave"].TotalSeconds < 45) break;
                        playerSaveComponent.Update(elapsed["PlayerSave"]);
                        _componentStopwatches["PlayerSave"].Restart();
                        break;
                    case MessageClearComponent messageClearComponent:
                        if (elapsed["MessageClear"].TotalSeconds < 60) break;
                        messageClearComponent.Update(elapsed["MessageClear"]);
                        UpdateBoards();
                        _componentStopwatches["MessageClear"].Restart();
                        break;
                    case BankInterestComponent bankInterestComponent:
                        if (elapsed["BankInterest"].TotalMinutes < 30) break;
                        bankInterestComponent.Update(elapsed["BankInterest"]);
                        _componentStopwatches["BankInterest"].Restart();
                        break;
                    case MoonPhaseComponent moonPhaseComponent:
                        if (elapsed["MoonPhase"].TotalMinutes < 1) break;
                        moonPhaseComponent.Update(elapsed["MoonPhase"]);
                        _componentStopwatches["MoonPhase"].Restart();
                        break;
                    case ClientCreationLimit clientCreationLimitComponent:
                        if (elapsed["Creation"].TotalMinutes < 60) break;
                        clientCreationLimitComponent.Update(elapsed["Creation"]);
                        _componentStopwatches["Creation"].Restart();
                        break;
                }
            });

            await Task.Delay(GameSpeed);
        }
    }

    private static async Task UpdateGroundItemsRoutine()
    {
        var groundWatch = new Stopwatch();
        groundWatch.Start();

        while (ServerSetup.Instance.Running)
        {
            var groundElapsed = groundWatch.Elapsed;
            if (groundElapsed.TotalMinutes < 1) continue;
            UpdateGroundItems();
            groundWatch.Restart();

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private static async Task UpdateGroundMoneyRoutine()
    {
        var groundWatch = new Stopwatch();
        groundWatch.Start();

        while (ServerSetup.Instance.Running)
        {
            var groundElapsed = groundWatch.Elapsed;
            if (groundElapsed.TotalMinutes < 1) continue;
            UpdateGroundMoney();
            groundWatch.Restart();

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private static async Task UpdateMundanesRoutine()
    {
        var mundanesWatch = new Stopwatch();
        mundanesWatch.Start();

        while (ServerSetup.Instance.Running)
        {
            var mundanesElapsed = mundanesWatch.Elapsed;
            if (mundanesElapsed.TotalMilliseconds < 1500) continue;
            UpdateMundanes(mundanesElapsed);
            mundanesWatch.Restart();

            await Task.Delay(TimeSpan.FromMilliseconds(1500));
        }
    }

    private static async Task UpdateMonstersRoutine()
    {
        var monstersWatch = new Stopwatch();
        monstersWatch.Start();

        while (ServerSetup.Instance.Running)
        {
            var monstersElapsed = monstersWatch.Elapsed;
            if (monstersElapsed.TotalMilliseconds < 300) continue;
            UpdateMonsters(monstersElapsed);
            monstersWatch.Restart();

            await Task.Delay(TimeSpan.FromMilliseconds(300));
        }
    }

    private static async Task UpdateMapsRoutine()
    {
        var gameWatch = new Stopwatch();
        gameWatch.Start();

        while (ServerSetup.Instance.Running)
        {
            var gameTimeElapsed = gameWatch.Elapsed;
            if (gameTimeElapsed.TotalMilliseconds < GameSpeed) continue;
            UpdateMaps(gameTimeElapsed);
            gameWatch.Restart();

            await Task.Delay(TimeSpan.FromMilliseconds(GameSpeed));
        }
    }

    private async Task UpdateTrapsRoutine()
    {
        var gameWatch = new Stopwatch();
        gameWatch.Start();

        while (ServerSetup.Instance.Running)
        {
            var gameTimeElapsed = gameWatch.Elapsed;
            if (gameTimeElapsed.TotalMilliseconds < GameSpeed) continue;
            CheckTraps(gameTimeElapsed);
            gameWatch.Restart();

            await Task.Delay(TimeSpan.FromMilliseconds(GameSpeed));
        }
    }

    private async Task UpdateClients()
    {
        const int maxConcurrency = 10;
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var playerUpdateTasks = new List<Task>();

        while (ServerSetup.Instance.Running)
        {
            var tickStart = DateTime.UtcNow;
            var players = Aislings.Where(player => player?.Client != null).ToList();

            foreach (var player in players)
            {
                await semaphore.WaitAsync();
                var task = ProcessClientTask(player)
                    .ContinueWith(t =>
                    {
                        semaphore.Release();
                    }, TaskScheduler.Default);
                playerUpdateTasks.Add(task);
            }

            await Task.WhenAll(playerUpdateTasks);
            playerUpdateTasks.Clear();

            var tickEnd = DateTime.UtcNow;
            var duration = tickEnd - tickStart;
            var remainingDelay = GameSpeed - duration.TotalMilliseconds;

            if (remainingDelay > 0)
                await Task.Delay((int)remainingDelay);
        }
    }

    private async Task ProcessClientTask(Aisling player)
    {
        if (player?.Client == null) return;

        try
        {
            if (!player.LoggedIn)
            {
                ClientRegistry.TryRemove(player.Client.Id, out _);
                return;
            }

            await player.Client.Update();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            player.Client.Disconnect();
            ClientRegistry.TryRemove(player.Client.Id, out _);
        }
    }

    private static void UpdateGroundItems()
    {
        try
        {
            // Routine to check items that have been on the ground longer than 30 minutes
            foreach (var item in from area in ServerSetup.Instance.GlobalMapCache.Values
                                 select ObjectManager.GetObjects<Item>(area, i => i.ItemPane == Item.ItemPanes.Ground)
                     into items
                                 from item in items
                                 let abandonedDiff = DateTime.UtcNow.Subtract(item.AbandonedDate)
                                 where !(abandonedDiff.TotalMinutes <= 30)
                                 select item)
                item.Remove();

            foreach (var item in from area in ServerSetup.Instance.GlobalMapCache.Values
                                 select ObjectManager.GetObjects<Item>(area, i => i.ItemPane == Item.ItemPanes.Ground)
                     into items
                                 from item in items
                                 let abandonedDiff = DateTime.UtcNow.Subtract(item.AbandonedDate)
                                 where (!(abandonedDiff.TotalMinutes <= 3) && (item.Template.Name is "Corpse"))
                                 select item)
                item.Remove();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void UpdateGroundMoney()
    {
        try
        {
            foreach (var money in ServerSetup.Instance.GlobalGroundMoneyCache.Values)
            {
                if (money == null) continue;
                var abandonedDiff = DateTime.UtcNow.Subtract(money.AbandonedDate);
                if (abandonedDiff.TotalMinutes <= 30) continue;
                var removed = ServerSetup.Instance.GlobalGroundMoneyCache.TryRemove(money.MoneyId, out var itemToBeRemoved);
                if (!removed) return;
                itemToBeRemoved.Remove();
            }
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void UpdateMonsters(TimeSpan elapsedTime)
    {
        try
        {
            Parallel.ForEach(ServerSetup.Instance.GlobalMapCache.Values, area =>
            {
                var monsters = ObjectManager.GetObjects<Monster>(area, i => !i.Skulled).ToList();
                if (monsters.Count <= 0) return;

                Parallel.ForEach(monsters, monster =>
                {
                    if (monster.Scripts == null) return;

                    if (monster.CurrentHp <= 0)
                    {
                        monster.Skulled = true;

                        if (monster.Target is Aisling aisling)
                        {
                            monster.Scripts.Values.FirstOrDefault()?.OnDeath(aisling.Client);
                        }
                        else
                        {
                            monster.Scripts.Values.FirstOrDefault()?.OnDeath();
                        }

                        return;
                    }

                    monster.Scripts.Values.FirstOrDefault()?.Update(elapsedTime);
                    monster.LastUpdated = DateTime.UtcNow;

                    if (!monster.MonsterBuffAndDebuffStopWatch.IsRunning)
                        monster.MonsterBuffAndDebuffStopWatch.Start();

                    if (monster.MonsterBuffAndDebuffStopWatch.Elapsed.TotalMilliseconds < 1000) return;

                    monster.UpdateBuffs(monster);
                    monster.UpdateDebuffs(monster);
                    monster.MonsterBuffAndDebuffStopWatch.Restart();
                });
            });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void UpdateMundanes(TimeSpan elapsedTime)
    {
        try
        {
            Parallel.ForEach(ServerSetup.Instance.GlobalMundaneCache.Values, (mundane) =>
            {
                if (mundane == null) return;
                mundane.Update(elapsedTime);
                mundane.LastUpdated = DateTime.UtcNow;
            });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private void CheckTraps(TimeSpan elapsedTime)
    {
        if (!_trapTimer.Update(elapsedTime)) return;

        try
        {
            Parallel.ForEach(ServerSetup.Instance.Traps.Values, (trap) => { trap?.Update(); });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void UpdateMaps(TimeSpan elapsedTime)
    {
        try
        {
            Parallel.ForEach(ServerSetup.Instance.GlobalMapCache.Values, (map) => { map?.Update(elapsedTime); });
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            SentrySdk.CaptureMessage($"Map failed to update; Reload Maps initiated: {DateTime.UtcNow}");

            // Wipe Caches
            ServerSetup.Instance.TempGlobalMapCache = [];
            ServerSetup.Instance.TempGlobalWarpTemplateCache = [];

            foreach (var npc in ServerSetup.Instance.GlobalMundaneCache.Values)
            {
                ObjectManager.DelObject(npc);
            }

            ServerSetup.Instance.GlobalMundaneCache = [];

            // Reload
            AreaStorage.Instance.CacheFromDatabase();
            DatabaseLoad.CacheFromDatabase(new WarpTemplate());

            foreach (var connected in ServerSetup.Instance.Game.Aislings)
            {
                connected.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qSelf-Heal Routine Invokes Reload Maps");
                connected.Client.ClientRefreshed();
            }
        }
    }

    private static void UpdateBoards()
    {
        try
        {
            ServerSetup.Instance.GlobalBoardPostCache.Clear();
            BoardPostStorage.CacheFromDatabase(AislingStorage.PersonalMailString);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    #endregion

    #region Server Utilities

    public static void CancelIfCasting(WorldClient client)
    {
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsCastingSpell)
            client.SendCancelCasting();

        client.Aisling.IsCastingSpell = false;
    }

    #endregion

    #region OnHandlers

    /// <summary>
    /// 0x05 - Request Map Data
    /// </summary>
    public ValueTask OnMapDataRequest(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling?.Map == null) return default;
        if (client.MapUpdating && client.Aisling.CurrentMapId != ServerSetup.Instance.Config.TransitionZone) return default;
        return ExecuteHandler(client, InnerOnMapDataRequest);

        static ValueTask InnerOnMapDataRequest(IWorldClient localClient)
        {
            try
            {
                localClient.MapUpdating = true;
                localClient.SendMapData();
            }
            finally
            {
                localClient.MapUpdating = false;
            }

            return default;
        }
    }

    /// <summary>
    /// 0x06 - Client Movement
    /// </summary>
    public ValueTask OnClientWalk(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling?.Map is not { Ready: true }) return default;
        var readyTime = DateTime.UtcNow;
        if (readyTime.Subtract(client.LastMapUpdated).TotalSeconds > 1)
            if (readyTime.Subtract(client.LastMovement).TotalSeconds < 0.30 && client.Aisling.MonsterForm == 0) return default;

        if (client.Aisling.CantMove)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "{=bYou cannot feel your legs...");
            client.ClientRefreshed();
            return default;
        }

        if (client.Aisling.Skulled)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();

            return default;
        }

        if (client.IsRefreshing && ServerSetup.Instance.Config.CancelWalkingIfRefreshing) return default;
        if (client.Aisling.IsCastingSpell && ServerSetup.Instance.Config.CancelCastingWhenWalking)
        {
            CancelIfCasting(client.Aisling.Client);
            return default;
        }

        var args = PacketSerializer.Deserialize<ClientWalkArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnClientWalk);

        static ValueTask InnerOnClientWalk(IWorldClient localClient, ClientWalkArgs localArgs)
        {
            localClient.Aisling.Direction = (byte)localArgs.Direction;
            var success = localClient.Aisling.Walk();

            if (success)
            {
                localClient.LastMovement = DateTime.UtcNow;

                if (localClient.Aisling.AreaId == ServerSetup.Instance.Config.TransitionZone)
                {
                    var portal = new PortalSession();
                    PortalSession.TransitionToMap(localClient.Aisling.Client);
                    return default;
                }

                localClient.CheckWarpTransitions(localClient.Aisling.Client);

                if (localClient.Aisling.Map?.Script.Item2 == null) return default;

                localClient.Aisling.Map.Script.Item2.OnPlayerWalk(localClient.Aisling.Client, localClient.Aisling.LastPosition, localClient.Aisling.Position);

                foreach (var trap in ServerSetup.Instance.Traps.Select(i => i.Value))
                {
                    if (trap?.Owner == null || trap.Owner.Serial == localClient.Aisling.Serial ||
                        localClient.Aisling.X != trap.Location.X ||
                        localClient.Aisling.Y != trap.Location.Y ||
                        localClient.Aisling.Map != trap.TrapItem.Map) continue;

                    if (trap.Owner is Aisling && !localClient.Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;

                    var triggered = Trap.Activate(trap, localClient.Aisling);
                    if (triggered) break;
                }
            }
            else
            {
                localClient.ClientRefreshed();
                localClient.CheckWarpTransitions(localClient.Aisling.Client);
            }

            return default;
        }
    }

    /// <summary>
    /// 0x07 - Object Pickup
    /// </summary>
    public ValueTask OnPickup(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling is null || client.Aisling.LoggedIn == false) return default;
        if (client.Aisling.IsDead())
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        if (client.Aisling.HasDebuff("Skulled") || client.Aisling.IsSleeping || client.Aisling.IsFrozen || client.Aisling.IsStopped)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        var args = PacketSerializer.Deserialize<PickupArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnPickup);

        ValueTask InnerOnPickup(IWorldClient localClient, PickupArgs localArgs)
        {
            var map = localClient.Aisling.Map;
            var itemObjs = ObjectManager.GetObjects<Item>(map, i => (int)i.Pos.X == localArgs.SourcePoint.X && (int)i.Pos.Y == localArgs.SourcePoint.Y).Where(i => !i.Template.Flags.FlagIsSet(ItemFlags.Trap)).ToList();
            var moneyObjs = ObjectManager.GetObjects(map, i => (int)i.Pos.X == localArgs.SourcePoint.X && (int)i.Pos.Y == localArgs.SourcePoint.Y, ObjectManager.Get.Money).ToList();

            if (!itemObjs.IsEmpty())
            {
                if (localClient.Aisling.Inventory.IsFull)
                {
                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYour inventory is full");
                    return default;
                }

                var item = itemObjs.FirstOrDefault();
                if (item?.CurrentMapId != localClient.Aisling.CurrentMapId) return default;
                if (!(localClient.Aisling.Position.DistanceFrom(item.Position) <= ServerSetup.Instance.Config.ClickLootDistance)) return default;
                var cantPickup = false;

                if (item.Template.Flags.FlagIsSet(ItemFlags.Unique) && item.Template.Name == "Necra Scribblings" && localClient.Aisling.Stage >= ClassStage.Master)
                {
                    if (itemObjs.Count >= 2)
                    {
                        cantPickup = true;
                    }
                    else
                        return default;
                }

                foreach (var invItem in localClient.Aisling.Inventory.Items.Values)
                {
                    if (invItem == null) continue;
                    if (!invItem.Template.Flags.FlagIsSet(ItemFlags.Unique)) continue;
                    if (invItem.Template.Name != item.Template.Name) continue;
                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, "You may only hold one in your possession.");

                    if (itemObjs.Count >= 2)
                    {
                        cantPickup = true;
                    }
                    else
                        return default;
                }

                foreach (var invItem in localClient.Aisling.BankManager.Items.Values)
                {
                    if (invItem == null) continue;
                    if (!invItem.Template.Flags.FlagIsSet(ItemFlags.Unique)) continue;
                    if (invItem.Template.Name != item.Template.Name) continue;
                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, "You may only hold one in your possession.");

                    if (itemObjs.Count >= 2)
                    {
                        cantPickup = true;
                    }
                    else
                        return default;
                }

                if (cantPickup)
                {
                    item = itemObjs.Last();
                }

                if (item.GiveTo(localClient.Aisling))
                {
                    item.Remove();
                    if (item.Scripts is null) return default;
                    foreach (var itemScript in item.Scripts.Values)
                        itemScript?.OnPickedUp(localClient.Aisling, new Position(localArgs.SourcePoint.X, localArgs.SourcePoint.Y), map);
                    return default;
                }
            }

            foreach (var obj in moneyObjs)
            {
                if (obj?.CurrentMapId != localClient.Aisling.CurrentMapId) break;
                if (!(localClient.Aisling.Position.DistanceFrom(obj.Position) <= ServerSetup.Instance.Config.ClickLootDistance)) break;

                if (obj is not Money money) continue;

                Money.GiveTo(money, localClient.Aisling);
            }

            return default;
        }
    }

    /// <summary>
    /// 0x08 - Drop Item
    /// </summary>
    public ValueTask OnItemDrop(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling is null || client.Aisling.LoggedIn == false) return default;
        if (client.Aisling.Map is not { Ready: true }) return default;
        if (client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.CantDropItems))
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        if (client.Aisling.IsDead())
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        if (client.Aisling.HasDebuff("Skulled") || client.Aisling.IsSleeping || client.Aisling.IsFrozen || client.Aisling.IsStopped)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        var args = PacketSerializer.Deserialize<ItemDropArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnItemDropped);

        static ValueTask InnerOnItemDropped(IWorldClient localClient, ItemDropArgs localArgs)
        {
            if (localArgs.SourceSlot is 0) return default;
            if (localArgs.Count is > 1000 or < 0) return default;
            if (!localClient.Aisling.Inventory.Items.TryGetValue(localArgs.SourceSlot, out var item)) return default;
            if (item == null) return default;

            if (item.Stacks > 1)
            {
                if (localArgs.Count > item.Stacks)
                {
                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, "Wait.. how many did I have again?");
                    return default;
                }
            }

            if (!item.Template.Flags.FlagIsSet(ItemFlags.Dropable))
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{ServerSetup.Instance.Config.CantDropItemMsg}");
                return default;
            }

            var itemPosition = new Position(localArgs.DestinationPoint.X, localArgs.DestinationPoint.Y);

            if (localClient.Aisling.Position.DistanceFrom(itemPosition.X, itemPosition.Y) > 11)
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "I can not do that. Too far.");
                return default;
            }

            if (localClient.Aisling.Map.IsWall(localArgs.DestinationPoint.X, localArgs.DestinationPoint.Y))
                if ((int)localClient.Aisling.Pos.X != localArgs.DestinationPoint.X || (int)localClient.Aisling.Pos.Y != localArgs.DestinationPoint.Y)
                {
                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, "Something is in the way.");
                    return default;
                }

            if (item.Template.Flags.FlagIsSet(ItemFlags.Stackable))
            {
                if (localArgs.Count > item.Stacks)
                {
                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, "Wait.. how many did I have again?");
                    return default;
                }

                var remaining = item.Stacks - (ushort)localArgs.Count;
                item.Dropping = localArgs.Count;

                if (remaining == 0)
                {
                    localClient.Aisling.Inventory.RemoveFromInventory(localClient.Aisling.Client, item);
                    item.Release(localClient.Aisling, new Position(localArgs.DestinationPoint.X, localArgs.DestinationPoint.Y));
                    CheckAltar(localClient, item);
                }
                else
                {
                    var temp = new Item
                    {
                        Slot = localArgs.SourceSlot,
                        Image = item.Image,
                        DisplayImage = item.DisplayImage,
                        Durability = item.Durability,
                        ItemVariance = item.ItemVariance,
                        WeapVariance = item.WeapVariance,
                        ItemQuality = item.ItemQuality,
                        OriginalQuality = item.OriginalQuality,
                        Stacks = (ushort)localArgs.Count,
                        Template = item.Template,
                        AbandonedDate = DateTime.UtcNow
                    };

                    temp.Release(localClient.Aisling, itemPosition);
                    CheckAltar(localClient, temp);

                    item.Stacks = (ushort)remaining;
                    localClient.SendRemoveItemFromPane(item.InventorySlot);
                    localClient.Aisling.Inventory.Items.TryUpdate(item.InventorySlot, item, item);
                    localClient.Aisling.Inventory.UpdateSlot(localClient.Aisling.Client, item);
                }
            }
            else
            {
                if (!item.Template.Flags.FlagIsSet(ItemFlags.DropScript))
                {
                    localClient.Aisling.Inventory.RemoveFromInventory(localClient.Aisling.Client, item);
                    item.Release(localClient.Aisling, new Position(localArgs.DestinationPoint.X, localArgs.DestinationPoint.Y));
                    CheckAltar(localClient, item);
                }
            }

            localClient.Aisling.Inventory.UpdatePlayersWeight(localClient.Aisling.Client);

            if (!item.Template.Flags.FlagIsSet(ItemFlags.DropScript))
            {
                localClient.Aisling.Map?.Script.Item2?.OnItemDropped(localClient.Aisling.Client, item, itemPosition);
            }

            if (item.Scripts == null) return default;
            foreach (var itemScript in item.Scripts.Values)
            {
                itemScript?.OnDropped(localClient.Aisling, new Position(localArgs.DestinationPoint.X, localArgs.DestinationPoint.Y), localClient.Aisling.Map);
            }

            return default;
        }
    }

    private static void CheckAltar(IWorldClient client, Item item)
    {
        switch (client.Aisling.Map.ID)
        {
            // Mileth Altar
            case 500:
                {
                    if ((item.X != 31 || item.Y != 52) && (item.X != 31 || item.Y != 53)) return;
                    item.Remove();
                    return;
                }
            // Undine Altar
            case 504:
                {
                    if ((item.X != 62 || item.Y != 47) && (item.X != 62 || item.Y != 48)) return;
                    item.Remove();
                    return;
                }
        }
    }

    /// <summary>
    /// 0x0B - Exit Request
    /// </summary>
    public ValueTask OnExitRequest(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<ExitRequestArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnExitRequest);

        ValueTask InnerOnExitRequest(IWorldClient localClient, ExitRequestArgs localArgs)
        {
            if (localClient?.Aisling == null) return default;

            if (localArgs.IsRequest)
            {
                localClient.SendConfirmExit();
                ClientRegistry.TryRemove(localClient.Id, out _);
            }
            else
            {
                var connectInfo = new IPEndPoint(_serverTable.Servers[0].Address, _serverTable.Servers[0].Port);
                var redirect = new Redirect(EphemeralRandomIdGenerator<uint>.Shared.NextId,
                    new Chaos.Networking.Options.ConnectionInfo { Address = connectInfo.Address, Port = connectInfo.Port },
                    ServerType.Login,
                    Encoding.ASCII.GetString(localClient.Crypto.Key),
                    localClient.Crypto.Seed);

                RedirectManager.Add(redirect);
                localClient.SendRedirect(redirect);
            }

            return default;
        }
    }

    /// <summary>
    /// 0x0C - Display Object Request
    /// </summary>
    public ValueTask OnDisplayEntityRequest(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<DisplayEntityRequestArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnDisplayEntityRequest);

        ValueTask InnerOnDisplayEntityRequest(IWorldClient localClient, DisplayEntityRequestArgs localArgs)
        {
            var aisling = localClient.Aisling;
            var mapInstance = aisling.Map;
            var sprite = ObjectManager.GetObjects(mapInstance, s => s.WithinRangeOf(aisling), ObjectManager.Get.All).ToList().FirstOrDefault(t => t.Serial == localArgs.TargetId);

            if (sprite is null) return default;
            if (aisling.CanSeeSprite(sprite)) return default;
            if (sprite is not Monster monster) return default;
            var script = monster.Scripts.FirstOrDefault().Value;
            script?.OnLeave(aisling.Client);
            return default;
        }
    }

    /// <summary>
    /// 0x0D - Ignore Player
    /// </summary>
    public ValueTask OnIgnore(IWorldClient client, in Packet clientPacket)
    {
        if (client != null && !client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<IgnoreArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnIgnore);

        static ValueTask InnerOnIgnore(IWorldClient localClient, IgnoreArgs localArgs)
        {
            switch (localArgs.IgnoreType)
            {
                case IgnoreType.Request:
                    var ignored = string.Join(", ", localClient.Aisling.IgnoredList);
                    localClient.SendServerMessage(ServerMessageType.NonScrollWindow, ignored);
                    break;
                case IgnoreType.AddUser:
                    if (localArgs.TargetName == null) break;
                    if (localArgs.TargetName.EqualsIgnoreCase("Death")) break;
                    if (localClient.Aisling.IgnoredList.ListContains(localArgs.TargetName)) break;
                    localClient.AddToIgnoreListDb(localArgs.TargetName);
                    break;
                case IgnoreType.RemoveUser:
                    if (localArgs.TargetName == null) break;
                    if (localArgs.TargetName.EqualsIgnoreCase("Death")) break;
                    if (!localClient.Aisling.IgnoredList.ListContains(localArgs.TargetName)) break;
                    localClient.RemoveFromIgnoreListDb(localArgs.TargetName);
                    break;
            }

            return default;
        }
    }

    /// <summary>
    /// 0x0E - Public Chat (Limited to 3 times a second)
    /// </summary>
    public ValueTask OnPublicMessage(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsSilenced) return default;
        var args = PacketSerializer.Deserialize<PublicMessageArgs>(in clientPacket);
        var readyTime = DateTime.UtcNow;
        return readyTime.Subtract(client.LastMessageSent).TotalSeconds < 0.30 ? default : ExecuteHandler(client, args, InnerOnPublicMessage);

        ValueTask InnerOnPublicMessage(IWorldClient localClient, PublicMessageArgs localArgs)
        {
            if (localClient.Aisling.DrunkenFist)
            {
                var slurred = Generator.RandomNumPercentGen();
                if (slurred >= .50)
                {
                    const string drunk = "..   .hic!  ";
                    var drunkSpot = Random.Shared.Next(0, localArgs.Message.Length);
                    localArgs.Message = localArgs.Message.Remove(drunkSpot).Insert(drunkSpot, drunk);
                }
            }
            localClient.LastMessageSent = readyTime;
            string response;
            IEnumerable<Aisling> audience;

            if (ParseCommand()) return default;

            switch (localArgs.PublicMessageType)
            {
                case PublicMessageType.Normal:
                    response = $"{localClient.Aisling.Username}: {localArgs.Message}";
                    audience = localClient.Aisling.AislingsEarShotNearby();
                    break;
                case PublicMessageType.Shout:
                    response = $"{localClient.Aisling.Username}! {localArgs.Message}";
                    audience = localClient.Aisling.AislingsOnMap();
                    break;
                case PublicMessageType.Chant:
                    response = localArgs.Message;
                    audience = localClient.Aisling.AislingsNearby();
                    break;
                default:
                    localClient.Disconnect();
                    return default;
            }

            var playersToShowList = audience.Where(player => !player.IgnoredList.ListContains(localClient.Aisling.Username));
            var toShowList = playersToShowList as Aisling[] ?? playersToShowList.ToArray();
            localClient.Aisling.SendTargetedClientMethod(PlayerScope.DefinedAislings, c => c.SendPublicMessage(localClient.Aisling.Serial, localArgs.PublicMessageType, response), toShowList);

            var nearbyMundanes = localClient.Aisling.MundanesNearby();

            foreach (var npc in nearbyMundanes)
            {
                if (npc?.Scripts is null) continue;

                foreach (var script in npc.Scripts.Values)
                    script?.OnGossip(localClient.Aisling.Client, localArgs.Message);
            }

            localClient.Aisling.Map.Script.Item2.OnGossip(localClient.Aisling.Client, localArgs.Message);

            return default;

            bool ParseCommand()
            {
                if (!localClient.Aisling.GameMaster) return false;
                if (!localArgs.Message.StartsWith("/")) return false;
                Commander.ParseChatMessage(localClient.Aisling.Client, localArgs.Message);
                return true;
            }
        }
    }

    /// <summary>
    /// 0x0F - Spell Use
    /// </summary>
    public ValueTask OnSpellUse(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead() || client.Aisling.Skulled) return default;

        if (client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.CantUseAbilities))
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        var args = PacketSerializer.Deserialize<SpellUseArgs>(in clientPacket);

        if (!client.Aisling.Client.SpellControl.IsRunning)
            client.Aisling.Client.SpellControl.Start();

        if (client.Aisling.Client.SpellControl.Elapsed.TotalMilliseconds <
            client.Aisling.Client.SkillSpellTimer.Delay.TotalMilliseconds - 200) return default;

        client.Aisling.Client.SpellControl.Restart();
        return ExecuteHandler(client, args, InnerOnUseSpell);

        ValueTask InnerOnUseSpell(IWorldClient localClient, SpellUseArgs localArgs)
        {
            var spell = localClient.Aisling.SpellBook.TryGetSpells(i => i != null && i.Slot == localArgs.SourceSlot).FirstOrDefault();
            if (spell == null)
            {
                localClient.SendCancelCasting();
                localClient.Aisling.SpellBook = new SpellBook();
                localClient.LoadSpellBook();
                return default;
            }

            if (localClient.Aisling.CantCast)
            {
                if (spell.Template.Name is not ("Ao Suain" or "Ao Sith"))
                {
                    localClient.SendServerMessage(ServerMessageType.OrangeBar1, "I am unable to cast that spell..");
                    localClient.SendCancelCasting();
                    return default;
                }
            }

            if (DateTime.UtcNow.Subtract(localClient.LastSpellCast).TotalMilliseconds < 750)
            {
                if (spell == localClient.Aisling.Client.LastSpell) return default;
            }

            localClient.LastSpellCast = DateTime.UtcNow;
            localClient.Aisling.Client.LastSpell = spell;
            var info = new CastInfo();

            if (localClient.SpellCastInfo is null)
            {
                if (localArgs.ArgsData.IsEmpty())
                {
                    info = new CastInfo
                    {
                        Slot = localArgs.SourceSlot,
                        Target = 0,
                        Position = new Position()
                    };
                }
                else
                {
                    info = new CastInfo
                    {
                        Slot = localArgs.SourceSlot,
                        Target = 0,
                        Position = new Position(),
                        Data = localArgs.ArgsData.ToString()
                    };
                }
            }
            else
            {
                info.Slot = localClient.SpellCastInfo.Slot;
                info.Target = localClient.SpellCastInfo.Target;
                info.Position = localClient.SpellCastInfo.Position;
                if (!localArgs.ArgsData.IsEmpty())
                    info.Data = localArgs.ArgsData.ToString();
            }

            var source = localClient.Aisling;

            //it's impossible to know what kind of spell is being used during deserialization
            //there is no spell type specified in the packet, so we arent sure if the packet will
            //contains a prompt or target info
            //so we have to do that deserialization here, where we know what spell type we're dealing with
            //we also need to build the activation context for the spell
            switch (spell.Template.TargetType)
            {
                case SpellTemplate.SpellUseType.None:
                    return default;
                case SpellTemplate.SpellUseType.Prompt:
                    if (!localArgs.ArgsData.IsEmpty())
                        info.Data = PacketSerializer.Encoding.GetString(localArgs.ArgsData);
                    break;
                case SpellTemplate.SpellUseType.ChooseTarget:
                    if (!localArgs.ArgsData.IsEmpty())
                    {
                        var targetIdSegment = new ArraySegment<byte>(localArgs.ArgsData, 0, 4);
                        var targetPointSegment = new ArraySegment<byte>(localArgs.ArgsData, 4, 4);
                        var targetId = (uint)((targetIdSegment[0] << 24)
                                              | (targetIdSegment[1] << 16)
                                              | (targetIdSegment[2] << 8)
                                              | targetIdSegment[3]);
                        var targetPoint = new Position((targetPointSegment[0] << 8) | targetPointSegment[1],
                            (targetPointSegment[2] << 8) | targetPointSegment[3]);
                        info.Position = targetPoint;
                        info.Target = targetId;
                    }
                    break;
                case SpellTemplate.SpellUseType.OneDigit:
                case SpellTemplate.SpellUseType.TwoDigit:
                case SpellTemplate.SpellUseType.ThreeDigit:
                case SpellTemplate.SpellUseType.FourDigit:
                case SpellTemplate.SpellUseType.NoTarget:
                    info.Target = source.Serial;
                    break;
            }

            info.Position ??= new Position(localClient.Aisling.X, localClient.Aisling.Y);
            localClient.Aisling.CastSpell(spell, info);
            return default;
        }
    }

    /// <summary>
    /// 0x10 - On Redirect
    /// </summary>
    public ValueTask OnClientRedirected(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<ClientRedirectedArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnClientRedirected);

        ValueTask InnerOnClientRedirected(IWorldClient localClient, ClientRedirectedArgs localArgs)
        {
            if (!RedirectManager.TryGetRemove(localArgs.Id, out var redirect))
            {
                SentrySdk.CaptureMessage($"{client.RemoteIp} tried to redirect to the world with invalid details.");
                localClient.Disconnect();
                return default;
            }

            //keep this case sensitive
            if (localArgs.Name != redirect.Name)
            {
                SentrySdk.CaptureMessage($"{client.RemoteIp} tried to impersonate a redirect with redirect {redirect.Id}.");
                localClient.Disconnect();
                return default;
            }

            ServerSetup.ConnectionLogger($"Received successful redirect: {redirect.Id}");
            var existingAisling = Aislings.FirstOrDefault(user => user.Username.EqualsI(redirect.Name));

            //double logon, disconnect both clients
            if (existingAisling == null && redirect.Type != ServerType.Lobby) return LoadAislingAsync(localClient, redirect);
            localClient.Disconnect();
            if (redirect.Type == ServerType.Lobby) return default;
            ServerSetup.ConnectionLogger($"Duplicate login, player {redirect.Name}, disconnecting both clients.");
            existingAisling?.Client.Disconnect();
            return default;
        }
    }

    private static async ValueTask LoadAislingAsync(IWorldClient client, IRedirect redirect)
    {
        client.Crypto = new Crypto(redirect.Seed, redirect.Key, redirect.Name);

        try
        {
            var exists = await AislingStorage.CheckPassword(redirect.Name);
            var aisling = await StorageManager.AislingBucket.LoadAisling(redirect.Name, exists.Serial);
            if (aisling == null)
            {
                SentrySdk.CaptureMessage($"Unable to retrieve player data: {client.RemoteIp}");
                client.Disconnect();
                return;
            }
            client.Aisling = aisling;
            SetPriorToLoad(client);
            client.Aisling.Serial = aisling.Serial;
            client.Aisling.Pos = new Vector2(aisling.X, aisling.Y);
            aisling.Client = client as WorldClient;
            aisling.GameMaster = ServerSetup.Instance.Config.GameMasters?.Any(n =>
                string.Equals(n, aisling.Username, StringComparison.OrdinalIgnoreCase)) ?? false;

            if (client.Aisling._Str <= 0 || client.Aisling._Int <= 0 || client.Aisling._Wis <= 0 ||
                client.Aisling._Con <= 0 || client.Aisling._Dex <= 0)
            {
                SentrySdk.CaptureMessage($"Player {client.Aisling.Username} has corrupt stats.");
                client.Disconnect();
                return;
            }

            if (client.Aisling.Map != null) client.Aisling.CurrentMapId = client.Aisling.Map.ID;
            client.LoggedIn(false);
            client.Aisling.EquipmentManager.Client = client as WorldClient;
            client.Aisling.CurrentWeight = 0;
            client.Aisling.ActiveStatus = ActivityStatus.Awake;
            client.Aisling.OldColor = client.Aisling.HairColor;
            client.Aisling.OldStyle = client.Aisling.HairStyle;

            if (aisling.GameMaster)
            {
                var ipLocal = IPAddress.Parse(ServerSetup.Instance.InternalAddress);

                if (!GameMastersIPs.Any(ip => client.RemoteIp.Equals(IPAddress.Parse(ip)))
                    && !IPAddress.IsLoopback(client.RemoteIp) && !client.RemoteIp.Equals(ipLocal))
                {
                    ServerSetup.ConnectionLogger($"Failed to login GM from {client.RemoteIp}.");
                    SentrySdk.CaptureMessage($"Failed to login GM from {client.RemoteIp}.");
                    client.Disconnect();
                    return;
                }
            }

            try
            {
                var load = await client.Aisling.Client.Load();

                if (load == null)
                {
                    ServerSetup.ConnectionLogger($"Failed to load player to client - exiting");
                    client.Disconnect();
                    return;
                }

                client.SendServerMessage(ServerMessageType.ActiveMessage,
                    $"{ServerSetup.Instance.Config.ServerWelcomeMessage}: {client.Aisling.Username}");
                client.SendAttributes(StatUpdateType.Full);
                client.LoggedIn(true);

                if (client.Aisling.Map != null && client.Aisling.IsDead())
                {
                    client.AislingToGhostForm();
                    if (!client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
                        client.Aisling.WarpToHell();
                }

                if (client.Aisling.AreaId == ServerSetup.Instance.Config.TransitionZone)
                {
                    var portal = new PortalSession();
                    PortalSession.TransitionToMap(client.Aisling.Client);
                }
            }
            catch (Exception e)
            {
                ServerSetup.ConnectionLogger($"Failed to add player {redirect.Name} to world server.");
                SentrySdk.CaptureException(e);
                client.Disconnect();
            }
        }
        catch (Exception e)
        {
            ServerSetup.ConnectionLogger($"Client with ip {client.RemoteIp} failed to load player {redirect.Name}.");
            SentrySdk.CaptureException(e);
            client.Disconnect();
        }
        finally
        {
            ServerSetup.ConnectionLogger($"{redirect.Name} logged in at: {DateTime.Now} on {client.RemoteIp}");
        }
    }

    private static void SetPriorToLoad(IWorldClient client)
    {
        var aisling = client.Aisling;
        aisling.SkillBook ??= new SkillBook();
        aisling.SpellBook ??= new SpellBook();
        aisling.Inventory ??= new InventoryManager();
        aisling.BankManager ??= new BankManager();
        aisling.EquipmentManager ??= new EquipmentManager(aisling.Client);
        aisling.QuestManager ??= new Quests();
    }

    /// <summary>
    /// 0x11 - Change Direction
    /// </summary>
    public ValueTask OnTurn(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<TurnArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnTurn);

        static ValueTask InnerOnTurn(IWorldClient localClient, TurnArgs localArgs)
        {
            localClient.Aisling.Direction = (byte)localArgs.Direction;

            if (localClient.Aisling.Skulled)
            {
                localClient.SendLocation();
                return default;
            }

            localClient.Aisling.Turn();

            return default;
        }
    }

    /// <summary>
    /// 0x13 - On Spacebar (Limited to 2 times a second)
    /// </summary>
    public ValueTask OnSpacebar(IWorldClient client, in Packet clientPacket)
    {
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;

        if (client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.CantUseAbilities))
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        var readyTime = DateTime.UtcNow;
        var overburden = 0;
        if (client.Aisling.Overburden)
            overburden = 2;
        if (readyTime.Subtract(client.LastAssail).TotalSeconds < 1 + overburden) return default;
        if (ServerSetup.Instance.Config.AssailsCancelSpells)
            client.SendCancelCasting();

        if (!client.Aisling.Skulled)
            return client.Aisling.CantAttack ? default : ExecuteHandler(client, InnerOnSpacebar);

        client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
        return default;

        static ValueTask InnerOnSpacebar(IWorldClient localClient)
        {
            AssailRoutine(localClient);
            return default;
        }
    }

    private static void AssailRoutine(IWorldClient lpClient)
    {
        var lastTemplate = string.Empty;

        foreach (var skill in lpClient.Aisling.GetAssails())
        {
            // Skill exists check
            if (skill?.Template == null) continue;
            if (lastTemplate == skill.Template.Name) continue;
            if (skill.Scripts == null) continue;

            // Skill can be used check
            if (!skill.Ready && skill.InUse) continue;

            skill.InUse = true;

            // Skill animation and execute
            ExecuteAssail(lpClient, skill);

            // Skill cleanup
            skill.CurrentCooldown = skill.Template.Cooldown;
            lpClient.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
            lastTemplate = skill.Template.Name;
            lpClient.LastAssail = DateTime.UtcNow;
            skill.LastUsedSkill = DateTime.UtcNow;

            skill.InUse = false;
        }

        if (lpClient.Aisling.Overburden)
            lpClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bOverburdened!");
    }

    private static void ExecuteAssail(IWorldClient lpClient, Skill lpSkill, bool optExecuteScript = true)
    {
        // On skill "Assail" also use weapon script, if there is one
        if (lpSkill.Template.ScriptName == "Assail")
        {
            // Uses a script equipped to the main-hand item if there is one
            var mainHandScript = lpClient.Aisling.EquipmentManager.Equipment[1]?.Item?.WeaponScripts;
            mainHandScript?.FirstOrDefault().Value.OnUse(lpClient.Aisling);

            // Uses a script associated with an accessory like Quivers
            var accessoryScript = lpClient.Aisling.EquipmentManager.Equipment[14]?.Item?.WeaponScripts;
            accessoryScript?.FirstOrDefault().Value.OnUse(lpClient.Aisling);
        }

        if (!optExecuteScript) return;
        var script = lpSkill.Scripts.Values.FirstOrDefault();
        script?.OnUse(lpClient.Aisling);
    }

    /// <summary>
    /// 0x18 - Request World List (Limited to 2 times a second)
    /// </summary>
    public ValueTask OnWorldListRequest(IWorldClient client, in Packet clientPacket)
    {
        if (!client.Aisling.LoggedIn) return default;
        if (client.IsRefreshing) return default;
        var readyTime = DateTime.UtcNow;
        return readyTime.Subtract(client.LastWorldListRequest).TotalSeconds < 0.50 ? default : ExecuteHandler(client, InnerOnWorldListRequest);

        ValueTask InnerOnWorldListRequest(IWorldClient localClient)
        {
            localClient.LastWorldListRequest = readyTime;
            localClient.SendWorldList(Aislings.ToList());

            return default;
        }
    }

    /// <summary>
    /// 0x19 - Private Message (Limited to 3 times a second)
    /// </summary>
    public ValueTask OnWhisper(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<WhisperArgs>(in clientPacket);
        var readyTime = DateTime.UtcNow;
        return readyTime.Subtract(client.LastWhisperMessageSent).TotalSeconds < 0.30 ? default : ExecuteHandler(client, args, InnerOnWhisper);

        ValueTask InnerOnWhisper(IWorldClient localClient, WhisperArgs localArgs)
        {
            var fromAisling = localClient.Aisling;
            if (localArgs.TargetName.Length > 12) return default;
            if (localArgs.Message.Length > 100) return default;
            if (localClient.Aisling.DrunkenFist)
            {
                var slurred = Generator.RandomNumPercentGen();
                if (slurred >= .50)
                {
                    const string drunk = "..   .hic!  ";
                    var drunkSpot = Random.Shared.Next(0, localArgs.Message.Length);
                    localArgs.Message = localArgs.Message.Remove(drunkSpot).Insert(drunkSpot, drunk);
                }
            }
            client.LastWhisperMessageSent = readyTime;
            var maxLength = CONSTANTS.MAX_MESSAGE_LINE_LENGTH - localArgs.TargetName.Length - 4;
            if (localArgs.Message.Length > maxLength)
                localArgs.Message = localArgs.Message[..maxLength];

            switch (localArgs.TargetName)
            {
                case "#" when client.Aisling.GameMaster:
                    foreach (var player in Aislings)
                    {
                        player.Client?.SendServerMessage(ServerMessageType.GroupChat, $"{{=b{client.Aisling.Username}{{=q: {localArgs.Message}");
                    }
                    return default;
                case "#" when client.Aisling.GameMaster != true:
                    client.SystemMessage("You cannot broadcast in this way.");
                    return default;
                case "!":
                    foreach (var player in Aislings)
                    {
                        if (player.Client is null) continue;
                        if (!player.GameSettings.GroupChat) continue;
                        if (player.IgnoredList.ListContains(client.Aisling.Username)) continue;
                        player.Client.SendServerMessage(ServerMessageType.GuildChat, $"{{=q{client.Aisling.Username}{{=a: {localArgs.Message}");
                    }
                    return default;
                case "!!" when client.Aisling.GroupParty?.PartyMembers != null:
                    foreach (var player in Aislings)
                    {
                        if (player.Client is null) continue;
                        if (!player.GameSettings.GroupChat) continue;
                        if (player.GroupParty == client.Aisling.GroupParty)
                        {
                            player.Client.SendServerMessage(ServerMessageType.GroupChat, $"[!{client.Aisling.Username}] {localArgs.Message}");
                        }
                    }
                    return default;
                case "!!" when client.Aisling.GroupParty?.PartyMembers == null:
                    client.SystemMessage("{=eYou're not in a group or party.");
                    return default;
            }

            var targetAisling = Aislings.FirstOrDefault(player => player.Username.EqualsI(localArgs.TargetName));

            if (targetAisling == null)
            {
                fromAisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{localArgs.TargetName} is not online.. :'(");
                return default;
            }

            if (targetAisling.Equals(fromAisling))
            {
                localClient.SendServerMessage(ServerMessageType.Whisper, "Little voice in yer head eh?");
                return default;
            }

            if (!targetAisling.GameSettings.Whisper)
            {
                localClient.SendServerMessage(ServerMessageType.Whisper, "Has direct messaging turned off");
                return default;
            }

            if (targetAisling.ActiveStatus == ActivityStatus.DoNotDisturb || targetAisling.IgnoredList.ListContains(fromAisling.Username))
            {
                localClient.SendServerMessage(ServerMessageType.Whisper, $"{targetAisling.Username} doesn't want to be bothered");
                return default;
            }

            localClient.SendServerMessage(ServerMessageType.Whisper, $"[{targetAisling.Username}]> {localArgs.Message}");
            targetAisling.Client.SendServerMessage(ServerMessageType.Whisper, $"[{fromAisling.Username}]: {localArgs.Message}");

            return default;
        }
    }

    /// <summary>
    /// 0x1B - User Option Toggle
    /// </summary>
    public ValueTask OnOptionToggle(IWorldClient client, in Packet clientPacket)
    {
        if (client.Aisling.GameSettings == null) return default;
        var args = PacketSerializer.Deserialize<OptionToggleArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnUsrOptionToggle);

        static ValueTask InnerOnUsrOptionToggle(IWorldClient localClient, OptionToggleArgs localArgs)
        {
            if (localArgs.UserOption == UserOption.Request)
            {
                localClient.SendServerMessage(ServerMessageType.UserOptions, localClient.Aisling.GameSettings.ToString());

                return default;
            }

            localClient.Aisling.GameSettings.Toggle(localArgs.UserOption);
            localClient.SendServerMessage(ServerMessageType.UserOptions, localClient.Aisling.GameSettings.ToString(localArgs.UserOption));

            return default;
        }
    }

    /// <summary>
    /// 0x1C - Item Usage (Limited to 3 times a second)
    /// </summary>
    public ValueTask OnItemUse(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling?.Map is not { Ready: true }) return default;
        if (!client.Aisling.LoggedIn) return default;
        var readyTime = DateTime.UtcNow;
        if (readyTime.Subtract(client.LastItemUsed).TotalSeconds < 0.33) return default;
        var args = PacketSerializer.Deserialize<ItemUseArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnUseItem);

        static ValueTask InnerOnUseItem(IWorldClient localClient, ItemUseArgs localArgs)
        {
            localClient.LastItemUsed = DateTime.UtcNow;

            if (localClient.Aisling.IsDead())
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "You cannot do that.");
                return default;
            }

            if (localClient.Aisling.Map.Flags.MapFlagIsSet(MapFlags.CantUseItems))
            {
                localClient.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
                return default;
            }

            // Speed equipping prevent (movement)
            if (!localClient.IsEquipping)
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "Slow down");
                return default;
            }

            var item = localClient.Aisling.Inventory.Get(i => i != null && i.InventorySlot == localArgs.SourceSlot).FirstOrDefault();
            if (item?.Template == null) return default;

            if ((localClient.Aisling.HasDebuff("Skulled") || localClient.Aisling.IsBlocked) && item.Template.Name != "Betrayal Blossom")
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "You cannot do that.");
                return default;
            }

            if (item.Template.Flags.FlagIsSet(ItemFlags.Equipable))
                localClient.LastEquip = DateTime.UtcNow;

            var activated = false;

            // Run Scripts on item on use
            if (!string.IsNullOrEmpty(item.Template.ScriptName)) item.Scripts ??= ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);
            if (!string.IsNullOrEmpty(item.Template.WeaponScript)) item.WeaponScripts ??= ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);

            if (item.Scripts == null)
            {
                localClient.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.CantUseThat}");
            }
            else
            {
                var script = item.Scripts.Values.FirstOrDefault();
                script?.OnUse(localClient.Aisling, localArgs.SourceSlot);
                activated = true;
            }

            if (!activated) return default;
            if (!item.Template.Flags.FlagIsSet(ItemFlags.Consumable)) return default;

            localClient.Aisling.Inventory.RemoveRange(localClient.Aisling.Client, item, 1);

            return default;
        }
    }

    /// <summary>
    /// 0x1D - Emote Usage
    /// </summary>
    public ValueTask OnEmote(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.IsRefreshing) return default;
        if (client.Aisling.IsDead()) return default;
        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.SendLocation();
            return default;
        }

        var args = PacketSerializer.Deserialize<EmoteArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnEmote);

        ValueTask InnerOnEmote(IWorldClient localClient, EmoteArgs localArgs)
        {
            if ((int)localArgs.BodyAnimation <= 44)
                localClient.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(localClient.Aisling.Serial, localArgs.BodyAnimation, 120));

            return default;
        }
    }

    /// <summary>
    /// 0x24 - Drop Gold
    /// </summary>
    public ValueTask OnGoldDrop(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;
        if (client.Aisling.CantAttack) return default;
        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.SendLocation();
            return default;
        }

        var args = PacketSerializer.Deserialize<GoldDropArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnGoldDropped);

        ValueTask InnerOnGoldDropped(IWorldClient localClient, GoldDropArgs localArgs)
        {
            if (localArgs.Amount <= 0) return default;

            if (client.Aisling.GoldPoints >= (uint)localArgs.Amount)
            {
                client.Aisling.GoldPoints -= (uint)localArgs.Amount;
                if (client.Aisling.GoldPoints <= 0)
                    client.Aisling.GoldPoints = 0;

                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.YouDroppedGoldMsg}");
                client.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislingsExludingSelf, c => c.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.UserDroppedGoldMsg.Replace("noname", client.Aisling.Username)}"));

                Money.Create(client.Aisling, (uint)localArgs.Amount, new Position(localArgs.DestinationPoint.X, localArgs.DestinationPoint.Y));
                client.SendAttributes(StatUpdateType.ExpGold);
            }
            else
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NotEnoughGoldToDropMsg}");
            }

            return default;
        }
    }

    /// <summary>
    /// 0x29 - Drop Item on Sprite
    /// </summary>
    public ValueTask OnItemDroppedOnCreature(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;
        if (client.Aisling.CantAttack) return default;

        var args = PacketSerializer.Deserialize<ItemDroppedOnCreatureArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnItemDroppedOnCreature);

        ValueTask InnerOnItemDroppedOnCreature(IWorldClient localClient, ItemDroppedOnCreatureArgs localArgs)
        {
            var result = new List<Sprite>();
            var listA = ObjectManager.GetObjects<Monster>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();
            var listB = ObjectManager.GetObjects<Mundane>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();
            var listC = ObjectManager.GetObjects<Aisling>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();
            result.AddRange(listA);
            result.AddRange(listB);
            result.AddRange(listC);

            foreach (var sprite in result.Where(sprite => sprite.Serial == localArgs.TargetId))
            {
                switch (sprite)
                {
                    case Monster monster:
                        {
                            var script = monster.Scripts?.Values.FirstOrDefault();
                            if (script is null) return default;
                            var item = localClient.Aisling.Inventory.FindInSlot(localArgs.SourceSlot);
                            item.Serial = monster.Serial;
                            if (item.Template.Flags.FlagIsSet(ItemFlags.Dropable) && !item.Template.Flags.FlagIsSet(ItemFlags.DropScript))
                                script?.OnItemDropped(localClient.Aisling.Client, item);
                            else
                                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "I can't seem to do that");
                            break;
                        }
                    case Mundane mundane:
                        {
                            var script = mundane.Scripts?.Values.FirstOrDefault();
                            if (script is null) return default;
                            var item = localClient.Aisling.Inventory.FindInSlot(localArgs.SourceSlot);
                            item.Serial = mundane.Serial;
                            localClient.EntryCheck = mundane.Serial;
                            mundane.Bypass = true;
                            script?.OnItemDropped(localClient.Aisling.Client, item);
                            break;
                        }
                    case Aisling aisling:
                        {
                            if (localArgs.SourceSlot == 0) return default;
                            var item = localClient.Aisling.Inventory.FindInSlot(localArgs.SourceSlot);

                            if (item.DisplayName.StringContains("deum"))
                            {
                                var script = item.Scripts?.Values.FirstOrDefault();
                                if (script is null) return default;
                                localClient.Aisling.Inventory.RemoveRange(localClient.Aisling.Client, item, 1);
                                localClient.Aisling.ThrewHealingPot = true;
                                script?.OnUse(aisling, localArgs.SourceSlot);
                                localClient.SendBodyAnimation(localClient.Aisling.Serial, BodyAnimation.Assail, 50);
                                return default;
                            }

                            if (item.DisplayName == "Elixir of Life")
                            {
                                localClient.Aisling.Inventory.RemoveRange(localClient.Aisling.Client, item, 1);
                                localClient.Aisling.ThrewHealingPot = true;
                                localClient.Aisling.ReviveFromAfar(aisling);
                                localClient.SendBodyAnimation(localClient.Aisling.Serial, BodyAnimation.Assail, 50);
                                return default;
                            }

                            if (item.Template.Flags.FlagIsSet(ItemFlags.Dropable) && !item.Template.Flags.FlagIsSet(ItemFlags.DropScript))
                            {
                                // Check Game Settings
                                if (!localClient.Aisling.GameSettings.Exchange)
                                {
                                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou have trading turned off");
                                    return default;
                                }

                                if (!aisling.GameSettings.Exchange)
                                {
                                    localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{aisling.Username}, is not actively trading");
                                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=qTrade ignored");
                                    return default;
                                }

                                localClient.Aisling.Exchange = new ExchangeSession(aisling);
                                aisling.Exchange = new ExchangeSession(localClient.Aisling);
                                localClient.SendExchangeStart(aisling);
                                aisling.Client.SendExchangeStart(localClient.Aisling);

                                if (aisling.CurrentWeight + item.Template.CarryWeight < aisling.MaximumWeight)
                                {
                                    localClient.Aisling.Inventory.RemoveFromInventory(localClient.Aisling.Client, item);
                                    localClient.Aisling.Exchange.Items.Add(item);
                                    localClient.Aisling.Exchange.Weight += item.Template.CarryWeight;
                                    localClient.Aisling.Client.SendExchangeAddItem(false,
                                        (byte)localClient.Aisling.Exchange.Items.Count, item);
                                    aisling.Client.SendExchangeAddItem(true, (byte)localClient.Aisling.Exchange.Items.Count,
                                        item);
                                    break;
                                }

                                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "They can't seem to lift that. The trade has been cancelled.");
                                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "That item seems to be too heavy for you, trade has been cancelled.");
                            }
                            else
                            {
                                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "I can't just give this away");
                            }

                            break;
                        }
                }
            }

            return default;
        }
    }

    /// <summary>
    /// 0x2A - Drop Gold on Sprite
    /// </summary>
    public ValueTask OnGoldDroppedOnCreature(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;
        if (client.Aisling.CantAttack) return default;

        var args = PacketSerializer.Deserialize<GoldDroppedOnCreatureArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnGoldDroppedOnCreature);

        ValueTask InnerOnGoldDroppedOnCreature(IWorldClient localClient, GoldDroppedOnCreatureArgs localArgs)
        {
            var result = new List<Sprite>();
            var listA = ObjectManager.GetObjects<Monster>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();
            var listB = ObjectManager.GetObjects<Mundane>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();
            var listC = ObjectManager.GetObjects<Aisling>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();

            result.AddRange(listA);
            result.AddRange(listB);
            result.AddRange(listC);

            foreach (var sprite in result.Where(sprite => sprite.Serial == localArgs.TargetId))
            {
                switch (sprite)
                {
                    case Monster monster:
                        {
                            var script = monster.Scripts.Values.FirstOrDefault();
                            if (localArgs.Amount <= 0) return default;
                            script?.OnGoldDropped(localClient.Aisling.Client, (uint)localArgs.Amount);
                            break;
                        }
                    case Mundane mundane:
                        {
                            var script = mundane.Scripts.Values.FirstOrDefault();
                            if (localArgs.Amount <= 0) return default;
                            script?.OnGoldDropped(localClient.Aisling.Client, (uint)localArgs.Amount);
                            break;
                        }
                    case Aisling aisling:
                        {
                            // Check Game Settings
                            if (!localClient.Aisling.GameSettings.Exchange)
                            {
                                localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou have trading turned off");
                                return default;
                            }

                            if (!aisling.GameSettings.Exchange)
                            {
                                localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{aisling.Username}, is not actively trading");
                                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{{=qTrade ignored");
                                return default;
                            }

                            localClient.Aisling.Exchange = new ExchangeSession(aisling);
                            aisling.Exchange = new ExchangeSession(localClient.Aisling);
                            localClient.SendExchangeStart(aisling);
                            aisling.Client.SendExchangeStart(localClient.Aisling);

                            if ((uint)localArgs.Amount > localClient.Aisling.GoldPoints)
                            {
                                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "You don't have that much to give");
                                break;
                            }

                            if (aisling.GoldPoints + (uint)localArgs.Amount > ServerSetup.Instance.Config.MaxCarryGold)
                            {
                                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "Player cannot hold that amount");
                                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You cannot hold that much");
                                break;
                            }

                            if (localArgs.Amount > 0)
                            {
                                localClient.Aisling.GoldPoints -= (uint)localArgs.Amount;
                                localClient.Aisling.Exchange.Gold = (uint)localArgs.Amount;
                                localClient.SendAttributes(StatUpdateType.ExpGold);
                                localClient.Aisling.Client.SendExchangeSetGold(false, localClient.Aisling.Exchange.Gold);
                                aisling.Client.SendExchangeSetGold(true, localClient.Aisling.Exchange.Gold);
                            }

                            break;
                        }
                }
            }

            return default;
        }
    }

    /// <summary>
    /// 0x2D - Request Player Profile & Load Character Meta Data (Skills/Spells)
    /// </summary>
    public ValueTask OnSelfProfileRequest(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;
        if (client.Aisling.CantAttack) return default;
        var readyTime = DateTime.UtcNow;
        return readyTime.Subtract(client.LastSelfProfileRequest).TotalSeconds < 1 ? default : ExecuteHandler(client, InnerOnProfileRequest);

        static ValueTask InnerOnProfileRequest(IWorldClient localClient)
        {
            localClient.LastSelfProfileRequest = DateTime.UtcNow;
            localClient.SendSelfProfile();
            return default;
        }
    }

    /// <summary>
    /// 0x2E - Request Party Join
    /// </summary>
    public ValueTask OnGroupInvite(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;

        var args = PacketSerializer.Deserialize<GroupInviteArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnGroupRequest);

        ValueTask InnerOnGroupRequest(IWorldClient localClient, GroupInviteArgs localArgs)
        {
            if (localArgs.ClientGroupSwitch == ClientGroupSwitch.CreateGroupbox)
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, "This feature is not yet implemented!");
                return default;
            }

            var player = ObjectManager.GetObject<Aisling>(localClient.Aisling.Map, i => string.Equals(i.Username, localArgs.TargetName, StringComparison.CurrentCultureIgnoreCase)
                                                                            && i.WithinRangeOf(localClient.Aisling));

            if (player == null)
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{localArgs.TargetName} is nowhere to be found");
                return default;
            }

            if (player.PartyStatus != GroupStatus.AcceptingRequests)
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{ServerSetup.Instance.Config.GroupRequestDeclinedMsg.Replace("noname", player.Username)}");
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{localClient.Aisling.Username} tried to group you, but you're not accepting requests.");
                return default;
            }

            if (Party.AddPartyMember(localClient.Aisling, player))
            {
                localClient.Aisling.PartyStatus = GroupStatus.AcceptingRequests;
                if (localClient.Aisling.GroupParty != null && localClient.Aisling.GroupParty.PartyMembers.Values.Any(other => other.IsInvisible))
                    localClient.UpdateDisplay();
                return default;
            }

            if (localClient.Aisling.LeaderPrivileges)
                Party.RemovePartyMember(player);

            return default;
        }
    }

    /// <summary>
    /// 0x2F - Toggle Group
    /// </summary>
    public ValueTask OnToggleGroup(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        return !client.Aisling.LoggedIn ? default : ExecuteHandler(client, InnerOnToggleGroup);

        static ValueTask InnerOnToggleGroup(IWorldClient localClient)
        {
            var mode = localClient.Aisling.PartyStatus;

            mode = mode switch
            {
                GroupStatus.AcceptingRequests => GroupStatus.NotAcceptingRequests,
                GroupStatus.NotAcceptingRequests => GroupStatus.AcceptingRequests,
                _ => mode
            };

            localClient.Aisling.PartyStatus = mode;

            if (localClient.Aisling.PartyStatus == GroupStatus.NotAcceptingRequests)
            {
                if (localClient.Aisling.LeaderPrivileges)
                {
                    if (!ServerSetup.Instance.GlobalGroupCache.TryGetValue(localClient.Aisling.GroupId, out var group)) return default;
                    Party.DisbandParty(group);
                }

                Party.RemovePartyMember(localClient.Aisling);
                localClient.SendRefreshResponse();
            }
            else
                localClient.SendSelfProfile();

            return default;
        }
    }

    /// <summary>
    /// 0x30 - Swap Slot
    /// </summary>
    public ValueTask OnSwapSlot(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.IsRefreshing) return default;
        if (client.Aisling.IsDead()) return default;

        if (client.Aisling.Skulled)
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.SendCancelCasting();
            client.SendLocation();
            return default;
        }

        var args = PacketSerializer.Deserialize<SwapSlotArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnSwapSlot);

        static ValueTask InnerOnSwapSlot(IWorldClient localClient, SwapSlotArgs localArgs)
        {
            switch (localArgs.PanelType)
            {
                case PanelType.Inventory:
                    var itemSwap = localClient.Aisling.Inventory.TrySwap(localClient.Aisling.Client, localArgs.Slot1, localArgs.Slot2);
                    if (itemSwap is { Item1: false, Item2: 0 })
                        ServerSetup.EventsLogger($"{localClient.Aisling.Username} - Swap item issue");
                    break;
                case PanelType.SpellBook:
                    var spellSwap = localClient.Aisling.SpellBook.AttemptSwap(localClient.Aisling.Client, localArgs.Slot1, localArgs.Slot2);
                    if (!spellSwap)
                        ServerSetup.EventsLogger($"{localClient.Aisling.Username} - Swap item issue");
                    break;
                case PanelType.SkillBook:
                    var skillSwap = localClient.Aisling.SkillBook.AttemptSwap(localClient.Aisling.Client, localArgs.Slot1, localArgs.Slot2);
                    if (!skillSwap)
                        ServerSetup.EventsLogger($"{localClient.Aisling.Username} - Swap item issue");
                    break;
                case PanelType.Equipment:
                    break;
            }

            return default;
        }
    }

    /// <summary>
    /// 0x38 - Request Refresh
    /// </summary>
    public ValueTask OnRefreshRequest(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.IsRefreshing) return default;
        var readyTime = DateTime.UtcNow;
        return readyTime.Subtract(client.LastClientRefresh).TotalSeconds < 0.4 ? default : ExecuteHandler(client, InnerOnRefreshRequest);

        static ValueTask InnerOnRefreshRequest(IWorldClient localClient)
        {
            localClient.ClientRefreshed();
            return default;
        }
    }

    /// <summary>
    /// 0x39 - Request Pursuit
    /// </summary>
    public ValueTask OnMenuInteraction(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<MenuInteractionArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnPursuitRequest);

        static ValueTask InnerOnPursuitRequest(IWorldClient localClient, MenuInteractionArgs localArgs)
        {
            try
            {
                ServerSetup.Instance.GlobalMundaneCache.TryGetValue(localArgs.EntityId, out var npc);
                if (npc == null) return default;

                var script = npc.Scripts.FirstOrDefault();

                if (localArgs.Slot is not null && localArgs.Slot != 0)
                {
                    var slotToString = localArgs.Slot.ToString();
                    script.Value?.OnResponse(localClient.Aisling.Client, localArgs.PursuitId, slotToString);
                    return default;
                }

                script.Value?.OnResponse(localClient.Aisling.Client, localArgs.PursuitId, localArgs.Args?[0]);
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(new Exception($"NPC Issue: {localClient.RemoteIp} sending:\n {e}"));
            }

            return default;
        }
    }

    /// <summary>
    /// 0x3A - Mundane Input Response
    /// </summary>
    public ValueTask OnDialogInteraction(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<DialogInteractionArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnDialogResponse);

        static ValueTask InnerOnDialogResponse(IWorldClient localClient, DialogInteractionArgs localArgs)
        {
            if (localArgs.DialogId == 0 && localArgs.PursuitId == ushort.MaxValue)
            {
                localClient.CloseDialog();
                return default;
            }

            ServerSetup.Instance.GlobalMundaneCache.TryGetValue(localArgs.EntityId, out var npc);
            if (npc == null) return default;

            if (localArgs.EntityId is > 0 and < uint.MaxValue)
            {
                var script = npc.Scripts.FirstOrDefault();
                script.Value?.OnResponse(localClient.Aisling.Client, localArgs.DialogId, (localArgs.Args?[0]));

                return default;
            }

            var result = (DialogResult)localArgs.DialogId;

            if (localArgs.PursuitId == ushort.MaxValue)
            {
                var pursuitScript = npc.Scripts.FirstOrDefault();

                switch (result)
                {
                    case DialogResult.Previous:
                        pursuitScript.Value?.OnBack(localClient.Aisling);
                        break;
                    case DialogResult.Next:
                        pursuitScript.Value?.OnNext(localClient.Aisling);
                        break;
                    case DialogResult.Close:
                        pursuitScript.Value?.OnClose(localClient.Aisling);
                        break;
                }
            }
            else
            {
                localClient.DlgSession?.Callback?.Invoke(localClient.Aisling.Client, localArgs.DialogId, localArgs.Args?[0]);
            }

            return default;
        }
    }

    /// <summary>
    /// 0x3B - Request Boards & Mailboxes
    /// </summary>
    public ValueTask OnBoardInteraction(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<BoardInteractionArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnBoardRequest);

        ValueTask InnerOnBoardRequest(IWorldClient localClient, BoardInteractionArgs localArgs)
        {
            switch (localArgs.BoardRequestType)
            {
                case BoardRequestType.BoardList:
                    {
                        // Sends Personal Mailbox - Delayed Population
                        localClient.SendMailBox();
                        break;
                    }
                case BoardRequestType.ViewBoard:
                    {
                        if (localArgs.BoardId == null) return default;
                        var boardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue((ushort)localArgs.BoardId, out var board);
                        if (boardFound)
                            localClient.SendBoard(board);
                        break;
                    }
                case BoardRequestType.ViewPost:
                    {
                        if (localArgs.BoardId == null) return default;
                        if (localArgs.BoardId == localClient.Aisling.QuestManager.MailBoxNumber)
                        {
                            var post = localClient.Aisling.PersonalLetters.Values.FirstOrDefault(p => p.PostId == localArgs.PostId);

                            // If null, check to see if there is a previous post first
                            if (post == null)
                            {
                                var postId = localArgs.PostId - 1;
                                post = localClient.Aisling.PersonalLetters.Values.FirstOrDefault(p => p.PostId == postId);
                            }

                            // If still null, display an error and exit
                            if (post == null)
                            {
                                localClient.SendBoardResponse(BoardOrResponseType.PublicPost, "There is nothing more to read", false);
                                break;
                            }

                            var prevEnabled = post.PostId > 0;
                            localClient.SendPost(post, true, prevEnabled);
                            break;
                        }

                        var boardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue((ushort)localArgs.BoardId, out var board);
                        if (boardFound)
                        {
                            var post = board.Posts.Values.FirstOrDefault(p => p.PostId == localArgs.PostId);

                            // If null, check to see if there is a previous post first
                            if (post == null)
                            {
                                var postId = localArgs.PostId - 1;
                                post = board?.Posts.Values.FirstOrDefault(p => p.PostId == postId);
                            }

                            // If still null, display an error and exit
                            if (post == null)
                            {
                                localClient.SendBoardResponse(BoardOrResponseType.PublicPost, "There is nothing more to read", false);
                                break;
                            }

                            var prevEnabled = post.PostId > 0;
                            localClient.SendPost(post, false, prevEnabled);
                        }

                        break;
                    }
                case BoardRequestType.SendMail:
                    {
                        var receiver = AislingStorage.CheckPassword(localArgs.To);
                        if (receiver.Result == null)
                        {
                            localClient.SendBoardResponse(BoardOrResponseType.SubmitPostResponse, "User does not exist.", false);
                            break;
                        }
                        var board = AislingStorage.ObtainMailboxId(receiver.Result.Serial);
                        var posts = AislingStorage.ObtainPosts(board.BoardId);
                        var postIdList = posts.Select(post => (int)post.PostId).ToList();
                        var postId = Enumerable.Range(1, 128).Except(postIdList).FirstOrDefault();
                        var np = new PostTemplate
                        {
                            PostId = (short)postId,
                            Highlighted = false,
                            DatePosted = DateTime.UtcNow,
                            Owner = localArgs.To,
                            Sender = client.Aisling.Username,
                            ReadPost = false,
                            SubjectLine = localArgs.Subject,
                            Message = localArgs.Message
                        };

                        AislingStorage.SendPost(np, board.BoardId);
                        localClient.SendBoardResponse(BoardOrResponseType.SubmitPostResponse, "Message Sent!", true);
                        break;
                    }
                case BoardRequestType.NewPost:
                    {
                        if (localArgs.BoardId == null) return default;
                        var boardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue((ushort)localArgs.BoardId, out var board);
                        if (boardFound)
                        {
                            var postIdList = board.Posts.Values.Select(post => (int)post.PostId).ToList();
                            var postId = Enumerable.Range(1, 128).Except(postIdList).FirstOrDefault();
                            var np = new PostTemplate
                            {
                                PostId = (short)postId,
                                Highlighted = false,
                                DatePosted = DateTime.UtcNow,
                                Owner = client.Aisling.Username,
                                Sender = client.Aisling.Username,
                                ReadPost = false,
                                SubjectLine = localArgs.Subject,
                                Message = localArgs.Message
                            };

                            board.Posts.TryAdd((short)postId, np);
                            AislingStorage.SendPost(np, board.BoardId);
                            localClient.SendBoardResponse(BoardOrResponseType.SubmitPostResponse, "Message Sent!", true);
                        }

                        break;
                    }
                case BoardRequestType.Delete:
                    {
                        if (localArgs.BoardId == null) return default;
                        if (localArgs.BoardId == localClient.Aisling.QuestManager.MailBoxNumber)
                        {
                            try
                            {
                                var postFound = localClient.Aisling.PersonalLetters.TryGetValue((short)localArgs.PostId!, out var post);
                                if (!postFound)
                                {
                                    localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Letter not found!", false);
                                    break;
                                }

                                BoardPostStorage.DeletePost(post, (ushort)client.Aisling.QuestManager.MailBoxNumber);
                                localClient.Aisling.PersonalLetters.TryRemove((short)localArgs.PostId!, out _);
                                localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Letter set on fire", true);
                            }
                            catch
                            {
                                localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Failed!", false);
                            }

                            break;
                        }

                        var boardFound = ServerSetup.Instance.GlobalBoardPostCache.TryGetValue((ushort)localArgs.BoardId, out var board);
                        if (!boardFound)
                        {
                            localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Failed!", false);
                            break;
                        }

                        try
                        {
                            var postFound = board.Posts.TryGetValue((short)localArgs.PostId!, out var post);
                            if (!postFound)
                            {
                                localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Post not found!", false);
                                break;
                            }

                            if (board.BoardId == client.Aisling.QuestManager.MailBoxNumber)
                            {
                                BoardPostStorage.DeletePost(post, (ushort)client.Aisling.QuestManager.MailBoxNumber);
                                board.Posts.TryRemove((short)localArgs.PostId!, out _);
                                localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Letter set on fire", true);
                                break;
                            }

                            if (string.Equals(post.Owner, client.Aisling.Username, StringComparison.InvariantCultureIgnoreCase))
                            {
                                BoardPostStorage.DeletePost(post, board.BoardId);
                                board.Posts.TryRemove((short)localArgs.PostId!, out _);
                                localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Post removed", true);
                                break;
                            }

                            if (localClient.Aisling.GameMaster)
                            {
                                BoardPostStorage.DeletePost(post, board.BoardId);
                                board.Posts.TryRemove((short)localArgs.PostId!, out _);
                                localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "GM Delete Used", true);
                                break;
                            }

                            localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "You do not have permission", false);
                        }
                        catch
                        {
                            localClient.SendBoardResponse(BoardOrResponseType.DeletePostResponse, "Failed!", false);
                        }

                        break;
                    }
                case BoardRequestType.Highlight:
                    {
                        //if (board == null) break;
                        if (!localClient.Aisling.GameMaster)
                        {
                            localClient.SendBoardResponse(BoardOrResponseType.HighlightPostResponse, "You do not have permission", false);
                            //break;
                        }

                        //////you cant highlight mail messages
                        //if (board.IsMail) break;

                        //foreach (var ind in board.Posts.Where(ind => ind.PostId == localArgs.PostId))
                        //{
                        //    if (ind.HighLighted)
                        //    {
                        //        ind.HighLighted = false;
                        //        client.SendServerMessage(ServerMessageType.ActiveMessage, $"Removed Highlight: {ind.Subject}");
                        //    }
                        //    else
                        //    {
                        //        ind.HighLighted = true;
                        //        client.SendServerMessage(ServerMessageType.ActiveMessage, $"Highlighted: {ind.Subject}");
                        //    }
                        //}

                        //localClient.SendBoardResponse(BoardOrResponseType.HighlightPostResponse, "Highlight Succeeded", true);

                        break;
                    }
            }

            return default;
        }
    }

    /// <summary>
    /// 0x3E - Skill Use
    /// </summary>
    public ValueTask OnSkillUse(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead() || client.Aisling.Skulled) return default;
        if (client.Aisling.CantAttack)
        {
            client.SendLocation();
            return default;
        }

        if (client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.CantUseAbilities))
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        var args = PacketSerializer.Deserialize<SkillUseArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnUseSkill);

        static ValueTask InnerOnUseSkill(IWorldClient localClient, SkillUseArgs localArgs)
        {
            if (localArgs.SourceSlot is 0) return default;
            var skill = localClient.Aisling.SkillBook.GetSkills(i => i.Slot == localArgs.SourceSlot).FirstOrDefault();
            if (skill == null)
            {
                localClient.Aisling.SkillBook = new SkillBook();
                localClient.LoadSkillBook();
                return default;
            }

            if (skill.Template == null || skill.Scripts == null) return default;

            if (!skill.CanUse()) return default;
            if (skill.InUse) return default;

            skill.InUse = true;

            var script = skill.Scripts.Values.FirstOrDefault();
            script?.OnUse(localClient.Aisling);
            skill.CurrentCooldown = skill.Template.Cooldown;
            localClient.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
            skill.LastUsedSkill = DateTime.UtcNow;
            script?.OnCleanup();

            skill.InUse = false;
            return default;
        }
    }

    /// <summary>
    /// 0x3F - World Map Click
    /// </summary>
    public ValueTask OnWorldMapClick(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<WorldMapClickArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnWorldMapClick);

        static ValueTask InnerOnWorldMapClick(IWorldClient localClient, WorldMapClickArgs localArgs)
        {
            ServerSetup.Instance.GlobalWorldMapTemplateCache.TryGetValue(localClient.Aisling.World, out var worldMap);

            //if player is not in a world map, return
            if (worldMap == null) return default;

            localClient.Aisling.Client.PendingNode = worldMap.Portals.Find(i => i.Destination.AreaID == localArgs.MapId);

            if (!localClient.Aisling.Client.MapOpen) return default;
            var selectedPortalNode = localClient.Aisling.Client.PendingNode;
            if (selectedPortalNode == null) return default;
            localClient.Aisling.Client.MapOpen = false;

            for (var i = 0; i < 1; i++)
            {
                localClient.Aisling.CurrentMapId = selectedPortalNode.Destination.AreaID;
                localClient.Aisling.Pos = new Vector2(selectedPortalNode.Destination.Location.X, selectedPortalNode.Destination.Location.Y);
                localClient.Aisling.X = selectedPortalNode.Destination.Location.X;
                localClient.Aisling.Y = selectedPortalNode.Destination.Location.Y;
                localClient.Aisling.Client.TransitionToMap(selectedPortalNode.Destination.AreaID, selectedPortalNode.Destination.Location);
            }

            localClient.Aisling.Client.PendingNode = null;
            return default;
        }
    }

    /// <summary>
    /// 0x43 - Client Click (map, player, npc, monster) - F1 Button
    /// </summary>
    public ValueTask OnClick(IWorldClient client, in Packet clientPacket)
    {
        if (!client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<ClickArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnClick);

        ValueTask InnerOnClick(IWorldClient localClient, ClickArgs localArgs)
        {
            if (localArgs.TargetPoint != null)
                localClient.Aisling.Map.Script.Item2.OnMapClick(localClient.Aisling.Client, localArgs.TargetPoint.X, localArgs.TargetPoint.Y);

            if (localArgs.TargetId == uint.MaxValue &&
                ServerSetup.Instance.GlobalMundaneTemplateCache.TryGetValue(ServerSetup.Instance.Config
                    .HelperMenuTemplateKey, out var value))
            {
                if (localClient.Aisling.CantCast || localClient.Aisling.CantAttack) return default;

                var helper = new UserHelper(this, new Mundane
                {
                    Serial = uint.MaxValue,
                    Template = value
                });

                helper.OnClick(localClient.Aisling.Client, (uint)localArgs.TargetId);
                return default;
            }

            var monsterCheck = ObjectManager.GetObject<Monster>(localClient.Aisling.Map, i => i.Serial == localArgs.TargetId);
            var npcCheck = ServerSetup.Instance.GlobalMundaneCache.Where(i => i.Key == localArgs.TargetId);

            if (monsterCheck != null)
            {
                if (monsterCheck.Template?.ScriptName == null) return default;
                var scripts = monsterCheck.Scripts?.Values;
                if (scripts == null) return default;
                foreach (var script in scripts)
                    script.OnClick(localClient.Aisling.Client);
                return default;
            }

            foreach (var (_, npc) in npcCheck)
            {
                if (npc?.Template?.ScriptKey == null) continue;
                var scripts = npc.Scripts?.Values;
                if (scripts == null || localArgs.TargetId == null) return default;
                foreach (var script in scripts)
                    script.OnClick(localClient.Aisling.Client, (uint)localArgs.TargetId);
                return default;
            }

            var obj = ObjectManager.GetObject(localClient.Aisling.Map, i => i.Serial == localArgs.TargetId, ObjectManager.Get.Aislings);
            switch (obj)
            {
                case null:
                    return default;
                case Aisling aisling:
                    localClient.SendProfile(aisling);
                    break;
            }

            return default;
        }
    }

    /// <summary>
    /// 0x44 - Unequip Item
    /// </summary>
    public ValueTask OnUnequip(IWorldClient client, in Packet clientPacket)
    {
        if (!client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<UnequipArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnUnequip);

        static ValueTask InnerOnUnequip(IWorldClient localClient, UnequipArgs localArgs)
        {
            if (localClient.Aisling.Inventory.IsFull)
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYour inventory is full");
                return default;
            }

            if (localClient.Aisling.EquipmentManager.Equipment.ContainsKey((int)localArgs.EquipmentSlot))
                localClient.Aisling.EquipmentManager?.RemoveFromExistingSlot((int)localArgs.EquipmentSlot);

            return default;
        }
    }

    /// <summary>
    /// 0x45 - Client Ping (Heartbeat)
    /// </summary>
    public override ValueTask OnHeartBeatAsync(IWorldClient client, in Packet clientPacket)
    {
        var args = PacketSerializer.Deserialize<HeartBeatArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnHeartBeat);

        static ValueTask InnerOnHeartBeat(IWorldClient localClient, HeartBeatArgs localArgs)
        {
            if (localArgs.First != 20 || localArgs.Second != 32) return default;
            localClient.Latency.Stop();

            return default;
        }
    }

    /// <summary>
    /// 0x47 - Stat Raised
    /// </summary>
    public ValueTask OnRaiseStat(IWorldClient client, in Packet clientPacket)
    {
        if (!client.Aisling.LoggedIn) return default;
        if (client.IsRefreshing) return default;
        var args = PacketSerializer.Deserialize<RaiseStatArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnRaiseStat);

        static ValueTask InnerOnRaiseStat(IWorldClient localClient, RaiseStatArgs localArgs)
        {
            switch (localClient.Aisling.StatPoints)
            {
                case 0:
                    localClient.SendServerMessage(ServerMessageType.OrangeBar1, "You do not have any stat points remaining.");
                    return default;
                case > 0:
                    switch (localArgs.Stat)
                    {
                        case Stat.STR:
                            if (localClient.Aisling._Str >= 500)
                            {
                                localClient.SendServerMessage(ServerMessageType.OrangeBar1, "Maxed strength!");
                                return default;
                            }

                            localClient.Aisling._Str++;
                            localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"Base strength now {localClient.Aisling._Str}");
                            break;
                        case Stat.INT:
                            if (localClient.Aisling._Int >= 500)
                            {
                                localClient.SendServerMessage(ServerMessageType.OrangeBar1, "Maxed intelligence!");
                                return default;
                            }

                            localClient.Aisling._Int++;
                            localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"Base intelligence now {localClient.Aisling._Int}");
                            break;
                        case Stat.WIS:
                            if (localClient.Aisling._Wis >= 500)
                            {
                                localClient.SendServerMessage(ServerMessageType.OrangeBar1, "Maxed wisdom!");
                                return default;
                            }

                            localClient.Aisling._Wis++;
                            localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"Base wisdom now {localClient.Aisling._Wis}");
                            break;
                        case Stat.CON:
                            if (localClient.Aisling._Con >= 500)
                            {
                                localClient.SendServerMessage(ServerMessageType.OrangeBar1, "Maxed constitution!");
                                return default;
                            }

                            localClient.Aisling._Con++;
                            localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"Base constitution now {localClient.Aisling._Con}");
                            break;
                        case Stat.DEX:
                            if (localClient.Aisling._Dex >= 500)
                            {
                                localClient.SendServerMessage(ServerMessageType.OrangeBar1, "Maxed dexterity!");
                                return default;
                            }

                            localClient.Aisling._Dex++;
                            localClient.SendServerMessage(ServerMessageType.ActiveMessage, $"Base dexterity now {localClient.Aisling._Dex}");
                            break;
                    }

                    if (!localClient.Aisling.GameMaster)
                        localClient.Aisling.StatPoints--;

                    if (localClient.Aisling.StatPoints < 0)
                        localClient.Aisling.StatPoints = 0;

                    localClient.SendAttributes(StatUpdateType.Full);
                    break;
            }

            return default;
        }
    }

    /// <summary>
    /// 0x4A - Client Exchange
    /// </summary>
    public ValueTask OnExchangeInteraction(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;
        var args = PacketSerializer.Deserialize<ExchangeInteractionArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnExchange);

        ValueTask InnerOnExchange(IWorldClient localClient, ExchangeInteractionArgs localArgs)
        {
            var otherPlayer = ObjectManager.GetObject<Aisling>(client.Aisling.Map, i => i.Serial.Equals(localArgs.OtherPlayerId));
            var localPlayer = localClient.Aisling;
            if (localPlayer == null || otherPlayer == null) return default;
            if (!localPlayer.WithinRangeOf(otherPlayer)) return default;

            switch (localArgs.ExchangeRequestType)
            {
                case ExchangeRequestType.StartExchange:
                    // Not possible to start an exchange directly
                    break;
                case ExchangeRequestType.AddItem:
                    if (localPlayer.ThrewHealingPot)
                    {
                        localPlayer.ThrewHealingPot = false;
                        break;
                    }

                    if (localArgs.SourceSlot != null)
                    {
                        var item = localPlayer.Inventory.Items[(int)localArgs.SourceSlot];
                        if (!item.Template.Flags.FlagIsSet(ItemFlags.Tradeable))
                        {
                            localClient.SendServerMessage(ServerMessageType.ActiveMessage, "You cannot trade that item");
                            break;
                        }

                        if (localPlayer.Exchange == null) break;
                        if (otherPlayer.Exchange == null) break;
                        if (localPlayer.Exchange.Trader != otherPlayer) break;
                        if (otherPlayer.Exchange.Trader != localPlayer) break;
                        if (localPlayer.Exchange.Confirmed) break;
                        if (item?.Template == null) break;

                        if (otherPlayer.CurrentWeight + item.Template.CarryWeight < otherPlayer.MaximumWeight)
                        {
                            localPlayer.Inventory.RemoveFromInventory(localPlayer.Client, item);
                            localPlayer.Exchange.Items.Add(item);
                            localPlayer.Exchange.Weight += item.Template.CarryWeight;
                            localPlayer.Client.SendExchangeAddItem(false, (byte)localPlayer.Exchange.Items.Count, item);
                            otherPlayer.Client.SendExchangeAddItem(true, (byte)localPlayer.Exchange.Items.Count, item);
                            break;
                        }

                        localClient.SendServerMessage(ServerMessageType.ActiveMessage, "They can't seem to lift that. The trade has been cancelled.");
                        otherPlayer.Client.SendServerMessage(ServerMessageType.ActiveMessage, "That item seems to be too heavy for you, trade has been cancelled.");
                    }

                    localPlayer.CancelExchange();

                    break;
                case ExchangeRequestType.AddStackableItem:
                    break;
                case ExchangeRequestType.SetGold:
                    if (localPlayer.Exchange == null) break;
                    if (otherPlayer.Exchange == null) break;
                    if (localPlayer.Exchange.Trader != otherPlayer) break;
                    if (otherPlayer.Exchange.Trader != localPlayer) break;
                    if (localPlayer.Exchange.Confirmed) break;
                    if (localPlayer.Exchange.Gold != 0) break;

                    var gold = localArgs.GoldAmount;
                    if (gold is null or <= 0) gold = 0;

                    if ((uint)gold > localPlayer.GoldPoints)
                    {
                        localClient.SendServerMessage(ServerMessageType.ActiveMessage, "You don't have that much to give");
                        break;
                    }

                    if (otherPlayer.GoldPoints + (uint)gold > ServerSetup.Instance.Config.MaxCarryGold)
                    {
                        localClient.SendServerMessage(ServerMessageType.ActiveMessage, "Player cannot hold that amount");
                        otherPlayer.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You cannot hold that much");
                        break;
                    }

                    if (gold > 0)
                    {
                        localPlayer.GoldPoints -= (uint)gold;
                        localPlayer.Exchange.Gold = (uint)gold;
                        localClient.SendAttributes(StatUpdateType.ExpGold);
                        localPlayer.Client.SendExchangeSetGold(false, localPlayer.Exchange.Gold);
                        otherPlayer.Client.SendExchangeSetGold(true, localPlayer.Exchange.Gold);
                    }

                    break;
                case ExchangeRequestType.Cancel:
                    localPlayer.CancelExchange();
                    break;
                case ExchangeRequestType.Accept:
                    if (localPlayer.Exchange == null) break;
                    if (otherPlayer.Exchange == null) break;
                    if (localPlayer.Exchange.Trader != otherPlayer) break;
                    if (otherPlayer.Exchange.Trader != localPlayer) break;

                    localPlayer.Exchange.Confirmed = true;

                    if (localPlayer.Exchange.Confirmed && otherPlayer.Exchange.Confirmed)
                    {
                        localPlayer.Client.SendExchangeAccepted(false);
                        otherPlayer.Client.SendExchangeAccepted(false);
                    }
                    else
                    {
                        localPlayer.Client.SendExchangeAccepted(localPlayer.Exchange.Confirmed);
                        otherPlayer.Client.SendExchangeAccepted(localPlayer.Exchange.Confirmed);
                    }

                    if (otherPlayer.Exchange.Confirmed)
                        localPlayer.FinishExchange();

                    break;
            }

            return default;
        }
    }

    /// <summary>
    /// 0x4D - Begin Casting
    /// </summary>
    public ValueTask OnBeginChant(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;

        if (client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.CantUseItems))
        {
            client.SendServerMessage(ServerMessageType.OrangeBar1, "You cannot do that.");
            return default;
        }

        var args = PacketSerializer.Deserialize<BeginChantArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnBeginChant);

        static ValueTask InnerOnBeginChant(IWorldClient localClient, BeginChantArgs localArgs)
        {
            localClient.Aisling.IsCastingSpell = true;
            if (localArgs.CastLineCount <= 0) return default;

            localClient.SpellCastInfo ??= new CastInfo
            {
                SpellLines = Math.Clamp(localArgs.CastLineCount, (byte)0, (byte)9),
                Started = DateTime.UtcNow
            };

            return default;
        }
    }

    /// <summary>
    /// 0x4E - Casting
    /// </summary>
    public ValueTask OnChant(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        if (client.Aisling.IsDead()) return default;
        var args = PacketSerializer.Deserialize<ChantArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnChant);

        static ValueTask InnerOnChant(IWorldClient localClient, ChantArgs localArgs)
        {
            localClient.Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendPublicMessage(localClient.Aisling.Serial, PublicMessageType.Chant, localArgs.ChantMessage));
            return default;
        }
    }

    /// <summary>
    /// 0x4F - Player Portrait & Profile Message
    /// </summary>
    public ValueTask OnEditableProfile(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<EditableProfileArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnProfile);

        static ValueTask InnerOnProfile(IWorldClient localClient, EditableProfileArgs localArgs)
        {
            localClient.Aisling.PictureData = localArgs.PortraitData;
            localClient.Aisling.ProfileMessage = localArgs.ProfileMessage;

            return default;
        }
    }

    /// <summary>
    /// 0x79 - Player Social Status
    /// </summary>
    public ValueTask OnSocialStatus(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<SocialStatusArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnSocialStatus);

        static ValueTask InnerOnSocialStatus(IWorldClient localClient, SocialStatusArgs localArgs)
        {
            localClient.Aisling.ActiveStatus = (ActivityStatus)localArgs.SocialStatus;

            return default;
        }
    }

    /// <summary>
    /// 0x7B - Request Metafile
    /// </summary>
    public ValueTask OnMetaDataRequest(IWorldClient client, in Packet clientPacket)
    {
        if (client?.Aisling == null) return default;
        if (!client.Aisling.LoggedIn) return default;
        var args = PacketSerializer.Deserialize<MetaDataRequestArgs>(in clientPacket);
        return ExecuteHandler(client, args, InnerOnMetaDataRequest);

        ValueTask InnerOnMetaDataRequest(IWorldClient localClient, MetaDataRequestArgs localArgs)
        {
            try
            {
                switch (localArgs.MetaDataRequestType)
                {
                    case MetaDataRequestType.DataByName:
                        if (localArgs.Name is null) return default;
                        if (!localArgs.Name.Contains("Class"))
                        {
                            localClient.SendMetaData(localArgs.MetaDataRequestType, new MetafileManager(), localArgs.Name);
                            break;
                        }

                        var skillSet = DecideOnSkillsToPull(client);
                        if (skillSet.IsNullOrEmpty()) break;
                        localClient.SendMetaData(MetaDataRequestType.DataByName, new MetafileManager(), skillSet);
                        break;
                    case MetaDataRequestType.AllCheckSums:
                        localClient.SendMetaData(MetaDataRequestType.AllCheckSums, new MetafileManager());
                        break;
                }
            }
            catch
            {
                // Ignore
            }

            return default;
        }
    }

    private static string DecideOnSkillsToPull(IWorldClient client)
    {
        return client.Aisling == null ? null : SkillMap.GetValueOrDefault((client.Aisling.Race, client.Aisling.Path, client.Aisling.PastClass));
    }

    #endregion

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(IWorldClient client, in Packet packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[packet.OpCode];

        if (client.Aisling is not null && IsManualAction((ClientOpCode)opCode))
            client.Aisling.AislingTracker = DateTime.UtcNow;

        // ToDo: Packet logging
        //ServerSetup.PacketLogger($"{packet.OpCode}");

        try
        {
            if (handler is not null) return handler(client, in packet);
            ServerSetup.PacketLogger("//////////////// Handled World Server Unknown Packet ////////////////", LogLevel.Error);
            ServerSetup.PacketLogger($"{opCode} from {client.RemoteIp}", LogLevel.Error);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(new Exception($"Unknown packet {opCode} from {client.RemoteIp} on WorldServer \n {ex}"));
        }

        return default;
    }

    protected override void IndexHandlers()
    {
        base.IndexHandlers();

        ClientHandlers[(byte)ClientOpCode.MapDataRequest] = OnMapDataRequest; // 0x05
        ClientHandlers[(byte)ClientOpCode.ClientWalk] = OnClientWalk; // 0x06
        ClientHandlers[(byte)ClientOpCode.Pickup] = OnPickup; // 0x07
        ClientHandlers[(byte)ClientOpCode.ItemDrop] = OnItemDrop; // 0x08
        ClientHandlers[(byte)ClientOpCode.ExitRequest] = OnExitRequest; // 0x0B
        ClientHandlers[(byte)ClientOpCode.DisplayEntityRequest] = OnDisplayEntityRequest; // 0x0C
        ClientHandlers[(byte)ClientOpCode.Ignore] = OnIgnore; // 0x0D
        ClientHandlers[(byte)ClientOpCode.PublicMessage] = OnPublicMessage; // 0x0E
        ClientHandlers[(byte)ClientOpCode.SpellUse] = OnSpellUse; // 0x0F
        ClientHandlers[(byte)ClientOpCode.ClientRedirected] = OnClientRedirected; // 0x10
        ClientHandlers[(byte)ClientOpCode.Turn] = OnTurn; // 0x11
        ClientHandlers[(byte)ClientOpCode.Spacebar] = OnSpacebar; // 0x13
        ClientHandlers[(byte)ClientOpCode.WorldListRequest] = OnWorldListRequest; // 0x18
        ClientHandlers[(byte)ClientOpCode.Whisper] = OnWhisper; // 0x19
        ClientHandlers[(byte)ClientOpCode.OptionToggle] = OnOptionToggle; // 0x1B
        ClientHandlers[(byte)ClientOpCode.ItemUse] = OnItemUse; // 0x1C
        ClientHandlers[(byte)ClientOpCode.Emote] = OnEmote; // 0x1D
        ClientHandlers[(byte)ClientOpCode.GoldDrop] = OnGoldDrop; // 0x24
        ClientHandlers[(byte)ClientOpCode.ItemDroppedOnCreature] = OnItemDroppedOnCreature; // 0x29
        ClientHandlers[(byte)ClientOpCode.GoldDroppedOnCreature] = OnGoldDroppedOnCreature; // 0x2A
        ClientHandlers[(byte)ClientOpCode.SelfProfileRequest] = OnSelfProfileRequest; // 0x2D
        ClientHandlers[(byte)ClientOpCode.GroupInvite] = OnGroupInvite; // 0x2E
        ClientHandlers[(byte)ClientOpCode.ToggleGroup] = OnToggleGroup; // 0x2F
        ClientHandlers[(byte)ClientOpCode.SwapSlot] = OnSwapSlot; // 0x30
        ClientHandlers[(byte)ClientOpCode.RefreshRequest] = OnRefreshRequest; // 0x38
        ClientHandlers[(byte)ClientOpCode.MenuInteraction] = OnMenuInteraction; // 0x39
        ClientHandlers[(byte)ClientOpCode.DialogInteraction] = OnDialogInteraction; // 0x3A
        ClientHandlers[(byte)ClientOpCode.BoardInteraction] = OnBoardInteraction; // 0x3B
        ClientHandlers[(byte)ClientOpCode.SkillUse] = OnSkillUse; // 0x3E
        ClientHandlers[(byte)ClientOpCode.WorldMapClick] = OnWorldMapClick; // 0x3F
        ClientHandlers[(byte)ClientOpCode.Click] = OnClick; // 0x43
        ClientHandlers[(byte)ClientOpCode.Unequip] = OnUnequip; // 0x44
        ClientHandlers[(byte)ClientOpCode.HeartBeat] = OnHeartBeatAsync; // 0x45
        ClientHandlers[(byte)ClientOpCode.RaiseStat] = OnRaiseStat; // 0x47
        ClientHandlers[(byte)ClientOpCode.ExchangeInteraction] = OnExchangeInteraction; // 0x4A
        ClientHandlers[(byte)ClientOpCode.BeginChant] = OnBeginChant; // 0x4D
        ClientHandlers[(byte)ClientOpCode.Chant] = OnChant; // 0x4E
        ClientHandlers[(byte)ClientOpCode.EditableProfile] = OnEditableProfile; // 0x4F
        ClientHandlers[(byte)ClientOpCode.SocialStatus] = OnSocialStatus; // 0x79
        ClientHandlers[(byte)ClientOpCode.MetaDataRequest] = OnMetaDataRequest; // 0x7B
    }

    protected override void OnConnected(Socket clientSocket)
    {
        ServerSetup.ConnectionLogger($"World connection from {clientSocket.RemoteEndPoint as IPEndPoint}");

        if (clientSocket.RemoteEndPoint is not IPEndPoint ip)
        {
            ServerSetup.ConnectionLogger("Socket not a valid endpoint");
            return;
        }

        var ipAddress = ip.Address;
        var client = _clientProvider.CreateClient(clientSocket);
        client.OnDisconnected += OnDisconnect;
        var safe = false;

        foreach (var _ in ServerSetup.Instance.GlobalKnownGoodActorsCache.Values.Where(savedIp => savedIp == ipAddress.ToString()))
            safe = true;

        if (!safe)
        {
            var badActor = ClientOnBlackList(ipAddress.ToString());

            if (badActor)
            {
                try
                {
                    client.Disconnect();
                    ServerSetup.ConnectionLogger($"Disconnected Bad Actor from {ip}");
                }
                catch
                {
                    // ignored
                }

                return;
            }
        }

        if (!ClientRegistry.TryAdd(client))
        {
            ServerSetup.ConnectionLogger("Two clients ended up with the same id - newest client disconnected");

            try
            {
                client.Disconnect();
            }
            catch
            {
                // ignored
            }

            return;
        }

        var lobbyCheck = ServerSetup.Instance.GlobalLobbyConnection.TryGetValue(ipAddress, out _);
        var loginCheck = ServerSetup.Instance.GlobalLoginConnection.TryGetValue(ipAddress, out _);

        if (!lobbyCheck || !loginCheck)
        {
            try
            {
                client.Disconnect();
            }
            catch
            {
                // ignored
            }

            ServerSetup.ConnectionLogger("---------World-Server---------");
            var comment = $"{ipAddress} has been blocked for violating security protocols through improper port access.";
            ServerSetup.ConnectionLogger(comment, LogLevel.Warning);
            ReportEndpoint(ipAddress.ToString(), comment);
            return;
        }

        ServerSetup.Instance.GlobalWorldConnection.TryAdd(ipAddress, ipAddress);
        client.BeginReceive();
    }

    private async void OnDisconnect(object sender, EventArgs e)
    {
        var client = (IWorldClient)sender!;
        var aisling = client.Aisling;

        if (aisling == null)
        {
            ClientRegistry.TryRemove(client.Id, out _);
            return;
        }

        if (aisling.Client.ExitConfirmed)
        {
            ServerSetup.ConnectionLogger($"{aisling.Username} either logged out or was removed from the server.");
            return;
        }

        try
        {
            // Close Popups
            client.CloseDialog();
            aisling.CancelExchange();

            // Exit Party
            if (aisling.GroupId != 0)
                Party.RemovePartyMember(aisling);

            // Set Timestamps
            aisling.LastLogged = DateTime.UtcNow;
            aisling.LoggedIn = false;
            aisling.Client.LastSave = DateTime.UtcNow;

            // Save
            await client.Save();

            // Cleanup
            aisling.Remove(true);
            ClientRegistry.TryRemove(client.Id, out _);
            ServerSetup.ConnectionLogger($"{aisling.Username} either logged out or was removed from the server.");
        }
        catch
        {
            // ignored
        }
    }

    private static bool ClientOnBlackList(string remoteIp)
    {
        if (remoteIp.IsNullOrEmpty()) return true;

        switch (remoteIp)
        {
            case "127.0.0.1":
            case InternalIP:
                return false;
        }

        try
        {
            var keyCode = ServerSetup.Instance.KeyCode;
            if (keyCode is null || keyCode.Length == 0)
            {
                ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
                return false;
            }

            // BLACKLIST check
            var request = new RestRequest("", Method.Get);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ipAddress", remoteIp);
            request.AddParameter("maxAgeInDays", "90");
            request.AddParameter("verbose", "");
            var response = ServerSetup.Instance.RestClient.Execute<Ipdb>(request);

            if (response.IsSuccessful)
            {
                var json = response.Content;

                if (json is null || json.Length == 0)
                {
                    ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, response is null or length is 0");
                    return false;
                }

                var ipdb = JsonConvert.DeserializeObject<Ipdb>(json!);
                var abuseConfidenceScore = ipdb?.Data?.AbuseConfidenceScore;
                var tor = ipdb?.Data?.IsTor;
                var usageType = ipdb?.Data?.UsageType;

                if (tor == true)
                {
                    ServerSetup.ConnectionLogger("---------World-Server---------");
                    ServerSetup.ConnectionLogger($"{remoteIp} is using tor and automatically blocked", LogLevel.Warning);
                    SentrySdk.CaptureMessage($"{remoteIp} has a confidence score of {abuseConfidenceScore}, and was using tor");
                    return true;
                }

                switch (usageType)
                {
                    case "Commercial":
                    case "Organization":
                    case "Government":
                    case "Military":
                    case "Content Delivery Network":
                    case "Data Center/Web Hosting/Transit":
                    case "Search Engine Spider":
                    case "Reserved":
                        {
                            ServerSetup.ConnectionLogger("---------World-Server---------");
                            ServerSetup.ConnectionLogger($"{remoteIp} was blocked due to being a {usageType} address", LogLevel.Warning);
                            SentrySdk.CaptureMessage($"{remoteIp} has a confidence score of {abuseConfidenceScore}, and was using a {usageType} address");
                            return true;
                        }
                    case "University/College/School":
                    case "Library":
                    case "Fixed Line ISP":
                    case "Mobile ISP":
                        break;
                }

                switch (abuseConfidenceScore)
                {
                    case >= 5:
                        ServerSetup.ConnectionLogger("---------World-Server---------");
                        var comment = $"{remoteIp} has been blocked due to a high risk assessment score of {abuseConfidenceScore}, indicating a recognized malicious entity.";
                        ServerSetup.ConnectionLogger(comment, LogLevel.Warning);
                        SentrySdk.CaptureMessage($"{remoteIp} has a confidence score of {abuseConfidenceScore}, is using tor: {tor}, and IP type: {usageType}");
                        ReportEndpoint(remoteIp, comment);
                        return true;
                    case >= 0:
                        return false;
                    case null:
                        // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                        ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, confidence score was null");
                        return false;
                }
            }
            else
            {
                // Can be null if there is an error in the API, don't want to punish players if its the APIs fault
                ServerSetup.ConnectionLogger($"{remoteIp} - API Issue, response was not successful");
                return false;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger("Unknown issue with IPDB, connections refused", LogLevel.Warning);
            ServerSetup.ConnectionLogger($"{ex}");
            SentrySdk.CaptureException(ex);
            return false;
        }

        return true;
    }

    private static void ReportEndpoint(string remoteIp, string comment)
    {
        var keyCode = ServerSetup.Instance.KeyCode;
        if (keyCode is null || keyCode.Length == 0)
        {
            ServerSetup.ConnectionLogger("Keycode not valid or not set within ServerConfig.json");
            return;
        }

        try
        {
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Key", keyCode);
            request.AddHeader("Accept", "application/json");
            request.AddParameter("ip", remoteIp);
            request.AddParameter("categories", "14, 15, 16, 21");
            request.AddParameter("comment", comment);
            var response = ServerSetup.Instance.RestReport.Execute(request);

            if (response.IsSuccessful) return;
            ServerSetup.ConnectionLogger($"Error reporting {remoteIp} : {comment}");
            SentrySdk.CaptureMessage($"Error reporting {remoteIp} : {comment}");
        }
        catch
        {
            // ignore
        }
    }

    private static bool IsManualAction(ClientOpCode opCode) => opCode switch
    {
        ClientOpCode.ClientWalk => true,
        ClientOpCode.Pickup => true,
        ClientOpCode.ItemDrop => true,
        ClientOpCode.ExitRequest => true,
        ClientOpCode.Ignore => true,
        ClientOpCode.PublicMessage => true,
        ClientOpCode.SpellUse => true,
        ClientOpCode.ClientRedirected => true,
        ClientOpCode.Turn => true,
        ClientOpCode.Spacebar => true,
        ClientOpCode.WorldListRequest => true,
        ClientOpCode.Whisper => true,
        ClientOpCode.OptionToggle => true,
        ClientOpCode.ItemUse => true,
        ClientOpCode.Emote => true,
        ClientOpCode.SetNotepad => true,
        ClientOpCode.GoldDrop => true,
        ClientOpCode.ItemDroppedOnCreature => true,
        ClientOpCode.GoldDroppedOnCreature => true,
        ClientOpCode.SelfProfileRequest => true,
        ClientOpCode.GroupInvite => true,
        ClientOpCode.ToggleGroup => true,
        ClientOpCode.SwapSlot => true,
        ClientOpCode.RefreshRequest => true,
        ClientOpCode.MenuInteraction => true,
        ClientOpCode.DialogInteraction => true,
        ClientOpCode.BoardInteraction => true,
        ClientOpCode.SkillUse => true,
        ClientOpCode.WorldMapClick => true,
        ClientOpCode.Click => true,
        ClientOpCode.Unequip => true,
        ClientOpCode.RaiseStat => true,
        ClientOpCode.ExchangeInteraction => true,
        ClientOpCode.BeginChant => true,
        ClientOpCode.Chant => true,
        ClientOpCode.EditableProfile => true,
        ClientOpCode.SocialStatus => true,
        _ => false
    };

    #endregion
}