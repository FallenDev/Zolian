using Chaos.Cryptography;
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
    public ServerPacketLogger ServerPacketLogger { get; } = new();
    public ClientPacketLogger ClientPacketLogger { get; } = new();
    private readonly MServerTable _serverTable;
    private static readonly string[] GameMastersIPs = ServerSetup.Instance.GameMastersIPs;
    public readonly MetafileManager MetafileManager;
    public FrozenDictionary<int, Metafile> Metafiles { get; set; }
    private ConcurrentDictionary<Type, WorldServerComponent> _serverComponents;
    private readonly WorldServerTimer _trapTimer = new(TimeSpan.FromSeconds(1));
    private const int GameSpeed = 50;

    // Subsystem intervals (ms)
    private const int MonstersIntervalMs = 250;    // 4x per second
    private const int MundanesIntervalMs = 1500;   // 1.5 seconds
    private const int GroundItemsIntervalMs = 60000;  // 60 seconds
    private const int GroundMoneyIntervalMs = 60000;  // 60 seconds

    // Accumulators for fixed-step scheduling
    private double _monsterAccumulatorMs;
    private double _mundaneAccumulatorMs;
    private double _groundItemsAccumulatorMs;
    private double _groundMoneyAccumulatorMs;

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
        SClassDictionary.SkillMapper();
        RegisterServerComponents();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Server is now Online\n");
        MetafileManager = new MetafileManager();
    }

    #region Server Init

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start base server 
        _ = base.ExecuteAsync(stoppingToken);
        
        try
        {
            ServerSetup.Instance.Running = true;
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            UpdateComponentsRoutine(linkedCts.Token);

            // Fixed-step world loop
            var sw = Stopwatch.StartNew();
            const double fixedStepMs = GameSpeed;
            var lastTimeMs = sw.Elapsed.TotalMilliseconds;
            double accumulatorMs = 0;

            while (!linkedCts.IsCancellationRequested && ServerSetup.Instance.Running)
            {
                var nowMs = sw.Elapsed.TotalMilliseconds;
                var deltaMs = nowMs - lastTimeMs;

                // Clamp delta to avoid spiral-of-death
                if (deltaMs < 0) deltaMs = 0;
                if (deltaMs > 200) deltaMs = 200;

                lastTimeMs = nowMs;
                accumulatorMs += deltaMs;

                // Catch-up: run one or more fixed steps if we fell behind
                while (accumulatorMs >= fixedStepMs)
                {
                    accumulatorMs -= fixedStepMs;
                    TickWorld(fixedStepMs, linkedCts.Token);
                }

                // Prevent busy waiting
                await Task.Delay(1, linkedCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            ServerSetup.ConnectionLogger(ex.Message, LogLevel.Error);
            ServerSetup.ConnectionLogger(ex.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(ex);
        }
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

    #endregion

    #region Server Loop

    private void TickWorld(double dtMs, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || !ServerSetup.Instance.Running)
            return;

        var dt = TimeSpan.FromMilliseconds(dtMs);

        try
        {
            // 1. Players – every 50 ms
            UpdateClients();

            // 2. High-frequency systems – every 50 ms
            UpdateMaps(dt);
            CheckTraps(dt);

            // 3. Monsters – every 250 ms
            _monsterAccumulatorMs += dtMs;
            while (_monsterAccumulatorMs >= MonstersIntervalMs)
            {
                UpdateMonsters(TimeSpan.FromMilliseconds(MonstersIntervalMs));
                _monsterAccumulatorMs -= MonstersIntervalMs;
            }

            // 4. Mundanes – every 1500 ms
            _mundaneAccumulatorMs += dtMs;
            while (_mundaneAccumulatorMs >= MundanesIntervalMs)
            {
                UpdateMundanes(TimeSpan.FromMilliseconds(MundanesIntervalMs));
                _mundaneAccumulatorMs -= MundanesIntervalMs;
            }

            // 5. Ground items – every 60 seconds
            _groundItemsAccumulatorMs += dtMs;
            while (_groundItemsAccumulatorMs >= GroundItemsIntervalMs)
            {
                UpdateGroundItems(); // already has its own try/catch
                _groundItemsAccumulatorMs -= GroundItemsIntervalMs;
            }

            // 6. Ground money – every 60 seconds
            _groundMoneyAccumulatorMs += dtMs;
            while (_groundMoneyAccumulatorMs >= GroundMoneyIntervalMs)
            {
                UpdateGroundMoney(); // already has its own try/catch
                _groundMoneyAccumulatorMs -= GroundMoneyIntervalMs;
            }
        }
        catch (Exception ex)
        {
            // World tick is NEVER allowed to die – log and keep going
            SentrySdk.CaptureException(ex);
        }
    }

    private void UpdateComponentsRoutine(CancellationToken ct)
    {
        foreach (var component in _serverComponents.Values)
            Task.Factory.StartNew(() => component.Update(), TaskCreationOptions.LongRunning);
    }

    private static class PeriodicTaskRunner
    {
        public static async Task RunPeriodicAsync(Func<CancellationToken, Task> action, int intervalMs, CancellationToken ct)
        {
            var sw = new Stopwatch();
            while (!ct.IsCancellationRequested)
            {
                sw.Restart();
                await action(ct);

                var elapsed = (int)sw.ElapsedMilliseconds;
                var delay = intervalMs - elapsed;
                if (delay > 0)
                    await Task.Delay(delay, ct);
            }
        }
    }

    private void UpdateClients()
    {
        // Snapshot to avoid concurrent modifications
        var players = Aislings.Where(p => p?.Client != null).ToList();
        if (players.Count == 0)
            return;

        Parallel.ForEach(
            players,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            ProcessClientSafely);
    }

    private void ProcessClientSafely(Aisling player)
    {
        if (player?.Client == null) return;

        try
        {
            if (!player.LoggedIn)
            {
                ClientRegistry.TryRemove(player.Client.Id, out _);
                return;
            }

            var updateTask = player.Client.Update();
            if (!updateTask.IsCompleted)
                updateTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);

            try
            {
                player.Client.Disconnect();
            }
            catch
            {
                // ignore secondary failures
            }

            ClientRegistry.TryRemove(player.Client.Id, out _);
        }
    }

    private static void UpdateGroundItems()
    {
        try
        {
            // Flatten all ground items from every map area into a single list
            var items = ServerSetup.Instance.GlobalMapCache.Values
                .SelectMany(area => ObjectManager.GetObjects<Item>(area, i => i.ItemPane == Item.ItemPanes.Ground))
                .ToList();

            // Process each item in parallel using PLINQ
            items.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var abandonedDiff = DateTime.UtcNow - item.Value.AbandonedDate;

                    // For corpses: remove if abandoned for more than 3 minutes
                    if (abandonedDiff.TotalMinutes > 3 && item.Value.Template.Name == "Corpse")
                        item.Value.Remove();

                    // For all items: remove if abandoned for more than 30 minutes
                    if (abandonedDiff.TotalMinutes > 30)
                        item.Value.Remove();
                });
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
            // Flatten all money drops into a list
            var moneyDrops = ServerSetup.Instance.GlobalGroundMoneyCache.Values.ToList();

            // Process each money drop in parallel
            moneyDrops.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(money =>
                {
                    var abandonedDiff = DateTime.UtcNow - money.AbandonedDate;

                    // Remove money drops abandoned for more than 30 minutes
                    if (!(abandonedDiff.TotalMinutes > 30)) return;
                    if (ServerSetup.Instance.GlobalGroundMoneyCache.TryRemove(money.MoneyId, out _))
                    {
                        money.Remove();
                    }
                });
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
            var monsters = ServerSetup.Instance.GlobalMapCache.Values
                .SelectMany(area => ObjectManager.GetObjects<Monster>(area, i => !i.Skulled).Values);

            monsters.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(monster => ProcessMonster(monster, elapsedTime));
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void ProcessMonster(Monster monster, TimeSpan elapsedTime)
    {
        if (monster.CurrentHp <= 0)
        {
            monster.Skulled = true;

            // Handle OnDeath logic
            if (monster.Target is Aisling aisling)
            {
                monster.Scripts?.Values.FirstOrDefault()?.OnDeath(aisling.Client);
            }
            else
            {
                monster.Scripts?.Values.FirstOrDefault()?.OnDeath();
            }

            return;
        }

        // Update monster scripts
        monster.Scripts?.Values.FirstOrDefault()?.Update(elapsedTime);
        monster.LastUpdated = DateTime.UtcNow;

        // Handle buffs and debuffs
        if (!monster.MonsterBuffAndDebuffStopWatch.IsRunning)
            monster.MonsterBuffAndDebuffStopWatch.Start();

        if (!(monster.MonsterBuffAndDebuffStopWatch.Elapsed.TotalMilliseconds >= 1000)) return;
        monster.UpdateBuffs(monster);
        monster.UpdateDebuffs(monster);
        monster.MonsterBuffAndDebuffStopWatch.Restart();
    }

    private static void UpdateMundanes(TimeSpan elapsedTime)
    {
        try
        {
            var mundanes = ServerSetup.Instance.GlobalMapCache.Values
                .SelectMany(area => ObjectManager.GetObjects<Mundane>(area, mundane => mundane != null).Values);

            mundanes.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(mundane => ProcessMundane(mundane, elapsedTime));
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private static void ProcessMundane(Mundane mundane, TimeSpan elapsedTime)
    {
        if (mundane == null) return;
        mundane.Update(elapsedTime);
        mundane.LastUpdated = DateTime.UtcNow;
    }

    private void CheckTraps(TimeSpan elapsedTime)
    {
        if (!_trapTimer.Update(elapsedTime)) return;

        try
        {
            foreach (var trap in ServerSetup.Instance.Traps.Values) { trap?.Update(); }
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
            // Process each area in parallel directly
            Parallel.ForEach(
                ServerSetup.Instance.GlobalMapCache.Values,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                area => area?.Update(elapsedTime));
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
            var itemObjs = ObjectManager.GetObjects<Item>(map, i => (int)i.Pos.X == localArgs.SourcePoint.X && (int)i.Pos.Y == localArgs.SourcePoint.Y).Values.Where(i => !i.Template.Flags.FlagIsSet(ItemFlags.Trap)).ToList();
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
                    item = itemObjs[^1];
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

            if (localClient.Aisling.Map.IsWall(localClient.Aisling, localArgs.DestinationPoint.X, localArgs.DestinationPoint.Y))
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
                var slurred = Generator.RandomPercentPrecise();
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

                client.SendServerMessage(ServerMessageType.ActiveMessage, $"{ServerSetup.Instance.Config.ServerWelcomeMessage}: {client.Aisling.Username}");
                client.SendAttributes(StatUpdateType.Full);
                client.LoggedIn(true);

                if (client.Aisling.Map != null && client.Aisling.IsDead())
                {
                    client.AislingToGhostForm();
                    if (!client.Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
                        client.Aisling.WarpToHell();
                }

                if (client.Aisling.AreaId == ServerSetup.Instance.Config.TransitionZone)
                    PortalSession.TransitionToMap(client.Aisling.Client);
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
                var slurred = Generator.RandomPercentPrecise();
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

            // Run Scripts on item on use
            if (!string.IsNullOrEmpty(item.Template.ScriptName)) item.Scripts ??= ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);
            if (!string.IsNullOrEmpty(item.Template.WeaponScript)) item.WeaponScripts ??= ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);

            if (!item.GiftWrapped.IsNullOrEmpty())
            {
                var consumeScript = ScriptManager.Load<ItemScript>("Consumable", item);
                var script = consumeScript.Values.FirstOrDefault();
                script?.OnUse(localClient.Aisling, localArgs.SourceSlot);
                return default;
            }

            if (item.Template.Flags.FlagIsSet(ItemFlags.Equipable))
                localClient.LastEquip = DateTime.UtcNow;

            var activated = false;

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
            if (item.Template.Name is "Chakra Stone" or "Cleric's Feather")
            {
                localClient.SendServerMessage(ServerMessageType.ActiveMessage, "I can't use this in that way.");
                return default;
            }

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
            var listA = ObjectManager.GetObjects<Monster>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).Values.ToList();
            var listB = ObjectManager.GetObjects<Mundane>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).Values.ToList();
            var listC = ObjectManager.GetObjects<Aisling>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).Values.ToList();
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
            var listA = ObjectManager.GetObjects<Monster>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).Values.ToList();
            var listB = ObjectManager.GetObjects<Mundane>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).Values.ToList();
            var listC = ObjectManager.GetObjects<Aisling>(localClient.Aisling.Map, i => i != null && i.WithinRangeOf(localClient.Aisling, ServerSetup.Instance.Config.WithinRangeProximity)).Values.ToList();

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

            if (skill.Template.Cooldown == 0)
                if (!skill.CanUseZeroLineAbility) return default;
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
    /// 0x42 - Client Exception reported to the Server
    /// </summary>
    /// <returns>Prints details of the packets leading up to the crash</returns>
    public ValueTask OnClientException(IWorldClient client, in Packet packet)
    {
        var args = PacketSerializer.Deserialize<ClientExceptionArgs>(in packet);
        ServerSetup.EventsLogger($"{client.RemoteIp} encountered an exception: {args.ExceptionStr} - See packetLogger for details", LogLevel.Critical);
        DisplayRecentServerPacketLogs(client);
        DisplayRecentClientPacketLogs(client);
        return default;
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
                        {
                            if (localArgs.Name is null) return default;
                            if (!localArgs.Name.Contains("Class"))
                            {
                                localClient.SendMetaData(localArgs.MetaDataRequestType, MetafileManager, localArgs.Name);
                            }
                        }
                        break;
                    case MetaDataRequestType.AllCheckSums:
                        {
                            localClient.SendMetaData(MetaDataRequestType.AllCheckSums, MetafileManager);
                        }
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

    #endregion

    #region Connection / Handler

    public override ValueTask HandlePacketAsync(IWorldClient client, in Packet packet)
    {
        var opCode = packet.OpCode;
        var handler = ClientHandlers[packet.OpCode];

        if (client.Aisling is not null && IsManualAction((ClientOpCode)opCode))
            client.Aisling.AislingTracker = DateTime.UtcNow;

        // ToDo: Packet logging
        //ServerSetup.ConnectionLogger($"Server: {packet.OpCode}");

        try
        {
            if (handler is not null)
            {
                ClientPacketLogger.LogPacket(client.RemoteIp, $"{client.Aisling?.Username ?? client.RemoteIp.ToString()} with Client OpCode: {opCode} ({Enum.GetName(typeof(ClientOpCode), opCode)})");
                return handler(client, in packet);
            }

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
        ClientHandlers[(byte)ClientOpCode.ClientException] = OnClientException; // 0x42
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
            var isBadActor = Task.Run(() => BadActor.ClientOnBlackListAsync(ipAddress.ToString())).Result;

            if (isBadActor)
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
            Task.Run(() => BadActor.ReportMaliciousEndpoint(ipAddress.ToString(), comment));
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

    private void DisplayRecentClientPacketLogs(IWorldClient client)
    {
        var logs = ClientPacketLogger.GetRecentLogs(client.RemoteIp).ToList();

        foreach (var log in logs)
            ServerSetup.PacketLogger(log);
    }

    private void DisplayRecentServerPacketLogs(IWorldClient client)
    {
        var logs = ServerPacketLogger.GetRecentLogs(client.RemoteIp).ToList();

        foreach (var log in logs)
            ServerSetup.PacketLogger(log);
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