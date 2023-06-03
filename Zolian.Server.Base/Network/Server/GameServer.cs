using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats;
using Darkages.Network.Formats.Models.ClientFormats;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.GameServer;
using Darkages.Network.GameServer.Components;
using Darkages.Object;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Systems;
using Darkages.Types;

using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

using ServiceStack;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Darkages.GameScripts.Mundanes.Generic;
using Darkages.Models;

namespace Darkages.Network.Server;

public class GameServer : NetworkServer<GameClient>
{
    public readonly ObjectService ObjectFactory = new();
    public readonly ObjectManager ObjectHandlers = new();
    private static Dictionary<(Race race, Class path, Class pastClass), string> _skillMap = new();
    private ConcurrentDictionary<Type, GameServerComponent> _serverComponents;
    private DateTime _fastGameTime;
    private DateTime _normalGameTime;
    private DateTime _slowGameTime;
    private DateTime _abilityGameTime;
    private TimeSpan _clientGameTimeSpan;
    private TimeSpan _abilityGameTimeSpan;
    public byte CurrentEncryptKey;
    private const int FastGameSpeed = 40;
    private const int NormalGameSpeed = 80;
    private const int SlowGameSpeed = 120;
    private const int AbilityGameSpeed = 500;

    public GameServer(int capacity)
    {
        SkillMapper();
        RegisterServerComponents();
    }

    private void RegisterServerComponents()
    {
        lock (ServerSetup.SyncLock)
        {
            _serverComponents = new ConcurrentDictionary<Type, GameServerComponent>
            {
                [typeof(InterestAndCommunityComponent)] = new InterestAndCommunityComponent(this),
                [typeof(MessageClearComponent)] = new MessageClearComponent(this),
                [typeof(MonolithComponent)] = new MonolithComponent(this),
                [typeof(MundaneComponent)] = new MundaneComponent(this),
                [typeof(ObjectComponent)] = new ObjectComponent(this),
                [typeof(PingComponent)] = new PingComponent(this),
                [typeof(PlayerRegenerationComponent)] = new PlayerRegenerationComponent(this),
                [typeof(PlayerSaveComponent)] = new PlayerSaveComponent(this),
                [typeof(MoonPhaseComponent)] = new MoonPhaseComponent(this)
            };

            ServerSetup.Logger($"Server Components Loaded: {_serverComponents.Count}");
        }
    }

    public override void Start(int port)
    {
        base.Start(port);
        ServerEncryptKey();

        try
        {
            ServerSetup.Instance.Running = true;

            lock (ServerSetup.SyncLock)
            {
                _ = UpdateComponentsFast();
                _ = UpdateObjectsNormal();
                _ = UpdateAreasSlow();
                _ = UpdateAbilityGameTimer();
                _ = NightlyServerRestart();
            }
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
            Crashes.TrackError(ex);
            ServerSetup.Instance.Running = false;
        }
    }

    #region Game Engine

    private static void SkillMapper()
    {
        _skillMap = new Dictionary<(Race race, Class path, Class pastClass), string>
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
    }

    private async Task UpdateComponentsFast()
    {
        _fastGameTime = DateTime.Now;

        while (ServerSetup.Instance.Running)
        {
            var gTimeConvert = DateTime.Now;
            var gameTime = gTimeConvert - _fastGameTime;

            UpdateComponents(gameTime);

            _fastGameTime += gameTime;

            await Task.Delay(FastGameSpeed).ConfigureAwait(false);
        }
    }

    private async Task UpdateObjectsNormal()
    {
        _normalGameTime = DateTime.Now;

        while (ServerSetup.Instance.Running)
        {
            var gTimeConvert = DateTime.Now;
            _clientGameTimeSpan = gTimeConvert - _normalGameTime;

            UpdateClients(_clientGameTimeSpan);
            UpdateMonsters(_clientGameTimeSpan);

            _normalGameTime += _clientGameTimeSpan;

            await Task.Delay(NormalGameSpeed).ConfigureAwait(false);
        }
    }

    private async Task UpdateAreasSlow()
    {
        _slowGameTime = DateTime.Now;

        while (ServerSetup.Instance.Running)
        {
            var gTimeConvert = DateTime.Now;
            var gameTime = gTimeConvert - _slowGameTime;

            UpdateMundanes(gameTime);
            UpdateMaps(gameTime);

            _slowGameTime += gameTime;

            await Task.Delay(SlowGameSpeed).ConfigureAwait(false);
        }
    }

    private async Task UpdateAbilityGameTimer()
    {
        _abilityGameTime = DateTime.Now;

        while (ServerSetup.Instance.Running)
        {
            var gTimeConvert = DateTime.Now;
            _abilityGameTimeSpan = gTimeConvert - _abilityGameTime;
            _abilityGameTime += _abilityGameTimeSpan;
            await Task.Delay(AbilityGameSpeed).ConfigureAwait(false);
        }
    }

    private static async Task NightlyServerRestart()
    {
        var currentTime = DateTime.Now;
        var midnight = DateTime.Today;

        while (ServerSetup.Instance.Running)
        {
            await Task.Delay(15000).ConfigureAwait(false);

            if (currentTime.Equals(midnight))
            {
                Commander.Chaos();
            }

            await Task.Delay(45000).ConfigureAwait(false);
        }
    }

    private static void ServerEncryptKey()
    {
        var data = new byte[40];

        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }

        ServerSetup.Instance.EncryptKeyConDict = new ConcurrentDictionary<int, byte>();
        ServerSetup.Instance.EncryptKeyConDict.TryAdd(Generator.GenerateNumber(), data[1]);
    }

    private void UpdateComponents(TimeSpan elapsedTime)
    {
        try
        {
            var components = _serverComponents.Select(i => i.Value);

            foreach (var component in components)
            {
                component?.Update(elapsedTime);
            }
        }
        catch (Exception ex)
        {
            ServerSetup.Logger(ex.Message, LogLevel.Error);
            Crashes.TrackError(ex);
        }
    }

    private void UpdateClients(TimeSpan elapsedTime)
    {
        var players = Clients.Values;

        foreach (var client in players.Where(client => client == null))
        {
            DisconnectAndRemoveClient(client);
        }

        foreach (var client in players.Where(client => client.Aisling != null))
        {
            try
            {
                switch (client.IsWarping)
                {
                    case false when !client.MapOpen:
                        client.Update(elapsedTime);
                        break;
                    case true:
                        break;
                }

                if (client.Aisling.Invisible) continue;
                var buffs = client.Aisling.Buffs.Values;

                foreach (var buff in buffs)
                {
                    if (buff.Name is "Hide" or "Shadowfade")
                        buff.OnEnded(client.Aisling, buff);
                }
            }
            catch (Exception ex)
            {
                ServerSetup.Logger(ex.Message, LogLevel.Error);
                ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
                Crashes.TrackError(ex);
                client.Server.ClientDisconnected(client);
                client.Server.RemoveClient(client);
            }
        }
    }

    private static void UpdateMonsters(TimeSpan elapsedTime)
    {
        // Cache traps to reduce Select operation on each iteration
        var traps = Trap.Traps.Values;

        foreach (var area in ServerSetup.Instance.GlobalMapCache.Values)
        {
            var updateList = ServerSetup.Instance.GlobalMonsterCache.Where(i => i.Value.Map.ID == area.ID);

            foreach (var (_, monster) in updateList)
            {
                if (monster == null) continue;
                if (monster.Map.ID != area.ID) continue;
                if (monster.Scripts == null) continue;
                if (monster.CurrentHp <= 0)
                {
                    UpdateKillCounters(monster);
                    monster.Skulled = true;

                    if (monster.Target?.Client != null)
                    {
                        monster.Scripts.Values.First().OnDeath(monster.Target.Client);
                    }
                    else
                    {
                        monster.Scripts.Values.First().OnDeath();
                    }
                }

                monster.Scripts.Values.First().Update(elapsedTime);

                foreach (var trap in traps)
                {
                    if (trap?.Owner == null || trap.Owner.Serial == monster.Serial ||
                        monster.X != trap.Location.X || monster.Y != trap.Location.Y) continue;

                    var triggered = Trap.Activate(trap, monster);
                    if (triggered) break;
                }

                monster.UpdateBuffs(elapsedTime);
                monster.UpdateDebuffs(elapsedTime);
                monster.LastUpdated = DateTime.Now;
            }
        }
    }

    private static void UpdateKillCounters(Monster monster)
    {
        if (monster.Target is not Aisling aisling) return;
        var readyTime = DateTime.Now;

        if (!aisling.MonsterKillCounters.ContainsKey(monster.Template.BaseName))
        {
            aisling.MonsterKillCounters[monster.Template.BaseName] =
                new KillRecord
                {
                    TotalKills = 1,
                    TimeKilled = readyTime
                };
        }
        else
        {
            aisling.MonsterKillCounters[monster.Template.BaseName].TotalKills++;
            aisling.MonsterKillCounters[monster.Template.BaseName].TimeKilled = readyTime;
        }

        QuestHandling(aisling, monster);
    }

    private static void QuestHandling(Aisling aisling, Monster monster)
    {
        if (!aisling.Client.Aisling.QuestManager.KeelaKill.IsNullOrEmpty())
        {
            if (aisling.Client.Aisling.QuestManager.KeelaKill == monster.Template.BaseName)
            {
                var killed = aisling.MonsterKillCounters[monster.Template.BaseName].TotalKills;

                if (killed >= aisling.Client.Aisling.QuestManager.KeelaCount)
                {
                    const string script = "Nadia";
                    var scriptObj = ServerSetup.Instance.GlobalMundaneScriptCache.FirstOrDefault(i => i.Key == script);
                    scriptObj.Value?.OnResponse(aisling.Client.Server, aisling.Client, 0x01, null);

                    return;
                }

                aisling.Client.SendMessage(0x03, $"{{=aAssassin Quest: {{=q{killed}{{=a killed.");
            }
        }

        if (!aisling.Client.Aisling.QuestManager.NealKill.IsNullOrEmpty())
        {
            if (aisling.Client.Aisling.QuestManager.NealKill == monster.Template.BaseName)
            {
                var killed = aisling.MonsterKillCounters[monster.Template.BaseName].TotalKills;

                if (killed >= aisling.Client.Aisling.QuestManager.NealCount)
                {
                    const string script = "Nadia";
                    var scriptObj = ServerSetup.Instance.GlobalMundaneScriptCache.FirstOrDefault(i => i.Key == script);
                    scriptObj.Value?.OnResponse(aisling.Client.Server, aisling.Client, 0x03, null);

                    return;
                }

                aisling.Client.SendMessage(0x03, $"{{=aNeal Quest: {{=q{killed}{{=a killed.");
            }
        }
    }

    private static void UpdateMundanes(TimeSpan elapsedTime)
    {
        foreach (var area in ServerSetup.Instance.GlobalMapCache.Values)
        {
            var updateList = ServerSetup.Instance.GlobalMundaneCache.Where(m => m.Value.Map.ID == area.ID);

            foreach (var (_, mundane) in updateList)
            {
                if (mundane == null) continue;
                if (mundane.Map.ID != area.ID) continue;

                mundane.Update(elapsedTime);
                mundane.LastUpdated = DateTime.Now;
            }
        }
    }

    private static void UpdateMaps(TimeSpan elapsedTime)
    {
        HashSet<Area> tmpAreas;

        lock (ServerSetup.SyncLock)
        {
            tmpAreas = new HashSet<Area>(ServerSetup.Instance.GlobalMapCache.Values);
        }

        foreach (var map in tmpAreas)
        {
            map.Update(elapsedTime);
        }
    }

    #endregion

    #region Client Handlers

    /// <summary>
    /// On Request Map Data
    /// </summary>
    protected override void Format05Handler(GameClient client, ClientFormat05 format)
    {
        if (client?.Aisling?.Map == null) return;
        if (client is not { Authenticated: true }) return;
        if (client.MapUpdating && client.Aisling.CurrentMapId != ServerSetup.Instance.Config.TransitionZone) return;

        client.MapUpdating = true;
        SendMapData(client);
        client.MapUpdating = false;
    }

    private static void SendMapData(GameClient client)
    {
        var cluster = new List<NetworkFormat>();

        for (var i = 0; i < client.Aisling.Map.Rows; i++)
        {
            var response = new ServerFormat3C
            {
                Line = (ushort)i,
                Data = client.Aisling.Map.GetRowData(i)
            };

            cluster.Add(response);
        }

        client.Send(cluster.ToArray());
        client.Aisling.Map.OnLoaded();
    }

    /// <summary>
    /// On Player Movement
    /// </summary>
    protected override void Format06Handler(GameClient client, ClientFormat06 format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.Map is not { Ready: true }) return;

        if (!client.Aisling.CanMove)
        {
            client.SendMessage(0x03, "{=bYou cannot feel your legs...");
            client.SendLocation();

            return;
        }

        client.Aisling.CanReact = true;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();

            return;
        }

        if (client.IsRefreshing && ServerSetup.Instance.Config.CancelWalkingIfRefreshing) return;

        if (client.Aisling.IsCastingSpell && ServerSetup.Instance.Config.CancelCastingWhenWalking) CancelIfCasting(client);

        client.Aisling.Direction = format.Direction;

        var success = client.Aisling.Walk();

        if (success)
        {
            if (client.Aisling.AreaId == ServerSetup.Instance.Config.TransitionZone)
            {
                var portal = new PortalSession();
                portal.TransitionToMap(client);
                return;
            }

            CheckWarpTransitions(client);

            if (client.Aisling.Map == null || !client.Aisling.Map.Scripts.Any()) return;

            foreach (var script in client.Aisling.Map.Scripts.Values)
            {
                script.OnPlayerWalk(client, client.Aisling.LastPosition, client.Aisling.Position);

                if (!client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill)) continue;

                foreach (var trap in Trap.Traps.Select(i => i.Value))
                {
                    if (trap?.Owner == null || trap.Owner.Serial == client.Aisling.Serial ||
                        client.Aisling.X != trap.Location.X ||
                        client.Aisling.Y != trap.Location.Y) continue;

                    var triggered = Trap.Activate(trap, client.Aisling);
                    if (triggered) break;
                }
            }
        }
        else
        {
            client.ClientRefreshed();
            CheckWarpTransitions(client);
        }

        client.LastMovement = DateTime.Now;
    }

    /// <summary>
    /// On Item Pickup from Map
    /// </summary>
    protected override void Format07Handler(GameClient client, ClientFormat07 format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsDead())
        {
            client.SendMessage(0x03, "You cannot do that.");
            return;
        }
        if (client.Aisling.HasDebuff("Skulled") || client.Aisling.IsParalyzed || client.Aisling.IsBeagParalyzed || client.Aisling.IsFrozen || client.Aisling.IsStopped)
        {
            client.SendMessage(0x03, "You cannot do that.");
            return;
        }

        var itemObjs = ObjectHandlers.GetObjects(client.Aisling.Map, i => (int)i.Pos.X == format.Position.X && (int)i.Pos.Y == format.Position.Y, ObjectManager.Get.Items).ToList();
        var moneyObjs = ObjectHandlers.GetObjects(client.Aisling.Map, i => (int)i.Pos.X == format.Position.X && (int)i.Pos.Y == format.Position.Y, ObjectManager.Get.Money);

        if (!itemObjs.IsEmpty())
        {
            var obj = itemObjs.First();
            if (obj?.CurrentMapId != client.Aisling.CurrentMapId) return;
            if (!(client.Aisling.Position.DistanceFrom(obj.Position) <= ServerSetup.Instance.Config.ClickLootDistance)) return;

            if (obj is not Item item) return;
            if ((item.Template.Flags & ItemFlags.Trap) == ItemFlags.Trap) return;
            if (item.Template.Flags.FlagIsSet(ItemFlags.Unique) && item.Template.Name == "Necra Scribblings")
                if (client.Aisling.Stage >= ClassStage.Master) return;


            foreach (var invItem in client.Aisling.Inventory.Items.Values)
            {
                if (invItem == null) continue;
                if (!invItem.Template.Flags.FlagIsSet(ItemFlags.Unique)) continue;
                if (invItem.Template.Name != item.Template.Name) continue;
                client.SendMessage(0x03, "You may only hold one in your possession.");
                return;
            }

            foreach (var invItem in client.Aisling.BankManager.Items.Values)
            {
                if (invItem == null) continue;
                if (!invItem.Template.Flags.FlagIsSet(ItemFlags.Unique)) continue;
                if (invItem.Template.Name != item.Template.Name) continue;
                client.SendMessage(0x03, "You may only hold one in your possession.");
                return;
            }

            if (item.Cursed)
            {
                Sprite first = null;

                if (item.AuthenticatedAislings != null)
                {
                    foreach (var i in item.AuthenticatedAislings)
                    {
                        if (i.Serial != client.Aisling.Serial) continue;

                        first = i;
                        break;
                    }

                    if (item.AuthenticatedAislings != null && first == null)
                    {
                        client.SendMessage(0x02, $"{ServerSetup.Instance.Config.CursedItemMessage}");
                        return;
                    }
                }

                item.Pos = client.Aisling.Pos;
                item.Show(Scope.NearbyAislings, new ServerFormat07(new[] { obj }));
            }

            if (item.GiveTo(client.Aisling))
            {
                item.Remove();
                if (item.Scripts is null) return;
                foreach (var itemScript in item.Scripts.Values)
                    itemScript?.OnPickedUp(client.Aisling, format.Position, client.Aisling.Map);
                return;
            }

            item.Pos = client.Aisling.Pos;
            item.Show(Scope.NearbyAislings, new ServerFormat07(new[] { obj }));
        }

        foreach (var obj in moneyObjs)
        {
            if (obj?.CurrentMapId != client.Aisling.CurrentMapId) break;
            if (!(client.Aisling.Position.DistanceFrom(obj.Position) <= ServerSetup.Instance.Config.ClickLootDistance)) break;

            if (obj is not Money money) continue;

            money.GiveTo(money.Amount, client.Aisling);
        }
    }

    /// <summary>
    /// On Item dropped on Map
    /// </summary>
    protected override void Format08Handler(GameClient client, ClientFormat08 format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsDead())
        {
            client.SendMessage(0x03, "You cannot do that.");
            return;
        }
        if (client.Aisling.HasDebuff("Skulled") || client.Aisling.IsParalyzed || client.Aisling.IsBeagParalyzed || client.Aisling.IsFrozen || client.Aisling.IsStopped)
        {
            client.SendMessage(0x03, "You cannot do that.");
            return;
        }
        if (client.Aisling.Map is not { Ready: true }) return;
        if (format.ItemSlot is 0) return;
        if (format.ItemAmount is > 1000 or < 0) return;

        Item item = null;
        if (client.Aisling.Inventory.Items.TryGetValue(format.ItemSlot, out var value))
        {
            if (value is null) return;
            item = value;
            item.Serial = Generator.GenerateNumber();
        }

        if (item == null) return;

        if (item.Stacks > 1)
        {
            if (format.ItemAmount > item.Stacks)
            {
                client.SendMessage(0x02, "Wait.. how many did I have again?");
                return;
            }
        }

        if (!item.Template.Flags.FlagIsSet(ItemFlags.Dropable))
        {
            client.SendMessage(Scope.Self, 0x02, $"{ServerSetup.Instance.Config.CantDropItemMsg}");
            return;
        }

        var itemPosition = new Position(format.X, format.Y);

        if (client.Aisling.Position.DistanceFrom(itemPosition.X, itemPosition.Y) > 9)
        {
            client.SendMessage(Scope.Self, 0x02, "I can not do that. Too far.");
            return;
        }

        if (client.Aisling.Map.IsWall(format.X, format.Y))
            if ((int)client.Aisling.Pos.X != format.X || (int)client.Aisling.Pos.Y != format.Y)
            {
                client.SendMessage(Scope.Self, 0x02, "Something is in the way.");
                return;
            }

        if (item.Template.Flags.FlagIsSet(ItemFlags.Stackable))
        {
            if (format.ItemAmount > item.Stacks)
            {
                client.SendMessage(0x02, "Wait.. how many did I have again?");
                return;
            }

            var remaining = item.Stacks - (ushort)format.ItemAmount;
            item.Dropping = format.ItemAmount;

            if (remaining == 0)
            {
                if (client.Aisling.EquipmentManager.RemoveFromInventory(item, true))
                {
                    item.Release(client.Aisling, new Position(format.X, format.Y));

                    // Mileth Altar 
                    if (client.Aisling.Map.ID == 500)
                    {
                        if (itemPosition.X == 31 && itemPosition.Y == 52 || itemPosition.X == 31 && itemPosition.Y == 53)
                            item.Remove();
                    }
                }
            }
            else
            {
                var temp = new Item
                {
                    Slot = item.Slot,
                    Image = item.Image,
                    DisplayImage = item.DisplayImage,
                    Durability = item.Durability,
                    ItemVariance = item.ItemVariance,
                    WeapVariance = item.WeapVariance,
                    ItemQuality = item.ItemQuality,
                    OriginalQuality = item.OriginalQuality,
                    InventorySlot = format.ItemSlot,
                    Stacks = (ushort)format.ItemAmount,
                    Template = item.Template
                };

                temp.Release(client.Aisling, itemPosition);

                // Mileth Altar 
                if (client.Aisling.Map.ID == 500)
                {
                    if (itemPosition.X == 31 && itemPosition.Y == 52 || itemPosition.X == 31 && itemPosition.Y == 53)
                        temp.Remove();
                }

                item.Stacks = (ushort)remaining;
                client.Send(new ServerFormat10(item.InventorySlot));
                client.Aisling.Inventory.Set(item);
                client.Aisling.Inventory.UpdateSlot(client, item);
            }
        }
        else
        {
            if (!item.Template.Flags.FlagIsSet(ItemFlags.DropScript))
                if (client.Aisling.EquipmentManager.RemoveFromInventory(item, true))
                {
                    item.Release(client.Aisling, new Position(format.X, format.Y));

                    // Mileth Altar 
                    if (client.Aisling.Map.ID == 500)
                    {
                        if (itemPosition.X == 31 && itemPosition.Y == 52 || itemPosition.X == 31 && itemPosition.Y == 53)
                            item.Remove();
                    }
                }
        }

        client.Aisling.Inventory.UpdatePlayersWeight(client);
        client.SendStats(StatusFlags.WeightMoney);

        if (!item.Template.Flags.FlagIsSet(ItemFlags.DropScript))
            if (client.Aisling.Map != null && client.Aisling.Map.Scripts.Any())
            {
                foreach (var script in client.Aisling.Map.Scripts.Values)
                {
                    script.OnItemDropped(client, item, itemPosition);
                }
            }

        if (item.Scripts == null) return;
        foreach (var itemScript in (item.Scripts.Values))
        {
            itemScript?.OnDropped(client.Aisling, new Position(format.X, format.Y), client.Aisling.Map);
        }
    }

    /// <summary>
    /// On Client End-Game Request
    /// </summary>
    protected override void Format0BHandler(GameClient client, ClientFormat0B format)
    {
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        LeaveGame(client, format);
    }

    /// <summary>
    /// Display Object Request
    /// </summary>
    protected override void Format0CHandler(GameClient client, ClientFormat0C format) { }

    /// <summary>
    /// On Client End-Game Request Continued...
    /// </summary>
    private void LeaveGame(GameClient client, ClientFormat0B format)
    {
        if (client?.Aisling == null) return;

        Party.RemovePartyMember(client.Aisling);
        RemoveFromServer(client, format.Type);

        switch (format.Type)
        {
            case 1:
                client.Send(new ServerFormat4C());
                break;
            case 3:
                {
                    client.Aisling.Remove(true);
                    ExitGame(client);
                    break;
                }
        }
    }

    /// <summary>
    /// On Ignore Player - F9 Button
    /// </summary>
    protected override void Format0DHandler(GameClient client, ClientFormat0D format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;

        switch (format.IgnoreType)
        {
            case 1:
                var ignored = string.Join(", ", client.Aisling.IgnoredList);
                client.SendMessage(0x09, ignored);
                break;
            case 2:
                if (format.Target == null) break;
                if (format.Target.EqualsIgnoreCase("Death")) break;
                if (client.Aisling.IgnoredList.ListContains(format.Target)) break;
                client.AddToIgnoreListDb(format.Target);
                break;
            case 3:
                if (format.Target == null) break;
                if (format.Target.EqualsIgnoreCase("Death")) break;
                if (!client.Aisling.IgnoredList.ListContains(format.Target)) break;
                client.RemoveFromIgnoreListDb(format.Target);
                break;
        }
    }

    /// <summary>
    /// On Public Message
    /// </summary>
    protected override void Format0EHandler(GameClient client, ClientFormat0E format)
    {
        bool ParseCommand()
        {
            if (!client.Aisling.GameMaster) return false;
            if (!format.Text.StartsWith("/")) return false;
            Commander.ParseChatMessage(client, format.Text);
            return true;
        }

        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsSilenced) return;

        var response = new ServerFormat0D
        {
            Serial = client.Aisling.Serial,
            Type = format.Type,
            Text = string.Empty
        };

        IEnumerable<Aisling> audience;

        if (ParseCommand()) return;

        switch (format.Type)
        {
            case 0x00:
                response.Text = $"{client.Aisling.Username}: {format.Text}";
                audience = ObjectHandlers.GetObjects<Aisling>(client.Aisling.Map, n => client.Aisling.WithinRangeOf(n));
                break;
            case 0x01:
                response.Text = $"{client.Aisling.Username}! {format.Text}";
                audience = ObjectHandlers.GetObjects<Aisling>(client.Aisling.Map, n => client.Aisling.CurrentMapId == n.CurrentMapId);
                break;
            case 0x02:
                response.Text = format.Text;
                audience = ObjectHandlers.GetObjects<Aisling>(client.Aisling.Map, n => client.Aisling.WithinRangeOf(n, false));
                break;
            default:
                ClientDisconnected(client);
                RemoveClient(client);
                return;
        }

        var nearbyMundanes = client.Aisling.MundanesNearby();
        var playerMap = client.Aisling.Map;

        foreach (var npc in nearbyMundanes)
        {
            if (npc?.Scripts is null) continue;

            foreach (var script in npc.Scripts.Values)
                script?.OnGossip(this, client, format.Text);
        }

        foreach (var action in playerMap.Scripts.Values)
        {
            action?.OnGossip(client, format.Text);
        }

        var playersToShowList = audience.Where(player => !player.IgnoredList.ListContains(client.Aisling.Username));
        client.Aisling.Show(Scope.DefinedAislings, response, playersToShowList);
    }

    /// <summary>
    /// Spell Use
    /// </summary>
    protected override void Format0FHandler(GameClient client, ClientFormat0F format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsDead()) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        var spellReq = client.Aisling.SpellBook.GetSpells(i => i != null && i.Slot == format.Index).FirstOrDefault();

        if (spellReq == null) return;

        //ToDo: Abort cast if spell is not "Ao Suain" or "Ao Pramh"
        if (!client.Aisling.CanCast)
            if (spellReq.Template.Name != "Ao Suain" && spellReq.Template.Name != "Ao Pramh")
            {
                CancelIfCasting(client);
                return;
            }

        var info = new CastInfo
        {
            Slot = format.Index,
            Target = format.Serial,
            Position = format.Point,
            Data = format.Data,
        };

        info.Position ??= new Position(client.Aisling.X, client.Aisling.Y);

        lock (client.CastStack)
        {
            client.CastStack?.Push(info);
        }
    }

    /// <summary>
    /// Client Redirect to GameServer from LoginServer
    /// </summary>
    protected override async Task Format10Handler(GameClient client, ClientFormat10 format)
    {
        if (client == null) return;
        if (format.Name.IsNullOrEmpty()) return;

        await EnterGame(client, format);

        if (client.Server.Clients.Values.Count <= 2000) return;
        client.SendMessage(0x08, $"Server Capacity Reached: {client.Server.Clients.Values.Count} \n Thank you, please come back later.");
        ClientDisconnected(client);
        RemoveClient(client);
    }

    /// <summary>
    /// Client Redirect to GameServer from LoginServer Continued...
    /// </summary>
    private async Task EnterGame(GameClient client, ClientFormat10 format)
    {
        client.Encryption.Parameters = format.Parameters;
        client.Server = this;
        var serialString = string.Concat(format.Name.Where(char.IsDigit));

        if (!uint.TryParse(serialString, out var serial) || serial == 0)
        {
            DisconnectAndRemoveClient(client);
            return;
        }

        var playerName = string.Concat(format.Name.TrimEnd("0123456789".ToCharArray()).Select(char.ToLowerInvariant));

        if (Clients.Values.Any(globalClient => globalClient.Serial != client.Serial &&
                                               globalClient.Aisling?.Username?.Equals(playerName, StringComparison.OrdinalIgnoreCase) == true))
        {
            client.SendMessage(0x08, "You're already logged in.");
            DisconnectAndRemoveClient(client);
            return;
        }

        var player = await LoadPlayer(client, playerName, serial);

        if (player == null || serial != client.Aisling.Serial)
        {
            DisconnectAndRemoveClient(client);
            return;
        }

        player.GameMaster = ServerSetup.Instance.Config.GameMasters?.Any(n => string.Equals(n, player.Username, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (player.GameMaster)
        {
            const string ip = "192.168.50.1";
            var ipLocal = IPAddress.Parse(ip);

            if (player.Client.Server.Address.Equals(ServerSetup.Instance.IpAddress) || player.Client.Server.Address.Equals(ipLocal))
            {
                player.Show(Scope.NearbyAislings, new ServerFormat29(391, player.Pos));
            }
            else
            {
                DisconnectAndRemoveClient(client);
                return;
            }
        }

        AuthenticateClient(client);
        var time = DateTime.Now;
        ServerSetup.Logger($"{player.Username} logged in at: {time}");
        client.LastPing = time;
    }

    private void DisconnectAndRemoveClient(GameClient client)
    {
        ClientDisconnected(client);
        RemoveClient(client);
    }

    /// <summary>
    /// Client Authentication
    /// </summary>
    private void AuthenticateClient(GameClient client)
    {
        if (ServerSetup.Redirects.ContainsKey(client.Aisling.Serial))
        {
            client.Aisling.Client.Authenticated = true;
            return;
        }

        ClientDisconnected(client);
        RemoveClient(client);
    }

    /// <summary>
    /// On Player Change Direction
    /// </summary>
    protected override void Format11Handler(GameClient client, ClientFormat11 format)
    {
        if (client.Aisling == null) return;
        if (client is not { Authenticated: true }) return;

        client.Aisling.Direction = format.Direction;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        client.Aisling.Show(Scope.NearbyAislings, new ServerFormat11
        {
            Direction = client.Aisling.Direction,
            Serial = client.Aisling.Serial
        });
    }

    /// <summary>
    /// Auto-Attack - Spacebar Button
    /// </summary>
    protected override void Format13Handler(GameClient client, ClientFormat13 format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsDead()) return;
        if (ServerSetup.Instance.Config.AssailsCancelSpells) CancelIfCasting(client);

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (!client.Aisling.CanAttack)
        {
            client.Interrupt();
            return;
        }

        Assail(client);
    }

    public static void Assail(IGameClient lpClient)
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
            ExecuteAbility(lpClient, skill);
            skill.InUse = false;

            // Skill cleanup
            skill.CurrentCooldown = skill.Template.Cooldown;
            lastTemplate = skill.Template.Name;
            lpClient.LastAssail = DateTime.Now;
        }
    }

    /// <summary>
    /// On World list Request - (Who is Online)
    /// </summary>
    protected override void Format18Handler(GameClient client, ClientFormat18 format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.IsRefreshing) return;

        client.Aisling.Show(Scope.Self, new ServerFormat36(client));
    }

    /// <summary>
    /// On Private Message (Guild, Group, Direct, World)
    /// </summary>
    protected override void Format19Handler(GameClient client, ClientFormat19 format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (format == null) return;

        var readyTime = DateTime.Now;
        if (readyTime.Subtract(client.LastWhisperMessageSent).TotalSeconds < 0.30) return;
        if (format.Name.Length > 24) return;

        client.LastWhisperMessageSent = readyTime;

        switch (format.Name)
        {
            case "#" when client.Aisling.GameMaster:
                foreach (var client2 in from client2 in ServerSetup.Instance.Game.Clients.Values where client2?.Aisling != null select client2)
                {
                    if (client2 is null) return;
                    client2.SendMessage(0x0B, $"{{=c{client.Aisling.Username}: {format.Message}");
                }
                break;
            case "#" when client.Aisling.GameMaster != true:
                client.SystemMessage("You cannot broadcast in this way.");
                break;
            case "!" when !string.IsNullOrEmpty(client.Aisling.Clan):
                foreach (var client2 in from client2 in ServerSetup.Instance.Game.Clients.Values
                                        where client2?.Aisling != null
                                        select client2)
                {
                    if (client2 is null) return;
                    if (client2.Aisling.Clan == client.Aisling.Clan)
                    {
                        client2.SendMessage(0x0B, $"<!{client.Aisling.Username}> {format.Message}");
                    }
                }
                break;
            case "!" when string.IsNullOrEmpty(client.Aisling.Clan):
                client.SystemMessage("{=eYou're not in a guild.");
                return;
            case "!!" when client.Aisling.PartyMembers != null:
                foreach (var client2 in from client2 in ServerSetup.Instance.Game.Clients.Values
                                        where client2?.Aisling != null
                                        select client2)
                {
                    if (client2 is null) return;
                    if (client2.Aisling.GroupParty == client.Aisling.GroupParty)
                    {
                        client2.SendMessage(0x0C, $"[!{client.Aisling.Username}] {format.Message}");
                    }
                }
                break;
            case "!!" when client.Aisling.PartyMembers == null:
                client.SystemMessage("{=eYou're not in a group or party.");
                return;
        }

        var user = Clients.Values.FirstOrDefault(i => i?.Aisling != null && i.Aisling.LoggedIn && i.Aisling.Username.ToLower() ==
            format.Name.ToLower(CultureInfo.CurrentCulture));

        if (format.Name != "!" && format.Name != "!!" && format.Name != "#" && user == null)
            client.SendMessage(0x02, string.Format(CultureInfo.CurrentCulture, "{0} is nowhere to be found.", format.Name));

        if (user == null) return;

        if (user.Aisling.IgnoredList.ListContains(client.Aisling.Username))
        {
            user.SendMessage(0x00, $"{client.Aisling.Username} tried to message you but it was intercepted.");
            client.SendMessage(0x00, "You cannot bother people this way.");
            return;
        }

        user.SendMessage(0x00, string.Format(CultureInfo.CurrentCulture, "{0}: {1}", client.Aisling.Username, format.Message));
        client.SendMessage(0x00, string.Format(CultureInfo.CurrentCulture, "{0}> {1}", user.Aisling.Username, format.Message));
    }

    /// <summary>
    /// On Client -Options Panel- Change - F4 Button
    /// </summary>
    protected override void Format1BHandler(GameClient client, ClientFormat1B format)
    {
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (client.Aisling.GameSettings == null) return;

        var settingKeys = client.Aisling.GameSettings.ToArray();

        if (settingKeys.Length == 0) return;

        var settingIdx = format.Index;

        if (settingIdx > 0)
        {
            settingIdx--;

            var setting = settingKeys[settingIdx];
            setting.Toggle();

            UpdateSettings(client);
        }
        else
        {
            UpdateSettings(client);
        }
    }

    private static void UpdateSettings(IGameClient client)
    {
        var msg = "\t";

        foreach (var setting in client.Aisling.GameSettings.Where(setting => setting != null))
        {
            msg += setting.Enabled ? setting.EnabledSettingStr : setting.DisabledSettingStr;
            msg += "\t";
        }

        client.SendMessage(0x07, msg);
    }

    /// <summary>
    /// On Item Usage
    /// </summary>
    protected override void Format1CHandler(GameClient client, ClientFormat1C format)
    {
        if (client?.Aisling?.Map == null || !client.Aisling.Map.Ready) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsDead())
        {
            client.SendMessage(0x03, "You cannot do that.");
            return;
        }
        if (client.Aisling.HasDebuff("Skulled") || client.Aisling.IsParalyzed || client.Aisling.IsBeagParalyzed || client.Aisling.IsFrozen || client.Aisling.IsStopped)
        {
            client.SendMessage(0x03, "You cannot do that.");
            return;
        }
        // Speed equipping prevent (movement)
        if (!client.IsEquipping)
        {
            client.SendMessage(0x03, "Slow down");
            return;
        }

        client.LastEquip = DateTime.Now;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (client.Aisling.IsParalyzed || client.Aisling.IsFrozen || client.Aisling.IsSleeping || client.Aisling.IsStopped)
        {
            client.SystemMessage("Unable to move your arms.");
            client.Interrupt();
            return;
        }

        var slot = format.Index;
        var item = client.Aisling.Inventory.Get(i => i != null && i.InventorySlot == slot).FirstOrDefault();

        if (item?.Template == null) return;
        var activated = false;

        // Run Scripts on item on use
        if (!string.IsNullOrEmpty(item.Template.ScriptName)) item.Scripts ??= ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);
        if (!string.IsNullOrEmpty(item.Template.WeaponScript)) item.WeaponScripts ??= ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);

        if (item.Scripts == null)
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.CantUseThat}");
        }
        else
        {
            var script = item.Scripts.Values.First();
            script?.OnUse(client.Aisling, slot);
            activated = true;
        }

        if (!activated) return;
        if (!item.Template.Flags.FlagIsSet(ItemFlags.Stackable)) return;
        if (!item.Template.Flags.FlagIsSet(ItemFlags.Consumable)) return;

        var stack = item.Stacks - 1;

        if (stack > 0)
        {
            item.Stacks -= 1;
            client.Send(new ServerFormat10(item.InventorySlot));
            client.Aisling.Inventory.Set(item);
            client.Aisling.Inventory.UpdateSlot(client, item);
        }
        else
        {
            client.Aisling.EquipmentManager.RemoveFromInventory(item, true);
        }
    }

    /// <summary>
    /// On Client using Emote
    /// </summary>
    protected override void Format1DHandler(GameClient client, ClientFormat1D format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.IsRefreshing) return;
        if (client.Aisling.IsDead()) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        var id = format.Number;

        if (id <= 35)
            client.Aisling.Show(Scope.NearbyAislings, new ServerFormat1A(client.Aisling.Serial, (byte)(id + 9), 120));
    }

    /// <summary>
    /// On Client Dropping Gold on Map
    /// </summary>
    protected override void Format24Handler(GameClient client, ClientFormat24 format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.CanAttack) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (client.Aisling.GoldPoints >= format.GoldAmount)
        {
            client.Aisling.GoldPoints -= format.GoldAmount;
            if (client.Aisling.GoldPoints <= 0)
                client.Aisling.GoldPoints = 0;

            client.SendMessage(Scope.Self, 0x02, $"{ServerSetup.Instance.Config.YouDroppedGoldMsg}");
            client.SendMessage(Scope.NearbyAislingsExludingSelf, 0x02, $"{ServerSetup.Instance.Config.UserDroppedGoldMsg.Replace("noname", client.Aisling.Username)}");

            Money.Create(client.Aisling, format.GoldAmount, new Position(format.X, format.Y));
            client.SendStats(StatusFlags.StructC);
        }
        else
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NotEnoughGoldToDropMsg}");
        }
    }

    /// <summary>
    /// On Item Dropped on Sprite
    /// </summary>
    protected override void Format29Handler(GameClient client, ClientFormat29 format)
    {
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;

        client.Send(new ServerFormat4B(format.ID, 0));
        client.Send(new ServerFormat4B(format.ID, 1, format.ItemSlot));
        var result = new List<Sprite>();
        var listA = client.Aisling.GetObjects<Monster>(client.Aisling.Map, i => i != null && i.WithinRangeOf(client.Aisling, ServerSetup.Instance.Config.WithinRangeProximity));
        var listB = client.Aisling.GetObjects<Mundane>(client.Aisling.Map, i => i != null && i.WithinRangeOf(client.Aisling, ServerSetup.Instance.Config.WithinRangeProximity));
        var listC = client.Aisling.GetObjects<Aisling>(client.Aisling.Map, i => i != null && i.WithinRangeOf(client.Aisling, ServerSetup.Instance.Config.WithinRangeProximity));
        result.AddRange(listA);
        result.AddRange(listB);
        result.AddRange(listC);

        foreach (var sprite in result.Where(sprite => sprite.Serial == format.ID))
        {
            switch (sprite)
            {
                case Monster monster:
                    {
                        var script = monster.Scripts.Values.First();
                        var item = client.Aisling.Inventory.Items[format.ItemSlot];
                        client.Aisling.EquipmentManager.RemoveFromInventory(item, true);
                        script?.OnItemDropped(client, item);

                        break;
                    }
                case Mundane mundane:
                    {
                        var script = mundane.Scripts.Values.First();
                        var item = client.Aisling.Inventory.Items[format.ItemSlot];
                        script?.OnItemDropped(client, item);

                        break;
                    }
                case Aisling aisling:
                    {
                        if (format.ItemSlot == 0) return;
                        var item = client.Aisling.Inventory.Items[format.ItemSlot];

                        if (item.DisplayName.StringContains("deum"))
                        {
                            var script = item.Scripts.Values.First();
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.Aisling.ThrewHealingPot = true;

                            var action = new ServerFormat1A
                            {
                                Serial = aisling.Serial,
                                Number = 0x06,
                                Speed = 50
                            };

                            script?.OnUse(aisling, format.ItemSlot);
                            client.Aisling.Show(Scope.NearbyAislings, action);
                        }

                        if (item.DisplayName == "Elixir of Life")
                        {
                            client.Aisling.Inventory.RemoveRange(client, item, 1);
                            client.Aisling.ThrewHealingPot = true;
                            client.Aisling.ReviveFromAfar(aisling);
                        }

                        break;
                    }
            }
        }
    }

    /// <summary>
    /// On Gold Dropped on Sprite
    /// </summary>
    protected override void Format2AHandler(GameClient client, ClientFormat2A format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        var result = new List<Sprite>();
        var listA = client.Aisling.GetObjects<Monster>(client.Aisling.Map, i => i != null && i.WithinRangeOf(client.Aisling, ServerSetup.Instance.Config.WithinRangeProximity));
        var listB = client.Aisling.GetObjects<Mundane>(client.Aisling.Map, i => i != null && i.WithinRangeOf(client.Aisling, ServerSetup.Instance.Config.WithinRangeProximity));
        var listC = client.Aisling.GetObjects<Aisling>(client.Aisling.Map, i => i != null && i.WithinRangeOf(client.Aisling, ServerSetup.Instance.Config.WithinRangeProximity));

        result.AddRange(listA);
        result.AddRange(listB);
        result.AddRange(listC);

        foreach (var sprite in result.Where(sprite => sprite.Serial == format.ID))
        {
            switch (sprite)
            {
                case Monster monster:
                    {
                        var script = monster.Scripts.Values.First();

                        if (client.Aisling.GoldPoints >= format.Gold)
                        {
                            client.Aisling.GoldPoints -= format.Gold;
                            client.SendStats(StatusFlags.WeightMoney);
                        }
                        else
                            break;

                        script?.OnGoldDropped(client, format.Gold);

                        break;
                    }
                case Mundane mundane:
                    {
                        var script = mundane.Scripts.Values.First();

                        if (client.Aisling.GoldPoints >= format.Gold)
                        {
                            client.Aisling.GoldPoints -= format.Gold;
                            client.SendStats(StatusFlags.WeightMoney);
                        }
                        else
                            break;

                        script?.OnGoldDropped(client, format.Gold);

                        break;
                    }
                case Aisling aisling:
                    {
                        var format4AReceiver = new ClientFormat4A
                        {
                            Id = (uint)client.Aisling.Serial,
                            Type = byte.MinValue,
                            Command = 74
                        };

                        var format4ASender = new ClientFormat4A
                        {
                            Gold = format.Gold,
                            Id = format.ID,
                            Type = 0x03,
                            Command = 74
                        };

                        Format4AHandler(aisling.Client, format4AReceiver);
                        Format4AHandler(client, format4ASender);
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// On Player Request Profile - Load Profile
    /// </summary>
    protected override void Format2DHandler(GameClient client, ClientFormat2D format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        client.Send(new ServerFormat39(client.Aisling));
    }

    /// <summary>
    /// On Party Join Request
    /// </summary>
    protected override void Format2EHandler(GameClient client, ClientFormat2E format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.IsRefreshing) return;
        if (client.Aisling.IsDead()) return;

        if (format.Type != 0x02) return;

        var player = ObjectHandlers.GetObject<Aisling>(client.Aisling.Map, i => string.Equals(i.Username, format.Name, StringComparison.CurrentCultureIgnoreCase)
            && i.WithinRangeOf(client.Aisling));

        if (player == null)
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.BadRequestMessage}");
            return;
        }

        if (player.PartyStatus != GroupStatus.AcceptingRequests)
        {
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.GroupRequestDeclinedMsg.Replace("noname", player.Username)}");
            return;
        }

        if (Party.AddPartyMember(client.Aisling, player))
        {
            client.Aisling.PartyStatus = GroupStatus.AcceptingRequests;
            if (client.Aisling.GroupParty.PartyMembers.Any(other => other.Invisible))
                client.UpdateDisplay();
            return;
        }

        Party.RemovePartyMember(player);
    }

    /// <summary>
    /// On Toggle Group Button
    /// </summary>
    protected override void Format2FHandler(GameClient client, ClientFormat2F format)
    {
        if (client is not { Authenticated: true }) return;

        var mode = client.Aisling.PartyStatus;

        mode = mode switch
        {
            GroupStatus.AcceptingRequests => GroupStatus.NotAcceptingRequests,
            GroupStatus.NotAcceptingRequests => GroupStatus.AcceptingRequests,
            _ => mode
        };

        client.Aisling.PartyStatus = mode;

        if (client.Aisling.PartyStatus == GroupStatus.NotAcceptingRequests)
        {
            if (client.Aisling.LeaderPrivileges)
            {
                if (!ServerSetup.Instance.GlobalGroupCache.TryGetValue(client.Aisling.GroupId, out var group)) return;

                if (group != null)
                    Party.DisbandParty(group);
            }

            Party.RemovePartyMember(client.Aisling);
            client.ClientRefreshed();
        }
    }

    /// <summary>
    /// Swapping Sprites within UI (Skills, Spells, Items)
    /// </summary>
    protected override void Format30Handler(GameClient client, ClientFormat30 format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.IsRefreshing) return;
        CancelIfCasting(client);
        if (client.Aisling.IsDead()) return;

        if (!client.Aisling.CanMove || !client.Aisling.CanAttack || !client.Aisling.CanCast || client.Aisling.IsCastingSpell)
        {
            client.Interrupt();
            return;
        }

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        // ToDo: Moving UI items, skills, spells around
        switch (format.PaneType)
        {
            case Pane.Inventory:
                {
                    if (format.MovingTo > 59) return;
                    if (format.MovingFrom > 59) return;
                    if (format.MovingTo - 1 < 0) return;
                    if (format.MovingFrom - 1 < 0) return;

                    client.Send(new ServerFormat10(format.MovingFrom));
                    client.Send(new ServerFormat10(format.MovingTo));

                    var a = client.Aisling.Inventory.Remove(format.MovingFrom);
                    var b = client.Aisling.Inventory.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.InventorySlot = format.MovingTo;
                        client.Aisling.Inventory.Set(a);
                        client.Aisling.Inventory.UpdateSlot(client, a);
                    }

                    if (b != null)
                    {
                        b.InventorySlot = format.MovingFrom;
                        client.Aisling.Inventory.Set(b);
                        client.Aisling.Inventory.UpdateSlot(client, b);
                    }
                }
                break;
            case Pane.Skills:
                {
                    if (format.MovingTo - 1 < 0) return;
                    if (format.MovingFrom - 1 < 0) return;

                    if (format.MovingTo == 35)
                    {
                        var skillSlot = client.Aisling.SkillBook.FindEmpty(36);
                        format.MovingTo = (byte)skillSlot;
                    }

                    if (format.MovingTo == 71)
                    {
                        var skillSlot = client.Aisling.SkillBook.FindEmpty();
                        format.MovingTo = (byte)skillSlot;
                    }

                    client.Send(new ServerFormat2D(format.MovingFrom));
                    client.Send(new ServerFormat2D(format.MovingTo));

                    var a = client.Aisling.SkillBook.Remove(format.MovingFrom);
                    var b = client.Aisling.SkillBook.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.Slot = format.MovingTo;
                        client.Send(new ServerFormat2C(a.Slot, a.Icon, a.Name));
                        client.Aisling.SkillBook.Set(a);
                    }

                    if (b != null)
                    {
                        b.Slot = format.MovingFrom;
                        client.Send(new ServerFormat2C(b.Slot, b.Icon, b.Name));
                        client.Aisling.SkillBook.Set(b);
                    }
                }
                break;
            case Pane.Spells:
                {
                    if (format.MovingTo - 1 < 0) return;
                    if (format.MovingFrom - 1 < 0) return;

                    if (format.MovingTo == 35)
                    {
                        var spellSlot = client.Aisling.SpellBook.FindEmpty(36);
                        format.MovingTo = (byte)spellSlot;
                    }

                    if (format.MovingTo == 71)
                    {
                        var spellSlot = client.Aisling.SpellBook.FindEmpty();
                        format.MovingTo = (byte)spellSlot;
                    }

                    client.Send(new ServerFormat18(format.MovingFrom));
                    client.Send(new ServerFormat18(format.MovingTo));

                    var a = client.Aisling.SpellBook.Remove(format.MovingFrom);
                    var b = client.Aisling.SpellBook.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.Slot = format.MovingTo;
                        client.Send(new ServerFormat17(a));
                        client.Aisling.SpellBook.Set(a);
                    }

                    if (b != null)
                    {
                        b.Slot = format.MovingFrom;
                        client.Send(new ServerFormat17(b));
                        client.Aisling.SpellBook.Set(b);
                    }
                }
                break;
            case Pane.Tools:
                {
                    if (format.MovingTo - 1 < 0) return;
                    if (format.MovingFrom - 1 < 0) return;

                    client.Send(new ServerFormat18(format.MovingFrom));
                    client.Send(new ServerFormat18(format.MovingTo));

                    var a = client.Aisling.SpellBook.Remove(format.MovingFrom);
                    var b = client.Aisling.SpellBook.Remove(format.MovingTo);

                    if (a != null)
                    {
                        a.Slot = format.MovingTo;
                        client.Send(new ServerFormat17(a));
                        client.Aisling.SpellBook.Set(a);
                    }

                    if (b != null)
                    {
                        b.Slot = format.MovingFrom;
                        client.Send(new ServerFormat17(b));
                        client.Aisling.SpellBook.Set(b);
                    }
                }
                break;
        }
    }

    protected override void Format32Handler(GameClient client, ClientFormat32 format)
    {
        Console.Write($"Format32HandlerDiscovery: {format.UnknownA}\n{format.UnknownB}\n{format.UnknownC}\n{format.UnknownD}\n");
    }

    /// <summary>
    /// On Client Refresh - F5 Button
    /// </summary>
    protected override void Format38Handler(GameClient client, ClientFormat38 format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.IsRefreshing) return;

        client.ClientRefreshed();
    }

    /// <summary>
    /// Request Pursuit
    /// </summary>
    protected override void Format39Handler(GameClient client, ClientFormat39 format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;

        CancelIfCasting(client);

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (!client.Aisling.CanCast || !client.Aisling.CanAttack)
        {
            client.Interrupt();
            return;
        }

        var npcs = ServerSetup.Instance.GlobalMundaneCache.Where(i => i.Key == format.Serial);

        foreach (var npc in npcs)
        {
            if (npc.Value?.Template?.ScriptKey == null) continue;

            var scriptObj = ServerSetup.Instance.GlobalMundaneScriptCache.FirstOrDefault(i => i.Key == npc.Value.Template.Name);
            scriptObj.Value?.OnResponse(this, client, format.Step, format.Args);
        }
    }

    /// <summary>
    /// NPC Input Response -- Story Building, Send 3A after OnResponse
    /// </summary>
    protected override void Format3AHandler(GameClient client, ClientFormat3A format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;

        CancelIfCasting(client);

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (!client.Aisling.CanCast || !client.Aisling.CanAttack)
        {
            client.Interrupt();
            return;
        }

        if (format.Step == 0 && format.ScriptId == ushort.MaxValue)
        {
            client.CloseDialog();
            return;
        }

        var objId = format.Serial;

        if (objId is > 0 and < int.MaxValue)
        {
            var npcs = ServerSetup.Instance.GlobalMundaneCache.Where(i => i.Key == format.Serial);

            foreach (var npc in npcs)
            {
                if (npc.Value?.Template?.ScriptKey == null) continue;

                var scriptObj = ServerSetup.Instance.GlobalMundaneScriptCache.FirstOrDefault(i => i.Key == npc.Value.Template.Name);
                scriptObj.Value?.OnResponse(this, client, format.Step, format.Input);
                return;
            }
        }

        if (format.ScriptId == ushort.MaxValue)
        {
            if (client.Aisling.ActiveReactor?.Decorators == null)
                return;

            switch (format.Step)
            {
                case 0:
                    foreach (var script in client.Aisling.ActiveReactor.Decorators.Values)
                        script.OnClose(client.Aisling);
                    break;

                case 255:
                    foreach (var script in client.Aisling.ActiveReactor.Decorators.Values)
                        script.OnBack(client.Aisling);
                    break;

                case 0xFFFF:
                    foreach (var script in client.Aisling.ActiveReactor.Decorators.Values)
                        script.OnBack(client.Aisling);
                    break;

                case 2:
                    foreach (var script in client.Aisling.ActiveReactor.Decorators.Values)
                        script.OnClose(client.Aisling);
                    break;

                case 1:
                    foreach (var script in client.Aisling.ActiveReactor.Decorators.Values)
                        script.OnNext(client.Aisling);
                    break;
            }
        }
        else
        {
            client.DlgSession?.Callback?.Invoke(this, client, format.Step, format.Input ?? string.Empty);
        }
    }

    /// <summary>
    /// Request Bulletin Board
    /// </summary>
    protected override void Format3BHandler(GameClient client, ClientFormat3B format)
    {
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (format.Type == 0x01)
        {
            client.Send(new BoardList(ServerSetup.PersonalBoards));
            return;
        }

        if (format.Type == 0x02)
        {
            if (format.BoardIndex == 0)
            {
                var clone = ObjectHandlers.PersonalMailJsonConvert<Board>(ServerSetup.PersonalBoards[format.BoardIndex]);
                {
                    clone.Client = client;
                    client.Send(clone);
                }
                return;
            }

            var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
                .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                .FirstOrDefault();

            if (boards != null)
                client.Send(boards);

            return;
        }

        if (format.Type == 0x03)
        {
            var index = format.TopicIndex - 1;

            var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
                .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                .FirstOrDefault();

            if (boards != null &&
                boards.Posts.Count > index)
            {
                var post = boards.Posts[index];
                if (!post.Read)
                {
                    post.Read = true;
                }

                client.Send(post);
                return;
            }

            client.Send(new ForumCallback("Unable to retrieve more.", 0x06, true));
            return;
        }

        var readyTime = DateTime.Now;
        if (format.Type == 0x06)
        {
            var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
                .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                .FirstOrDefault();

            if (boards == null) return;
            var np = new PostFormat(format.BoardIndex, format.TopicIndex)
            {
                DatePosted = readyTime,
                Message = format.Message,
                Subject = format.Title,
                Read = false,
                Sender = client.Aisling.Username,
                Recipient = format.To,
                PostId = (ushort)(boards.Posts.Count + 1)
            };

            np.Associate(client.Aisling.Username);
            boards.Posts.Add(np);
            ServerSetup.SaveCommunityAssets();
            client.Send(new ForumCallback("Message Delivered.", 0x06, true));
            var recipient = ObjectHandlers.GetAislingForMailDeliveryMessage(Convert.ToString(format.To));

            if (recipient == null) return;
            recipient.Client.SendStats(StatusFlags.UnreadMail);
            recipient.Client.SendMessage(0x03, "{=cYou have new mail.");
            return;
        }

        if (format.Type == 0x04)
        {
            var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
                .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                .FirstOrDefault();

            if (boards == null) return;
            var np = new PostFormat(format.BoardIndex, format.TopicIndex)
            {
                DatePosted = readyTime,
                Message = format.Message,
                Subject = format.Title,
                Read = false,
                Sender = client.Aisling.Username,
                PostId = (ushort)(boards.Posts.Count + 1)
            };

            np.Associate(client.Aisling.Username);

            boards.Posts.Add(np);
            ServerSetup.SaveCommunityAssets();
            client.Send(new ForumCallback("Post Added.", 0x06, true));

            return;
        }

        if (format.Type == 0x05)
        {
            var community = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
                .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                .FirstOrDefault();

            if (community == null || community.Posts.Count <= 0) return;
            try
            {
                if ((format.BoardIndex == 0
                        ? community.Posts[format.TopicIndex - 1].Recipient
                        : community.Posts[format.TopicIndex - 1].Sender
                    ).Equals(client.Aisling.Username, StringComparison.OrdinalIgnoreCase) || client.Aisling.GameMaster)
                {
                    client.Send(new ForumCallback("", 0x07, true));
                    client.Send(new BoardList(ServerSetup.PersonalBoards));
                    client.Send(new ForumCallback("Post Deleted.", 0x07, true));

                    community.Posts.RemoveAt(format.TopicIndex - 1);
                    ServerSetup.SaveCommunityAssets();

                    client.Send(new ForumCallback("Post Deleted.", 0x07, true));
                }
                else
                {
                    client.Send(new ForumCallback(ServerSetup.Instance.Config.CantDoThat, 0x07, true));
                }
            }
            catch (Exception ex)
            {
                ServerSetup.Logger(ex.Message, LogLevel.Error);
                ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
                Crashes.TrackError(ex);
                client.Send(new ForumCallback(ServerSetup.Instance.Config.CantDoThat, 0x07, true));
            }
        }

        if (format.Type != 0x07) return;
        {
            client.Send(client.Aisling.GameMaster == false
                ? new ForumCallback("You cannot perform this action.", 0x07, true)
                : new ForumCallback("Action completed.", 0x07, true));

            if (format.BoardIndex == 0)
            {
                var clone = ObjectHandlers.PersonalMailJsonConvert<Board>(ServerSetup.PersonalBoards[format.BoardIndex]);
                {
                    clone.Client = client;
                    client.Send(clone);
                }
                return;
            }

            var boards = ServerSetup.Instance.GlobalBoardCache.Select(i => i.Value)
                .SelectMany(i => i.Where(n => n.Index == format.BoardIndex))
                .FirstOrDefault();

            if (!client.Aisling.GameMaster) return;
            if (boards == null) return;

            foreach (var ind in boards.Posts.Where(ind => ind.PostId == format.TopicIndex))
            {
                if (ind.HighLighted)
                {
                    ind.HighLighted = false;
                    client.SendMessage(0x08, $"Removed Highlight: {ind.Subject}");
                }
                else
                {
                    ind.HighLighted = true;
                    client.SendMessage(0x08, $"Highlighted: {ind.Subject}");
                }
            }

            client.Send(boards);
        }
    }

    /// <summary>
    /// Skill Use
    /// </summary>
    protected override void Format3EHandler(GameClient client, ClientFormat3E format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (client.Aisling.IsDead()) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (!client.Aisling.CanAttack)
        {
            client.Interrupt();
            return;
        }

        var skill = client.Aisling.SkillBook.GetSkills(i => i.Slot == format.Index).FirstOrDefault();
        if (skill?.Template == null || skill.Scripts == null) return;

        if (!skill.CanUse()) return;

        skill.InUse = true;

        if (skill.ZeroLineTimer.Update(client.Server._abilityGameTimeSpan)) return;
        skill.ZeroLineTimer.Delay = client.Server._abilityGameTimeSpan + TimeSpan.FromMilliseconds(500);

        var script = skill.Scripts.Values.First();
        script?.OnUse(client.Aisling);

        skill.InUse = false;
        skill.CurrentCooldown = skill.Template.Cooldown;
    }

    /// <summary>
    /// World Map Click
    /// </summary>
    protected override void Format3FHandler(GameClient client, ClientFormat3F format)
    {
        if (client.Aisling is not { LoggedIn: true }) return;
        if (client is not { Authenticated: true, MapOpen: true }) return;

        if (ServerSetup.Instance.GlobalWorldMapTemplateCache.TryGetValue(client.Aisling.World, out var worldMap))
        {
            client.PendingNode = worldMap?.Portals.Find(i => i.Destination.AreaID == format.Index);
        }

        TraverseWorldMap(client);
    }

    private static void TraverseWorldMap(GameClient client)
    {
        if (!client.MapOpen) return;
        var selectedPortalNode = client.PendingNode;
        if (selectedPortalNode == null) return;
        client.MapOpen = false;

        for (var i = 0; i < 1; i++)
        {
            client.Aisling.CurrentMapId = selectedPortalNode.Destination.AreaID;
            client.Aisling.Pos = new Vector2(selectedPortalNode.Destination.Location.X, selectedPortalNode.Destination.Location.Y);
            client.Aisling.X = selectedPortalNode.Destination.Location.X;
            client.Aisling.Y = selectedPortalNode.Destination.Location.Y;
            client.TransitionToMap(selectedPortalNode.Destination.AreaID, selectedPortalNode.Destination.Location);
        }

        client.PendingNode = null;
    }

    /// <summary>
    /// On (map, player, monster, npc) Click - F1 Button
    /// </summary>
    protected override void Format43Handler(GameClient client, ClientFormat43 format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;

        foreach (var script in client.Aisling.Map.Scripts.Values)
        {
            script.OnMapClick(client, format.X, format.Y);
        }

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (format.Serial == ServerSetup.Instance.Config.HelperMenuId &&
            ServerSetup.Instance.GlobalMundaneTemplateCache.ContainsKey(ServerSetup.Instance.Config
                .HelperMenuTemplateKey))
        {
            if (!client.Aisling.CanCast || !client.Aisling.CanAttack) return;

            if (format.Type != 0x01) return;

            var helper = new UserHelper(this, new Mundane
            {
                Serial = ServerSetup.Instance.Config.HelperMenuId,
                Template = ServerSetup.Instance.GlobalMundaneTemplateCache[
                    ServerSetup.Instance.Config.HelperMenuTemplateKey]
            });

            helper.OnClick(this, client);
            return;
        }

        if (format.Type != 1) return;
        {
            var isMonster = false;
            var isNpc = false;
            var monsterCheck = ServerSetup.Instance.GlobalMonsterCache.Where(i => i.Key == format.Serial);
            var npcCheck = ServerSetup.Instance.GlobalMundaneCache.Where(i => i.Key == format.Serial);

            foreach (var (_, monster) in monsterCheck)
            {
                if (monster?.Template?.ScriptName == null) continue;
                var scripts = monster.Scripts?.Values;
                if (scripts != null)
                    foreach (var script in scripts)
                        script.OnClick(client);
                isMonster = true;
            }

            if (isMonster) return;

            foreach (var (_, npc) in npcCheck)
            {
                if (npc?.Template?.ScriptKey == null) continue;
                var scripts = npc.Scripts?.Values;
                if (scripts != null)
                    foreach (var script in scripts)
                        script.OnClick(this, client);
                isNpc = true;
            }

            if (isNpc) return;

            var obj = ObjectHandlers.GetObject(client.Aisling.Map, i => i.Serial == format.Serial, ObjectManager.Get.Aislings);
            switch (obj)
            {
                case null:
                    return;
                case Aisling aisling:
                    client.Aisling.Show(Scope.Self, new ServerFormat34(aisling));
                    break;
            }
        }
    }

    /// <summary>
    /// Remove Equipment From Slot
    /// </summary>
    protected override void Format44Handler(GameClient client, ClientFormat44 format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.Dead) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        if (!client.Aisling.CanCast || !client.Aisling.CanAttack) return;

        if (client.Aisling.EquipmentManager.Equipment.ContainsKey(format.Slot))
            client.Aisling.EquipmentManager?.RemoveFromExisting(format.Slot);
    }

    /// <summary>
    /// Client Ping - Heartbeat
    /// </summary>
    protected override void Format45Handler(GameClient client, ClientFormat45 format)
    {
        if (client is not { Authenticated: true }) return;
        if (format.Second != 0x14)
        {
            client.SendMessage(0x02, "Issue with your network, please reconnect.");
            Analytics.TrackEvent($"{client.Aisling.Username} sent ping {format.Second} and was removed.");
            ExitGame(client);
            return;
        }

        CurrentEncryptKey = format.First;
        client.LastPingResponse = format.Ping;
    }

    /// <summary>
    /// Stat Increase Buttons
    /// </summary>
    protected override void Format47Handler(GameClient client, ClientFormat47 format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.IsRefreshing) return;
        CancelIfCasting(client);

        if (!client.Aisling.CanCast || !client.Aisling.CanAttack) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        var attribute = (Stat)format.Stat;

        if (client.Aisling.StatPoints == 0)
        {
            client.SendMessage(0x02, "You do not have any stat points left to use.");
            return;
        }

        if ((attribute & Stat.Str) == Stat.Str)
        {
            if (client.Aisling._Str >= 255)
            {
                client.SendMessage(0x02, "You've maxed this attribute.");
                return;
            }
            client.Aisling._Str++;
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.StrAddedMessage}");
        }

        if ((attribute & Stat.Int) == Stat.Int)
        {
            if (client.Aisling._Int >= 255)
            {
                client.SendMessage(0x02, "You've maxed this attribute.");
                return;
            }
            client.Aisling._Int++;
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.IntAddedMessage}");
        }

        if ((attribute & Stat.Wis) == Stat.Wis)
        {
            if (client.Aisling._Wis >= 255)
            {
                client.SendMessage(0x02, "You've maxed this attribute.");
                return;
            }
            client.Aisling._Wis++;
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.WisAddedMessage}");
        }

        if ((attribute & Stat.Con) == Stat.Con)
        {
            if (client.Aisling._Con >= 255)
            {
                client.SendMessage(0x02, "You've maxed this attribute.");
                return;
            }
            client.Aisling._Con++;
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.ConAddedMessage}");
        }

        if ((attribute & Stat.Dex) == Stat.Dex)
        {
            if (client.Aisling._Dex >= 255)
            {
                client.SendMessage(0x02, "You've maxed this attribute.");
                return;
            }
            client.Aisling._Dex++;
            client.SendMessage(0x02, $"{ServerSetup.Instance.Config.DexAddedMessage}");
        }

        if (client.Aisling._Wis > ServerSetup.Instance.Config.StatCap)
            client.Aisling._Wis = ServerSetup.Instance.Config.StatCap;
        if (client.Aisling._Str > ServerSetup.Instance.Config.StatCap)
            client.Aisling._Str = ServerSetup.Instance.Config.StatCap;
        if (client.Aisling._Int > ServerSetup.Instance.Config.StatCap)
            client.Aisling._Int = ServerSetup.Instance.Config.StatCap;
        if (client.Aisling._Con > ServerSetup.Instance.Config.StatCap)
            client.Aisling._Con = ServerSetup.Instance.Config.StatCap;
        if (client.Aisling._Dex > ServerSetup.Instance.Config.StatCap)
            client.Aisling._Dex = ServerSetup.Instance.Config.StatCap;

        if (client.Aisling._Wis <= 0)
            client.Aisling._Wis = 0;
        if (client.Aisling._Str <= 0)
            client.Aisling._Str = 0;
        if (client.Aisling._Int <= 0)
            client.Aisling._Int = 0;
        if (client.Aisling._Con <= 0)
            client.Aisling._Con = 0;
        if (client.Aisling._Dex <= 0)
            client.Aisling._Dex = 0;

        if (!client.Aisling.GameMaster)
            client.Aisling.StatPoints--;

        if (client.Aisling.StatPoints < 0)
            client.Aisling.StatPoints = 0;

        client.Aisling.Show(Scope.Self, new ServerFormat08(client.Aisling, StatusFlags.StructA));
    }

    /// <summary>
    /// Client Trading
    /// </summary>
    protected override void Format4AHandler(GameClient client, ClientFormat4A format)
    {
        if (format == null) return;
        if (client == null || !client.Aisling.LoggedIn) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;

        if (client.Aisling.Skulled)
        {
            client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
            client.Interrupt();
            return;
        }

        var trader = ObjectHandlers.GetObject<Aisling>(client.Aisling.Map, i => i.Serial.Equals((int)format.Id));
        var player = client.Aisling;

        if (player == null || trader == null) return;
        if (!player.WithinRangeOf(trader)) return;

        switch (format.Type)
        {
            case 0x00:
                {
                    if (player.ThrewHealingPot) break;

                    player.Exchange = new ExchangeSession(trader);
                    trader.Exchange = new ExchangeSession(player);

                    var packet = new NetworkPacketWriter();
                    packet.Write((byte)0x42);
                    packet.Write((byte)0x00);
                    packet.Write((byte)0x00);
                    packet.Write((uint)trader.Serial);
                    packet.WriteStringA(trader.Username);
                    client.Send(packet);

                    packet = new NetworkPacketWriter();
                    packet.Write((byte)0x42);
                    packet.Write((byte)0x00);
                    packet.Write((byte)0x00);
                    packet.Write((uint)player.Serial);
                    packet.WriteStringA(player.Username);
                    trader.Client.Send(packet);
                }
                break;
            case 0x01:
                {
                    if (player.ThrewHealingPot)
                    {
                        player.ThrewHealingPot = false;
                        break;
                    }

                    var item = client.Aisling.Inventory.Items[format.ItemSlot];

                    if (!item.Template.Flags.FlagIsSet(ItemFlags.Tradeable))
                    {
                        player.Client.SendMessage(0x03, "That item is not tradeable");
                        return;
                    }

                    if (player.Exchange == null) return;
                    if (trader.Exchange == null) return;
                    if (player.Exchange.Trader != trader) return;
                    if (trader.Exchange.Trader != player) return;
                    if (player.Exchange.Confirmed) return;
                    if (item?.Template == null) return;

                    if (trader.CurrentWeight + item.Template.CarryWeight < trader.MaximumWeight)
                    {
                        if (player.EquipmentManager.RemoveFromInventory(item, true))
                        {
                            player.Exchange.Items.Add(item);
                            player.Exchange.Weight += item.Template.CarryWeight;
                        }

                        var packet = new NetworkPacketWriter();
                        packet.Write((byte)0x42);
                        packet.Write((byte)0x00);

                        packet.Write((byte)0x02);
                        packet.Write((byte)0x00);
                        packet.Write((byte)player.Exchange.Items.Count);
                        packet.Write(item.DisplayImage);
                        packet.Write(item.Color);
                        packet.WriteStringA(item.NoColorDisplayName);
                        client.Send(packet);

                        packet = new NetworkPacketWriter();
                        packet.Write((byte)0x42);
                        packet.Write((byte)0x00);

                        packet.Write((byte)0x02);
                        packet.Write((byte)0x01);
                        packet.Write((byte)player.Exchange.Items.Count);
                        packet.Write(item.DisplayImage);
                        packet.Write(item.Color);
                        packet.WriteStringA(item.NoColorDisplayName);
                        trader.Client.Send(packet);
                    }
                    else
                    {
                        var packet = new NetworkPacketWriter();
                        packet.Write((byte)0x42);
                        packet.Write((byte)0x00);

                        packet.Write((byte)0x04);
                        packet.Write((byte)0x00);
                        packet.WriteStringA("They can't seem to lift that. The trade has been cancelled.");
                        client.Send(packet);

                        packet = new NetworkPacketWriter();
                        packet.Write((byte)0x42);
                        packet.Write((byte)0x00);

                        packet.Write((byte)0x04);
                        packet.Write((byte)0x01);
                        packet.WriteStringA("That item seems to be too heavy. The trade has been cancelled.");
                        trader.Client.Send(packet);
                        player.CancelExchange();
                    }
                }
                break;
            case 0x02:
                break;
            case 0x03:
                {
                    if (player.Exchange == null) return;
                    if (trader.Exchange == null) return;
                    if (player.Exchange.Trader != trader) return;
                    if (trader.Exchange.Trader != player) return;
                    if (player.Exchange.Confirmed) return;
                    if (player.Exchange.Gold != 0) return;

                    var gold = format.Gold;

                    if (gold > player.GoldPoints)
                    {
                        player.Client.SendMessage(0x03, "You don't have that much.");
                        return;
                    }

                    if (trader.GoldPoints + gold > ServerSetup.Instance.Config.MaxCarryGold)
                    {
                        player.Client.SendMessage(0x03, "Player cannot hold that amount.");
                        return;
                    }

                    player.GoldPoints -= gold;
                    player.Exchange.Gold = gold;
                    player.Client.SendStats(StatusFlags.StructC);

                    var packet = new NetworkPacketWriter();
                    packet.Write((byte)0x42);
                    packet.Write((byte)0x00);

                    packet.Write((byte)0x03);
                    packet.Write((byte)0x00);
                    packet.Write(gold);
                    client.Send(packet);

                    packet = new NetworkPacketWriter();
                    packet.Write((byte)0x42);
                    packet.Write((byte)0x00);

                    packet.Write((byte)0x03);
                    packet.Write((byte)0x01);
                    packet.Write(gold);
                    trader.Client.Send(packet);
                }
                break;
            case 0x04:
                {
                    if (player.Exchange == null) return;
                    if (trader.Exchange == null) return;
                    if (player.Exchange.Trader != trader) return;
                    if (trader.Exchange.Trader != player) return;

                    player.CancelExchange();
                }
                break;

            case 0x05:
                {
                    if (player.Exchange == null) return;
                    if (trader.Exchange == null) return;
                    if (player.Exchange.Trader != trader) return;
                    if (trader.Exchange.Trader != player) return;
                    if (player.Exchange.Confirmed) return;

                    player.Exchange.Confirmed = true;

                    if (trader.Exchange.Confirmed)
                        player.FinishExchange();

                    var packet = new NetworkPacketWriter();
                    packet.Write((byte)0x42);
                    packet.Write((byte)0x00);

                    packet.Write((byte)0x05);
                    packet.Write((byte)0x00);
                    packet.WriteStringA("Trade was completed.");
                    client.Send(packet);

                    packet = new NetworkPacketWriter();
                    packet.Write((byte)0x42);
                    packet.Write((byte)0x00);

                    packet.Write((byte)0x05);
                    packet.Write((byte)0x01);
                    packet.WriteStringA("Trade was completed.");
                    trader.Client.Send(packet);
                }
                break;
        }
    }

    /// <summary>
    /// Begin Casting - Spell Lines
    /// </summary>
    protected override void Format4DHandler(GameClient client, ClientFormat4D format)
    {
        if (client?.Aisling == null) return;
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsDead()) return;

        client.Aisling.IsCastingSpell = true;

        var lines = format.Lines;

        if (lines <= 0)
        {
            CancelIfCasting(client);
            return;
        }

        if (!client.CastStack.Any()) return;
        var info = client.CastStack.Peek();

        if (info == null) return;
        info.SpellLines = lines;
        info.Started = DateTime.Now;
    }

    /// <summary>
    /// Skill / Spell Lines - Chant Message
    /// </summary>
    protected override void Format4EHandler(GameClient client, ClientFormat4E format)
    {
        if (client?.Aisling == null) return;
        if (client is not { Authenticated: true }) return;
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsDead()) return;

        var chant = format.Message;
        var subject = chant.IndexOf(" Lev", StringComparison.Ordinal);

        if (subject > 0)
        {
            if (chant.Length <= subject) return;

            if (chant.Length > subject)
            {
                client.Say(chant.Trim());
            }

            return;
        }

        client.Say(chant, 0x02);
    }

    /// <summary>
    /// Player Portrait & Profile Message
    /// </summary>
    protected override void Format4FHandler(GameClient client, ClientFormat4F format)
    {
        if (client is not ({ Authenticated: true } and { EncryptPass: true })) return;

        client.Aisling.ProfileMessage = format.Words;
        client.Aisling.PictureData = format.Image;
    }

    /// <summary>
    /// Client Tick Sync
    /// </summary>
    protected override void Format75Handler(GameClient client, ClientFormat75 format)
    {
        Console.Write($"Format75HandlerDiscovery: {format.ClientTick} - {format.ServerTick}\n");
    }

    /// <summary>
    /// Player Social Status
    /// </summary>
    protected override void Format79Handler(GameClient client, ClientFormat79 format)
    {
        if (client is not { Authenticated: true }) return;
        client.Aisling.ActiveStatus = format.Status;
    }

    /// <summary>
    /// Client Metafile Request
    /// </summary>
    protected override void Format7BHandler(GameClient client, ClientFormat7B format)
    {
        if (client is not { Authenticated: true }) return;

        switch (format.Type)
        {
            case 0x00:
                {
                    if (!format.Name.Contains("Class"))
                    {
                        client.Send(new ServerFormat6F
                        {
                            Type = 0x00,
                            Name = format.Name
                        });
                        break;
                    }

                    var name = DecideOnSkillsToPull(client);

                    client.Send(new ServerFormat6F
                    {
                        Client = client,
                        Type = 0x00,
                        Name = name
                    });
                }
                break;
            case 0x01:
                client.Send(new ServerFormat6F
                {
                    Type = 0x01
                });
                break;
        }
    }

    private static string DecideOnSkillsToPull(IGameClient client)
    {
        if (client.Aisling == null) return null;
        return _skillMap.TryGetValue((client.Aisling.Race, client.Aisling.Path, client.Aisling.PastClass), out var skillCode) ? skillCode : null;
    }

    /// <summary>
    /// Display Mask
    /// </summary>
    protected override void Format89Handler(GameClient client, ClientFormat89 format)
    {
        Console.Write($"Format89HandlerDiscovery\n");
    }

    #endregion

    public static void CancelIfCasting(GameClient client)
    {
        if (!client.Aisling.LoggedIn) return;
        if (client.Aisling.IsCastingSpell)
            client.Send(new ServerFormat48());

        client.CastStack.Clear();
        client.Aisling.IsCastingSpell = false;
    }

    private static void ExecuteAbility(IGameClient lpClient, Skill lpSkill, bool optExecuteScript = true)
    {
        if (lpSkill.Template.ScriptName == "Assail")
        {
            // Uses a script equipped to the main-hand item if there is one
            var itemScripts = lpClient.Aisling.EquipmentManager.Equipment[1]?.Item?.WeaponScripts;

            if (itemScripts != null)
                foreach (var itemScript in itemScripts.Values.Where(itemScript => itemScript != null))
                    itemScript.OnUse(lpClient.Aisling);
        }

        if (!optExecuteScript) return;
        var script = lpSkill.Scripts.Values.First();
        script?.OnUse(lpClient.Aisling);
    }

    public static void CheckWarpTransitions(GameClient client)
    {
        foreach (var (_, value) in ServerSetup.Instance.GlobalWarpTemplateCache)
        {
            var breakOuterLoop = false;
            if (value.ActivationMapId != client.Aisling.CurrentMapId) continue;

            lock (ServerSetup.SyncLock)
            {
                foreach (var _ in value.Activations.Where(o =>
                             o.Location.X == (int)client.Aisling.Pos.X &&
                             o.Location.Y == (int)client.Aisling.Pos.Y))
                {
                    if (value.WarpType == WarpType.Map)
                    {
                        client.WarpToAdjacentMap(value);
                        breakOuterLoop = true;
                        break;
                    }

                    if (value.WarpType != WarpType.World) continue;
                    if (!ServerSetup.Instance.GlobalWorldMapTemplateCache.ContainsKey(value.To.PortalKey)) return;
                    if (client.Aisling.World != value.To.PortalKey) client.Aisling.World = value.To.PortalKey;

                    var portal = new PortalSession();
                    portal.TransitionToMap(client);
                    breakOuterLoop = true;
                    break;
                }
            }

            if (breakOuterLoop) break;
        }
    }

    public static void CheckWarpTransitions(GameClient client, int x, int y)
    {
        foreach (var (_, value) in ServerSetup.Instance.GlobalWarpTemplateCache)
        {
            var breakOuterLoop = false;
            if (value.ActivationMapId != client.Aisling.CurrentMapId) continue;

            lock (ServerSetup.SyncLock)
            {
                foreach (var _ in value.Activations.Where(o =>
                             o.Location.X == x &&
                             o.Location.Y == y))
                {
                    if (value.WarpType == WarpType.Map)
                    {
                        client.WarpToAdjacentMap(value);
                        breakOuterLoop = true;
                        client.Interrupt();
                        break;
                    }

                    if (value.WarpType != WarpType.World) continue;
                    if (!ServerSetup.Instance.GlobalWorldMapTemplateCache.ContainsKey(value.To.PortalKey)) return;
                    if (client.Aisling.World != value.To.PortalKey) client.Aisling.World = value.To.PortalKey;

                    var portal = new PortalSession();
                    portal.TransitionToMap(client);
                    breakOuterLoop = true;
                    client.Interrupt();
                    break;
                }
            }

            if (breakOuterLoop) break;
        }
    }

    private void RemoveFromServer(GameClient client, byte type = 0)
    {
        if (client == null) return;

        try
        {
            if (client.Server == null) return;
            if (client.Server.Clients.IsEmpty) return;
            if (client.Server.Clients.Values.Count == 0) return;
            if (!client.Server.Clients.Values.Contains(client)) return;

            if (type == 0)
            {
                ExitGame(client);
                return;
            }

            client.CloseDialog();
            if (client.Aisling == null) return;
            client.Aisling.CancelExchange();

            client.DlgSession = null;
            client.Aisling.LastLogged = DateTime.Now;
            client.Aisling.ActiveReactor = null;
            client.Aisling.ActiveSequence = null;
            client.Aisling.Remove(true);
            client.Aisling.LoggedIn = false;
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
        }
    }

    private async void ExitGame(GameClient client)
    {
        if (client.Aisling is null) return;
        var nameSeed = $"{client.Aisling.Username.ToLower()}{client.Aisling.Serial}";
        var redirect = new Redirect
        {
            Serial = Convert.ToString(client.Serial, CultureInfo.CurrentCulture),
            Salt = Encoding.UTF8.GetString(client.Encryption.Parameters.Salt),
            Seed = Convert.ToString(client.Encryption.Parameters.Seed, CultureInfo.CurrentCulture),
            Name = nameSeed,
            Type = "2"
        };

        client.Aisling.LoggedIn = false;
        await client.Save();

        if (ServerSetup.Redirects.ContainsKey(client.Aisling.Serial))
            ServerSetup.Redirects.TryRemove(client.Aisling.Serial, out _);

        client.Send(new ServerFormat03
        {
            CalledFromMethod = true,
            EndPoint = new IPEndPoint(Address, 2610),
            Redirect = redirect
        });

        client.Send(new ServerFormat02(0x00, ""));
        RemoveClient(client);
        ServerSetup.Logger($"{client.Aisling.Username} either logged out or was removed from the server.");
        client.Dispose();
    }

    public override void ClientDisconnected(GameClient client)
    {
        if (client == null) return;
        if (client.Aisling?.GroupId != 0)
            Party.RemovePartyMember(client.Aisling);
        RemoveFromServer(client, 1);
        RemoveFromServer(client);
    }

    private Task<Aisling> LoadPlayer(GameClient client, string player, uint serial)
    {
        if (client == null) return null;
        if (player.IsNullOrEmpty()) return null;

        var aisling = StorageManager.AislingBucket.LoadAisling(player, serial);

        if (aisling.Result == null) return null;
        client.Aisling = aisling.Result;
        client.Aisling.Serial = aisling.Result.Serial;
        client.Aisling.Pos = new Vector2(aisling.Result.X, aisling.Result.Y);
        client.Aisling.Client = client;

        if (client.Aisling == null)
        {
            client.SendMessage(0x02, "Something happened. Please report this bug to Zolian staff.");
            Analytics.TrackEvent($"{player} failed to load character.");
            base.ClientDisconnected(client);
            RemoveClient(client);
            return null;
        }

        if (client.Aisling._Str <= 0 || client.Aisling._Int <= 0 || client.Aisling._Wis <= 0 || client.Aisling._Con <= 0 || client.Aisling._Dex <= 0)
        {
            client.SendMessage(0x02, "Your stats have has been corrupted. Please report this bug to Zolian staff.");
            Analytics.TrackEvent($"{player} has corrupted stats.");
            base.ClientDisconnected(client);
            RemoveClient(client);
            return null;
        }

        return CleanUpLoadPlayer(client);
    }

    private static async Task<Aisling> CleanUpLoadPlayer(GameClient client)
    {
        CheckOnLoad(client);

        if (client.Aisling.Map != null) client.Aisling.CurrentMapId = client.Aisling.Map.ID;
        client.Aisling.LoggedIn = false;
        client.Aisling.EquipmentManager.Client = client;
        client.Aisling.CurrentWeight = 0;
        client.Aisling.ActiveStatus = ActivityStatus.Awake;

        var ip = client.Socket.RemoteEndPoint as IPEndPoint;
        Analytics.TrackEvent($"{ip!.Address} logged in {client.Aisling.Username}");
        client.FlushAfterCleanup();
        await client.Load();
        client.SendMessage(0x02, $"{ServerSetup.Instance.Config.ServerWelcomeMessage}: {client.Aisling.Username}");
        client.SendStats(StatusFlags.All);
        client.LoggedIn(true);

        if (client.Aisling == null) return null;

        if (!client.Aisling.Dead) return client.Aisling;
        client.Aisling.Flags = AislingFlags.Ghost;
        client.Aisling.WarpToHell();

        return client.Aisling;
    }

    private static void CheckOnLoad(IGameClient client)
    {
        var aisling = client.Aisling;

        aisling.SkillBook ??= new SkillBook();
        aisling.SpellBook ??= new SpellBook();
        aisling.Inventory ??= new Inventory();
        aisling.BankManager ??= new Bank();
        aisling.EquipmentManager ??= new EquipmentManager(aisling.Client);
        aisling.QuestManager ??= new Quests();
    }
}