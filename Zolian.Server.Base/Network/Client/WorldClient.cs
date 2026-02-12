using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;

using Chaos.Cryptography.Abstractions;
using Chaos.Extensions.Networking;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Dapper;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Events;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Formulas;
using Darkages.Managers;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Client.Coalescer;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Templates;
using Darkages.Types;

using JetBrains.Annotations;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using ServiceStack;

using BodyColor = Chaos.DarkAges.Definitions.BodyColor;
using BodySprite = Chaos.DarkAges.Definitions.BodySprite;
using EquipmentSlot = Chaos.DarkAges.Definitions.EquipmentSlot;
using Gender = Chaos.DarkAges.Definitions.Gender;
using IWorldClient = Darkages.Network.Client.Abstractions.IWorldClient;
using LanternSize = Chaos.DarkAges.Definitions.LanternSize;
using MapFlags = Darkages.Enums.MapFlags;
using Nation = Chaos.DarkAges.Definitions.Nation;
using RestPosition = Chaos.DarkAges.Definitions.RestPosition;

namespace Darkages.Network.Client;

[UsedImplicitly]
public class WorldClient : WorldClientBase, IWorldClient
{
    private readonly IWorldServer<WorldClient> _server;
    private readonly SoundCoalescer _soundCoalescer;
    private readonly HealthBarCoalescer _healthBarCoalescer;
    private readonly BodyAnimationCoalescer _bodyAnimationCoalescer;
    private int _updateInFlight;

    public readonly WorldServerTimer SkillSpellTimer = new(TimeSpan.FromMilliseconds(1000));
    public readonly Stopwatch CooldownControl = new();
    public readonly Stopwatch SpellControl = new();
    public readonly Lock SyncModifierRemovalLock = new();
    public Spell LastSpell = new();
    public bool ExitConfirmed;

    // Ping/Pong RTT
    public int HeartBeatInFlight;
    public long HeartBeatStartTimestamp;
    public int HeartBeatIdx;
    public int HeartBeatCount;
    public readonly int[] HeartBeatSamplesMs = new int[3];
    public int LastRttMs { get; set; }
    public int RollingRtt15sMs { get; set; }
    public int SmoothedRttMs { get; set; }

    private readonly Stopwatch _afflictionSw = Stopwatch.StartNew();
    private readonly Stopwatch _lanternSw = Stopwatch.StartNew();
    private readonly Stopwatch _dayDreamSw = Stopwatch.StartNew();
    private readonly Stopwatch _mailSw = Stopwatch.StartNew();
    private readonly Stopwatch _deathRattleSw = Stopwatch.StartNew();

    public Aisling Aisling { get; set; }
    public bool TryBeginUpdate() => Interlocked.CompareExchange(ref _updateInFlight, 1, 0) == 0;
    public void EndUpdate() => Volatile.Write(ref _updateInFlight, 0);
    public bool MapUpdating { get; set; }
    public bool MapOpen { get; set; }
    public DateTime BoardOpened { get; set; }
    public DialogSession DlgSession { get; set; }
    private readonly List<LegendMarkInfo> _legendMarksPublic = [];
    private readonly List<LegendMarkInfo> _legendMarksPrivate = [];

    public bool IsRefreshing
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime - LastClientRefresh < new TimeSpan(0, 0, 0, 0, ServerSetup.Instance.Config.RefreshRate);
        }
    }

    public bool CanRefresh
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime - LastClientRefresh > new TimeSpan(0, 0, 0, 0, 100);
        }
    }

    public bool IsEquipping
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime - LastEquip > new TimeSpan(0, 0, 0, 0, 200);
        }
    }

    public bool IsDayDreaming
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime - LastMovement > new TimeSpan(0, 0, 2, 0, 0);
        }
    }

    public CastInfo SpellCastInfo { get; set; }
    public bool SummonRiftBoss { get; set; }
    public DateTime LastAssail { get; set; }
    public DateTime LastSpellCast { get; set; }
    public DateTime LastSelfProfileRequest { get; set; }
    public DateTime LastItemUsed { get; set; }
    public DateTime LastWorldListRequest { get; set; }
    public DateTime LastClientRefresh { get; set; }
    public DateTime LastWarp { get; set; }
    public Area LastMap { get; set; }
    public Item LastItemDropped { get; set; }
    public DateTime LastLocationSent { get; set; }
    public DateTime LastMapUpdated { get; set; }
    public DateTime LastMessageSent { get; set; }
    public DateTime LastMovement { get; set; }
    public DateTime LastEquip { get; set; }
    public DateTime LastSave { get; set; }
    public DateTime LastWhisperMessageSent { get; set; }
    public PendingBuy PendingBuySessions { get; set; }
    public PendingSell PendingItemSessions { get; set; }
    public WorldPortal PendingNode { get; set; }
    public uint EntryCheck { get; set; }
    private readonly Lock _warpCheckLock = new();

    // Client-owned work queue
    private readonly ConcurrentQueue<IClientWork> _clientWorkQueue = new();
    private readonly SemaphoreSlim _clientWorkSignal = new(0, int.MaxValue);

    public WorldClient([NotNull] IWorldServer<IWorldClient> server, [NotNull] Socket socket,
        [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
        [NotNull] ILogger<WorldClient> logger) : base(socket, crypto, packetSerializer, logger)
    {
        _server = server;
        _soundCoalescer = new SoundCoalescer(SendSoundImmediate, 150, 24);
        _healthBarCoalescer = new HealthBarCoalescer(SendHealthBarCoalesced, 150, 24);
        _bodyAnimationCoalescer = new BodyAnimationCoalescer(SendBodyAnimationCoalesced, 150, 24);
        _ = Task.Run(ProcessPlayerWorkQueue);
    }

    public void Update()
    {
        if (Aisling is not { LoggedIn: true })
            return;

        EquipLantern(_lanternSw);
        CheckDayDreaming(_dayDreamSw);
        CheckForMail(_mailSw);
        //ApplyAffliction(_afflictionSw);
        HandleDeathRattleRefresh(_deathRattleSw);
        HandleBadTrades();
        HandleSecOffenseEle();
    }

    private void EquipLantern(Stopwatch sw)
    {
        if (sw.ElapsedMilliseconds < 1500) return;
        sw.Restart();

        var map = Aisling.Map;
        if (map is null)
            return;

        var isDark = map.Flags.MapFlagIsSet(MapFlags.Darkness);

        if (isDark)
        {
            if (Aisling.Lantern == 2)
                return;

            Aisling.Lantern = 2;
            SendDisplayAisling(Aisling);
            return;
        }

        if (Aisling.Lantern != 2)
            return;

        Aisling.Lantern = 0;
        SendDisplayAisling(Aisling);
    }

    private void CheckDayDreaming(Stopwatch sw)
    {
        switch (Aisling.ActiveStatus)
        {
            case ActivityStatus.DayDreaming:
            case ActivityStatus.NeedHelp:
                DaydreamingRoutine(sw);
                break;
            default:
                break;
        }
    }

    private void DaydreamingRoutine(Stopwatch sw)
    {
        if (sw.ElapsedMilliseconds < 5000) return;
        sw.Restart();

        if (Aisling.Direction is not (1 or 2)) return;
        if (!((DateTime.UtcNow - Aisling.AislingTracker).TotalMinutes > 2)) return;
        if (!Socket.Connected || !IsDayDreaming) return;

        Aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendBodyAnimation(Aisling.Serial, (BodyAnimation)16, 100));
        Aisling.SendAnimationNearby(32, Aisling.Position);

        if (Aisling.Resting == Enums.RestPosition.RestPosition1) return;
        Aisling.Resting = Enums.RestPosition.RestPosition1;
        Aisling.Client.UpdateDisplay();
        Aisling.Client.SendDisplayAisling(Aisling);
    }

    private void CheckForMail(Stopwatch sw)
    {
        if (sw.ElapsedMilliseconds < 15000) return;
        sw.Restart();

        BoardPostStorage.MailFromDatabase(this);

        var hasUnreadMail = false;

        foreach (var post in Aisling.PersonalLetters)
        {
            var letter = post.Value;
            if (letter.ReadPost) continue;
            hasUnreadMail = true;
            break;
        }

        if (hasUnreadMail)
            SendAttributes(StatUpdateType.Secondary);
    }

    private void ApplyAffliction(Stopwatch sw)
    {
        if (sw.ElapsedMilliseconds < 5000) return;
        sw.Restart();

        if (Aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Normal)) return;

        var hasAny = false;

        if (Aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Lycanisim))
        {
            hasAny = true;
            if (!Aisling.HasBuff("Lycanisim"))
                EnqueueBuffAppliedEvent(Aisling, new BuffLycanisim());
        }

        if (Aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Vampirisim))
        {
            hasAny = true;
            if (!Aisling.HasBuff("Vampirisim"))
                EnqueueBuffAppliedEvent(Aisling, new BuffVampirisim());
        }

        if (!hasAny)
            Aisling.Afflictions |= Afflictions.Normal;
    }

    private void HandleDeathRattleRefresh(Stopwatch sw)
    {
        if (!Aisling.DeathRattle)
        {
            if (sw.IsRunning)
                sw.Reset();

            return;
        }

        if (!sw.IsRunning)
            sw.Start();

        if (sw.ElapsedMilliseconds < 300000) return;
        Aisling.DeathRattle = false;
        sw.Reset();
    }

    /// <summary>
    /// Method to handle trades when a player leaves the range or map of another player, or disconnects
    /// </summary>
    private void HandleBadTrades()
    {
        if (Aisling.Exchange?.Trader == null) return;
        if (Aisling.Exchange.Trader.LoggedIn && Aisling.WithinRangeOf(Aisling.Exchange.Trader)) return;
        Aisling.CancelExchange();
    }

    /// <summary>
    /// Method to handle edge case where buff "Atlantean Weapon" is on a player
    /// Prevents an item in the shield slot from overwriting the secondary offensive element
    /// </summary>
    private void HandleSecOffenseEle()
    {
        if (Aisling.IsEnhancingSecondaryOffense) return;
        if (Aisling.EquipmentManager.Shield == null && Aisling.SecondaryOffensiveElement != ElementManager.Element.None)
            Aisling.SecondaryOffensiveElement = ElementManager.Element.None;
    }

    public void PlayerDeathStatusCheck(Sprite damageDealer)
    {
        var proceed = false;

        if (Aisling.CurrentHp <= 0)
        {
            Aisling.CurrentHp = 1;
            if (Aisling.DeathRattle)
            {
                proceed = true;
            }
            else
            {
                ApplyDeathRattle(Aisling);
                return;
            }
        }

        if (!proceed) return;
        SendAttributes(StatUpdateType.Vitality);

        if (Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill) && damageDealer is Aisling)
        {
            for (var i = 0; i < 2; i++)
                Aisling.RemoveBuffsAndDebuffs();

            Aisling.CastDeath();
            var target = Aisling.Target;

            if (target != null)
            {
                if (target is Aisling aisling)
                    aisling.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{Aisling.Username} has been killed by {aisling.Username}."));
            }
            else
            {
                Aisling.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{Aisling.Username} has died."));
            }

            ReviveOnPlayerKillMap(Aisling);

            return;
        }

        if (Aisling.CurrentMapId == ServerSetup.Instance.Config.DeathMap || Aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap)) return;
        if (Aisling.Skulled) return;

        var debuff = new DebuffReaping();
        EnqueueDebuffAppliedEvent(Aisling, debuff);
    }

    private void ApplyDeathRattle(Aisling player)
    {
        player.DeathRattle = true;
        player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You feel a cold chill run down your spine...");
        player.SendAnimationNearby(401, player.Position);
        var save = (int)(player.MaximumHp * 0.05);
        player.CurrentHp = save;
        player.Client.SendAttributes(StatUpdateType.Vitality);
    }

    private static void ReviveOnPlayerKillMap(Aisling aisling)
    {
        _ = ReviveOnPlayerKillMapAsync(aisling);

        static async Task ReviveOnPlayerKillMapAsync(Aisling aisling)
        {
            try
            {
                for (var i = 10; i >= 1; i--)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Revive in {i}");
                }

                await Task.Delay(1000).ConfigureAwait(false);
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Revived");
                aisling.Client.Recover();
                aisling.Client.UpdateDisplay();
                aisling.Client.SendDisplayAisling(aisling);
            }
            catch { }
        }
    }

    #region Player Load

    public async Task<WorldClient> Load()
    {
        if (Aisling == null || Aisling.AreaId == 0) return null;
        Aisling.Client = this;

        if (!ServerSetup.Instance.GlobalMapCache.ContainsKey(Aisling.AreaId)) return null;
        await using var loadConnection = new SqlConnection(AislingStorage.ConnectionString);

        try
        {
            loadConnection.Open();
            SetAislingStartupVariables();
            SendUserId();
            SendEditableProfileRequest();
            InitCombos();
            InitQuests();
            LoadEquipment(loadConnection).LoadInventory(loadConnection).LoadBank(loadConnection).InitSpellBar().InitDiscoveredMaps().InitIgnoreList().InitLegend();
            SendDisplayAisling(Aisling);
            Enter();
            if (Aisling.Username == "Death")
                Aisling.SendAnimationNearby(391, Aisling.Position);
            BoardPostStorage.MailFromDatabase(this);
            var skillSet = DecideOnSkillsToPull();
            if (!skillSet.IsNullOrEmpty())
                SendMetaData(MetaDataRequestType.DataByName, ServerSetup.Instance.Game.MetafileManager, skillSet);

            // Increment playerCount for current map
            Aisling.Map.OnPlayerEnter();
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"Unhandled Exception in {nameof(Load)}.");
            ServerSetup.EventsLogger(ex.Message, LogLevel.Error);
            ServerSetup.EventsLogger(ex.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(ex);

            CloseTransport();
            return null;
        }
        finally
        {
            loadConnection.Close();
        }

        SendHeartBeat(0x14, 0x20);
        return this;
    }

    private void SetAislingStartupVariables()
    {
        var readyTime = DateTime.UtcNow;
        LastSave = readyTime;
        PendingItemSessions = null;
        LastLocationSent = readyTime;
        LastMovement = readyTime;
        LastClientRefresh = readyTime;
        LastMessageSent = readyTime;
        BoardOpened = readyTime;
        Aisling.Client = this;
        Aisling.BonusAc = 0;
        Aisling.Exchange = null;
        Aisling.LastMapId = ushort.MaxValue;
        Aisling.Aegis = 0;
        Aisling.Bleeding = 0;
        Aisling.Rending = 0;
        Aisling.Spikes = 0;
        Aisling.Reaping = 0;
        Aisling.Vampirism = 0;
        Aisling.Haste = 0;
        Aisling.Gust = 0;
        Aisling.Quake = 0;
        Aisling.Rain = 0;
        Aisling.Flame = 0;
        Aisling.Dusk = 0;
        Aisling.Dawn = 0;
        Aisling.Hacked = false;
        Aisling.PasswordAttempts = 0;
        Aisling.MonsterKillCounters = [];
        ReapplyKillCount();
        Aisling.Loading = true;
    }

    private string DecideOnSkillsToPull()
    {
        return Aisling == null ? null : SClassDictionary.SkillMap.GetValueOrDefault((Aisling.Race, Aisling.Path, Aisling.PastClass, Aisling.JobClass));
    }

    public WorldClient LoadEquipment(SqlConnection sConn)
    {
        try
        {
            const string procedure = "[SelectEquipped]";
            var values = new { Serial = (long)Aisling.Serial };
            var itemList = sConn.Query<Item>(procedure, values, commandType: CommandType.StoredProcedure).ToList();
            var aislingEquipped = Aisling.EquipmentManager.Equipment;

            foreach (var item in itemList.Where(s => s is { Name: not null }))
            {
                if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Name)) continue;

                var itemName = item.Name;
                var template = ServerSetup.Instance.GlobalItemTemplateCache[itemName];
                {
                    item.Template = template;
                }

                var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                var newGear = new Item
                {
                    ItemId = item.ItemId,
                    Template = item.Template,
                    Serial = item.Serial,
                    ItemPane = item.ItemPane,
                    Slot = item.Slot,
                    InventorySlot = item.InventorySlot,
                    Color = color,
                    Durability = item.Durability,
                    Identified = item.Identified,
                    ItemVariance = item.ItemVariance,
                    WeapVariance = item.WeapVariance,
                    ItemQuality = item.ItemQuality,
                    OriginalQuality = item.OriginalQuality,
                    Stacks = item.Stacks,
                    Enchantable = item.Template.Enchantable,
                    Tarnished = item.Tarnished,
                    GearEnhancement = item.GearEnhancement,
                    ItemMaterial = item.ItemMaterial,
                    GiftWrapped = item.GiftWrapped,
                    Image = item.Template.Image,
                    DisplayImage = item.Template.DisplayImage
                };

                ItemQualityVariance.SetMaxItemDurability(newGear, newGear.ItemQuality);
                newGear.GetDisplayName();
                newGear.NoColorGetDisplayName();

                aislingEquipped[newGear.Slot] = new Models.EquipmentSlot(newGear.Slot, newGear);
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        LoadSkillBook();
        LoadSpellBook();
        EquipGearAndAttachScripts();
        return this;
    }

    public WorldClient LoadInventory(SqlConnection sConn)
    {
        try
        {
            const string procedure = "[SelectInventory]";
            var values = new { Serial = (long)Aisling.Serial };
            var itemList = sConn.Query<Item>(procedure, values, commandType: CommandType.StoredProcedure).OrderBy(s => s.InventorySlot);

            foreach (var item in itemList)
            {
                if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Name)) continue;
                if (item.InventorySlot is <= 0 or >= 60)
                    item.InventorySlot = Aisling.Inventory.FindEmpty();

                var itemName = item.Name;
                var template = ServerSetup.Instance.GlobalItemTemplateCache[itemName];
                {
                    item.Template = template;
                }

                var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                var newItem = new Item
                {
                    ItemId = item.ItemId,
                    Template = item.Template,
                    Serial = item.Serial,
                    ItemPane = item.ItemPane,
                    Slot = item.Slot,
                    InventorySlot = item.InventorySlot,
                    Color = color,
                    Durability = item.Durability,
                    Identified = item.Identified,
                    ItemVariance = item.ItemVariance,
                    WeapVariance = item.WeapVariance,
                    ItemQuality = item.ItemQuality,
                    OriginalQuality = item.OriginalQuality,
                    Stacks = item.Stacks,
                    Enchantable = item.Template.Enchantable,
                    Tarnished = item.Tarnished,
                    GearEnhancement = item.GearEnhancement,
                    ItemMaterial = item.ItemMaterial,
                    GiftWrapped = item.GiftWrapped,
                    Image = item.Template.Image,
                    DisplayImage = item.Template.DisplayImage
                };

                if (Aisling.Inventory.Items[newItem.InventorySlot] != null)
                {
                    var routineCheck = 0;

                    for (byte i = 1; i < 60; i++)
                    {
                        if (Aisling.Inventory.Items[i] is null)
                        {
                            newItem.InventorySlot = i;
                            break;
                        }

                        if (i == 59)
                            routineCheck++;

                        if (routineCheck <= 4) continue;
                        ServerSetup.EventsLogger($"{Aisling.Username} has somehow exceeded their inventory, and have hanging items.");
                        CloseTransport();
                        break;
                    }
                }

                ItemQualityVariance.SetMaxItemDurability(newItem, newItem.ItemQuality);
                newItem.GetDisplayName();
                newItem.NoColorGetDisplayName();

                Aisling.Inventory.Items[newItem.InventorySlot] = newItem;
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        var itemsAvailable = Aisling.Inventory.Items.Values;

        foreach (var item in itemsAvailable)
        {
            if (item == null) continue;
            if (string.IsNullOrEmpty(item.Template.Name)) continue;

            Aisling.CurrentWeight += item.Template.CarryWeight;
            Aisling.Inventory.Items.TryUpdate(item.InventorySlot, item, null);
            Aisling.Inventory.UpdateSlot(Aisling.Client, item);
        }

        return this;
    }

    public WorldClient LoadBank(SqlConnection sConn)
    {
        Aisling.BankManager = new BankManager();

        try
        {
            const string procedure = "[SelectBanked]";
            var values = new { Serial = (long)Aisling.Serial };
            var itemList = sConn.Query<Item>(procedure, values, commandType: CommandType.StoredProcedure).ToList();

            foreach (var item in itemList)
            {
                if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Name)) continue;

                var itemName = item.Name;
                var template = ServerSetup.Instance.GlobalItemTemplateCache[itemName];
                {
                    item.Template = template;
                }

                var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                var newItem = new Item
                {
                    ItemId = item.ItemId,
                    Template = item.Template,
                    Serial = item.Serial,
                    ItemPane = item.ItemPane,
                    Slot = item.Slot,
                    InventorySlot = item.InventorySlot,
                    Color = color,
                    Durability = item.Durability,
                    Identified = item.Identified,
                    ItemVariance = item.ItemVariance,
                    WeapVariance = item.WeapVariance,
                    ItemQuality = item.ItemQuality,
                    OriginalQuality = item.OriginalQuality,
                    Stacks = item.Stacks,
                    Enchantable = item.Template.Enchantable,
                    Tarnished = item.Tarnished,
                    GearEnhancement = item.GearEnhancement,
                    ItemMaterial = item.ItemMaterial,
                    GiftWrapped = item.GiftWrapped,
                    Image = item.Template.Image,
                    DisplayImage = item.Template.DisplayImage
                };

                ItemQualityVariance.SetMaxItemDurability(newItem, newItem.ItemQuality);
                newItem.GetDisplayName();
                newItem.NoColorGetDisplayName();

                Aisling.BankManager.Items[newItem.ItemId] = newItem;
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        return this;
    }

    public void LoadSkillBook()
    {
        try
        {
            const string procedure = "[SelectSkills]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var skillList = sConn.Query<Skill>(procedure, values, commandType: CommandType.StoredProcedure).ToList();

            foreach (var skill in skillList.Where(s => s is { SkillName: not null }))
            {
                if (!ServerSetup.Instance.GlobalSkillTemplateCache.ContainsKey(skill.SkillName)) continue;

                var skillName = skill.SkillName;
                var template = ServerSetup.Instance.GlobalSkillTemplateCache[skillName];
                {
                    skill.Template = template;
                }

                var newSkill = new Skill
                {
                    Icon = skill.Template.Icon,
                    Level = skill.Level,
                    Slot = skill.Slot,
                    SkillName = skill.SkillName,
                    Uses = skill.Uses,
                    CurrentCooldown = skill.CurrentCooldown,
                    Template = skill.Template
                };

                Aisling.SkillBook.Skills[skill.Slot] = newSkill;
            }

            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        SkillCleanup();
    }

    public void LoadSpellBook()
    {
        try
        {
            const string procedure = "[SelectSpells]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var spellList = sConn.Query<Spell>(procedure, values, commandType: CommandType.StoredProcedure).ToList();

            foreach (var spell in spellList.Where(s => s is { SpellName: not null }))
            {
                if (!ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey(spell.SpellName)) continue;

                var spellName = spell.SpellName;
                var template = ServerSetup.Instance.GlobalSpellTemplateCache[spellName];
                {
                    spell.Template = template;
                }

                var newSpell = new Spell
                {
                    Icon = spell.Template.Icon,
                    Level = spell.Level,
                    Slot = spell.Slot,
                    SpellName = spell.SpellName,
                    Casts = spell.Casts,
                    CurrentCooldown = spell.CurrentCooldown,
                    Template = spell.Template
                };

                Aisling.SpellBook.Spells[spell.Slot] = newSpell;
            }

            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        SpellCleanup();
    }

    private WorldClient InitSpellBar()
    {
        return InitBuffs()
            .InitDeBuffs();
    }

    private WorldClient InitBuffs()
    {
        try
        {
            const string procedure = "[SelectBuffs]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var buffs = sConn.Query<Buff>(procedure, values, commandType: CommandType.StoredProcedure).ToList();
            var orderedBuffs = buffs.OrderBy(b => b.TimeLeft);

            foreach (var buffDb in orderedBuffs.Where(s => s is { Name: not null }))
            {
                var buffCheck = false;
                Buff buffFromCache = null;

                foreach (var buffInCache in ServerSetup.Instance.GlobalBuffCache.Values.Where(buffCache =>
                             buffCache.Name == buffDb.Name))
                {
                    buffCheck = true;
                    buffFromCache = buffInCache;
                }

                if (!buffCheck) continue;
                // Set script to Buff
                var buff = buffDb.ObtainBuffName(Aisling, buffFromCache);
                buff.Icon = buffFromCache.Icon;
                buff.Name = buffDb.Name;
                buff.Cancelled = buffFromCache.Cancelled;
                buff.Length = buffFromCache.Length;
                // Apply Buff on login - Use direct call, so we can set the db TimeLeft
                buff.OnApplied(Aisling, buff);
                // Set Time left
                buff.TimeLeft = buffDb.TimeLeft;
            }

            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        return this;
    }

    private WorldClient InitDeBuffs()
    {
        try
        {
            const string procedure = "[SelectDeBuffs]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var deBuffs = sConn.Query<Debuff>(procedure, values, commandType: CommandType.StoredProcedure).ToList();
            var orderedDebuffs = deBuffs.OrderBy(d => d.TimeLeft);

            foreach (var deBuffDb in orderedDebuffs.Where(s => s is { Name: not null }))
            {
                var debuffCheck = false;
                Debuff debuffFromCache = null;

                foreach (var debuffInCache in ServerSetup.Instance.GlobalDeBuffCache.Values.Where(debuffCache => debuffCache.Name == deBuffDb.Name))
                {
                    debuffCheck = true;
                    debuffFromCache = debuffInCache;
                }

                if (!debuffCheck) continue;
                // Set script to Debuff
                var debuff = deBuffDb.ObtainDebuffName(Aisling, debuffFromCache);
                debuff.Icon = debuffFromCache.Icon;
                debuff.Name = deBuffDb.Name;
                debuff.Cancelled = debuffFromCache.Cancelled;
                debuff.Length = debuffFromCache.Length;
                // Apply Debuff on login - Use direct call, so we can set the db TimeLeft
                debuff.OnApplied(Aisling, debuff);
                // Set Time left
                debuff.TimeLeft = deBuffDb.TimeLeft;
            }

            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        return this;
    }

    private WorldClient InitDiscoveredMaps()
    {
        try
        {
            const string procedure = "[SelectDiscoveredMaps]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var discovered = sConn.Query<DiscoveredMap>(procedure, values, commandType: CommandType.StoredProcedure).ToList();

            foreach (var map in discovered.Where(s => s is not null))
            {
                var temp = new DiscoveredMap
                {
                    Serial = map.Serial,
                    MapId = map.MapId
                };

                Aisling.DiscoveredMaps.Add(temp.MapId);
            }

            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        return this;
    }

    private WorldClient InitIgnoreList()
    {
        try
        {
            const string procedure = "[SelectIgnoredPlayers]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var ignoredRecords = sConn.Query<IgnoredRecord>(procedure, values, commandType: CommandType.StoredProcedure).ToList();

            foreach (var ignored in ignoredRecords.Where(s => s is not null))
            {
                if (ignored.PlayerIgnored is null) continue;

                var temp = new IgnoredRecord
                {
                    Serial = ignored.Serial,
                    PlayerIgnored = ignored.PlayerIgnored
                };

                Aisling.IgnoredList.Add(temp.PlayerIgnored);
            }

            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        return this;
    }

    private WorldClient InitLegend()
    {
        try
        {
            const string procedure = "[SelectLegends]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var legends = sConn.Query<Legend.LegendItem>(procedure, values, commandType: CommandType.StoredProcedure).ToList();

            foreach (var legend in legends.Where(s => s is not null).OrderBy(s => s.Time))
            {
                var newLegend = new Legend.LegendItem
                {
                    LegendId = legend.LegendId,
                    Key = legend.Key,
                    IsPublic = legend.IsPublic,
                    Time = legend.Time,
                    Color = legend.Color,
                    Icon = legend.Icon,
                    Text = legend.Text
                };

                Aisling.LegendBook.LegendMarks.Add(newLegend);
            }

            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        // Initial
        ObtainProfileLegendMarks(null, null);
        // Observable Collection
        Aisling.LegendBook.LegendMarks.CollectionChanged += ObtainProfileLegendMarks;
        Aisling.Loading = false;
        return this;
    }

    private void InitCombos()
    {
        try
        {
            const string procedure = "[SelectCombos]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            Aisling.ComboManager = sConn.QueryFirstOrDefault<ComboScroll>(procedure, values, commandType: CommandType.StoredProcedure);
            Aisling.ComboManager ??= new ComboScroll();
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }

    private void InitQuests()
    {
        try
        {
            const string procedure = "[SelectQuests]";
            var values = new { Serial = (long)Aisling.Serial };
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            Aisling.QuestManager = sConn.QueryFirst<Quests>(procedure, values, commandType: CommandType.StoredProcedure);
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }

    private void SkillCleanup()
    {
        var skillsAvailable = Aisling.SkillBook.Skills.Values.Where(i => i?.Template != null);
        var hasAssail = false;

        foreach (var skill in skillsAvailable)
        {
            switch (skill.SkillName)
            {
                case null:
                    continue;
                case "Assail":
                    hasAssail = true;
                    break;
            }

            SendAddSkillToPane(skill);

            if (skill.CurrentCooldown < skill.Template.Cooldown && skill.CurrentCooldown != 0)
            {
                SendCooldown(true, skill.Slot, skill.CurrentCooldown);
            }

            Skill.AttachScript(skill);
            {
                Aisling.SkillBook.Set(skill.Slot, skill, null);
            }
        }

        if (hasAssail) return;

        Skill.GiveTo(Aisling, "Assail");
    }

    private void SpellCleanup()
    {
        var spellsAvailable = Aisling.SpellBook.Spells.Values.Where(i => i?.Template != null);

        foreach (var spell in spellsAvailable)
        {
            if (spell.SpellName == null) continue;

            spell.Lines = spell.Template.BaseLines;
            SendAddSpellToPane(spell);

            if (spell.CurrentCooldown < spell.Template.Cooldown && spell.CurrentCooldown != 0)
            {
                SendCooldown(false, spell.Slot, spell.CurrentCooldown);
            }

            Spell.AttachScript(spell);
            {
                Aisling.SpellBook.Set(spell.Slot, spell, null);
            }
        }
    }

    private void EquipGearAndAttachScripts()
    {
        foreach (var (_, equipment) in Aisling.EquipmentManager.Equipment)
        {
            if (equipment?.Item?.Template == null) continue;

            Aisling.CurrentWeight += equipment.Item.Template.CarryWeight;
            SendEquipment(equipment.Item.Slot, equipment.Item);
            ScriptManager.TryCreate<ItemScript>(equipment.Item.Template.ScriptName, out var itemScript, equipment.Item);
            equipment.Item.Script = itemScript;

            if (!string.IsNullOrEmpty(equipment.Item.Template.WeaponScript))
            {
                ScriptManager.TryCreate<WeaponScript>(equipment.Item.Template.WeaponScript, out var weaponScript, equipment.Item);
                equipment.Item.WeaponScript = weaponScript;
            }

            equipment.Item.Script.Equipped(Aisling, equipment.Item.Slot);
        }

        var item = new Item();
        item.ReapplyItemModifiers(this);
    }

    #endregion

    #region Handlers

    protected override ValueTask OnPacketAsync(Span<byte> span)
    {
        var opCode = span[3];
        var packet = new Packet(ref span, Crypto.IsClientEncrypted(opCode));

        if (packet.IsEncrypted)
            Crypto.Decrypt(ref packet);

        // ToDo: Packet logging
        //ServerSetup.ConnectionLogger($"Client: {packet.OpCode}");

        return _server.HandlePacketAsync(this, in packet);
    }

    /// <summary>
    /// 0x02 - Send Login Message
    /// </summary>
    public void SendLoginMessage(LoginMessageType loginMessageType, string message = null)
    {
        var args = new LoginMessageArgs
        {
            LoginMessageType = loginMessageType,
            Message = message
        };

        Send(args);
    }

    /// <summary>
    /// 0x0F - Add Inventory
    /// </summary>
    public void SendAddItemToPane(Item item)
    {
        var args = new AddItemToPaneArgs
        {
            Item = new ItemInfo
            {
                Color = (DisplayColor)item.Color,
                Cost = (int?)item.Template.Value,
                Count = item.Stacks,
                CurrentDurability = (int)item.Durability,
                MaxDurability = (int)item.MaxDurability,
                Name = item.DisplayName,
                Slot = item.InventorySlot,
                Sprite = item.DisplayImage,
                Stackable = item.Template.CanStack
            }
        };

        Send(args);
    }

    /// <summary>
    /// 0x2C - Add Skill
    /// </summary>
    public void SendAddSkillToPane(Skill skill)
    {
        var args = new AddSkillToPaneArgs
        {
            Skill = new SkillInfo
            {
                Name = skill.SkillName,
                PanelName = skill.Name,
                Slot = skill.Slot,
                Sprite = skill.Icon
            }
        };

        Send(args);
    }

    /// <summary>
    /// 0x17 - Add Spell
    /// </summary>
    public void SendAddSpellToPane(Spell spell)
    {
        var args = new AddSpellToPaneArgs
        {
            Spell = new SpellInfo
            {
                Name = spell.SpellName,
                PanelName = spell.Name,
                Slot = spell.Slot,
                Sprite = spell.Icon,
                CastLines = Math.Clamp((byte)spell.Lines, (byte)0, (byte)9),
                Prompt = string.Empty,
                SpellType = (SpellType)spell.Template.TargetType
            }
        };

        Send(args);
    }

    /// <summary>
    /// 0x29 - Animation
    /// </summary>
    public void SendAnimation(ushort targetEffect, Position position = null, uint targetSerial = 0, ushort speed = 100, ushort casterEffect = 0, uint casterSerial = 0)
    {
        try
        {
            Point? point;

            if (position is null)
                point = null;
            else
                point = new Point(position.X, position.Y);

            var args = new AnimationArgs
            {
                AnimationSpeed = speed,
                SourceAnimation = casterEffect,
                SourceId = casterSerial,
                TargetAnimation = targetEffect,
                TargetId = targetSerial,
                TargetPoint = point
            };

            Send(args);
        }
        catch
        {
            try
            {
                var stackTrace = new StackTrace();
                var callingMethodOne = stackTrace.GetFrame(2)?.GetMethod()?.Name ?? "Unknown";
                var callingMethodTwo = stackTrace.GetFrame(3)?.GetMethod()?.Name ?? "Unknown";
                var callingMethodThree = stackTrace.GetFrame(4)?.GetMethod()?.Name ?? "Unknown";
                var callingMethodFour = stackTrace.GetFrame(5)?.GetMethod()?.Name ?? "Unknown";
                var trace = $"{Aisling?.Username ?? RemoteIp.ToString()} Animation from: {callingMethodOne}, from: {callingMethodTwo}, from: {callingMethodThree}, from: {callingMethodFour}";
                ServerSetup.Instance.Game.ServerPacketLogger.LogPacket(RemoteIp, trace);
                SentrySdk.CaptureMessage($"Issue with SendAnimation called from {trace}", SentryLevel.Error);
                ServerSetup.EventsLogger($"Issue in SendAnimation called from {trace}");
            }
            catch { }
        }
    }

    /// <summary>
    /// 0x08 - Attributes
    /// </summary>
    public void SendAttributes(StatUpdateType statUpdateType)
    {
        byte levelCap;
        byte abCap;

        if (Aisling.ExpLevel > 255)
            levelCap = 255;
        else
            levelCap = (byte)Aisling.ExpLevel;

        if (Aisling.AbpLevel > 255)
            abCap = 255;
        else
            abCap = (byte)Aisling.AbpLevel;

        var hasUnreadMail = false;

        foreach (var letter in Aisling.PersonalLetters.Values)
        {
            if (letter.ReadPost) continue;
            hasUnreadMail = true;
            break;
        }

        var args = new AttributesArgs
        {
            Ability = abCap,
            Ac = (sbyte)Math.Clamp(Aisling.SealedAc, sbyte.MinValue, sbyte.MaxValue),
            Blind = Aisling.IsBlind,
            Con = (byte)Math.Clamp(Aisling.Con, byte.MinValue, byte.MaxValue),
            CurrentHp = (uint)Aisling.CurrentHp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.CurrentHp,
            CurrentMp = (uint)Aisling.CurrentMp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.CurrentMp,
            CurrentWeight = Aisling.CurrentWeight,
            DefenseElement = (Element)Aisling.DefenseElement,
            Dex = (byte)Math.Clamp(Aisling.Dex, 0, 255),
            Dmg = (byte)Math.Clamp((sbyte)Aisling.Dmg, sbyte.MinValue, sbyte.MaxValue),
            GamePoints = (uint)Aisling.GamePoints,
            Gold = (uint)Aisling.GoldPoints,
            Hit = (byte)Math.Clamp((sbyte)Aisling.Hit, sbyte.MinValue, sbyte.MaxValue),
            Int = (byte)Math.Clamp(Aisling.Int, 0, 255),
            IsAdmin = Aisling.GameMaster,
            IsSwimming = true,
            Level = levelCap,
            MagicResistance = (byte)(Aisling.Regen / 10),
            HasUnreadMail = hasUnreadMail,
            MaximumHp = (uint)Aisling.MaximumHp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.MaximumHp,
            MaximumMp = (uint)Aisling.MaximumMp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.MaximumMp,
            MaxWeight = (short)Aisling.MaximumWeight,
            OffenseElement = (Element)Aisling.OffenseElement,
            StatUpdateType = statUpdateType,
            Str = (byte)Math.Clamp(Aisling.Str, byte.MinValue, byte.MaxValue),
            ToNextAbility = (uint)Aisling.AbpNext,
            ToNextLevel = (uint)Aisling.ExpNext,
            TotalAbility = (uint)Aisling.AbpTotal,
            TotalExp = (uint)Math.Clamp(Aisling.ExpTotal, 0, uint.MaxValue),
            UnspentPoints = (byte)Aisling.StatPoints,
            Wis = (byte)Math.Clamp(Aisling.Wis, byte.MinValue, byte.MaxValue)
        };

        Send(args);
    }

    /// <summary>
    /// 0x31 - Show Board
    /// </summary>
    public bool SendBoard(BoardTemplate board)
    {
        try
        {
            var postsCollection = board.Posts.Values.Select(postFormat => new PostInfo
            {
                Author = postFormat.Sender,
                DayOfMonth = postFormat.DatePosted.Day,
                MonthOfYear = postFormat.DatePosted.Month,
                IsHighlighted = postFormat.Highlighted,
                Message = postFormat.Message,
                PostId = (short)postFormat.PostId,
                Subject = postFormat.SubjectLine
            }).ToList();

            var boardInfo = new BoardInfo
            {
                BoardId = (ushort)board.BoardId,
                Name = board.Name,
                Posts = postsCollection
            };

            var args = new DisplayBoardArgs
            {
                Type = BoardOrResponseType.PublicBoard,
                Board = boardInfo,
                StartPostId = short.MaxValue
            };

            Send(args);
            return true;
        }
        catch
        {
            SendBoardResponse(BoardOrResponseType.SubmitPostResponse, "Issue with board", false);
        }

        return false;
    }

    /// <summary>
    /// 0x31 - Show Mailbox
    /// </summary>
    public bool SendMailBox()
    {
        try
        {
            var postsCollection = Aisling.PersonalLetters.Values.Select(postFormat => new PostInfo
            {
                Author = postFormat.Sender,
                DayOfMonth = postFormat.DatePosted.Day,
                MonthOfYear = postFormat.DatePosted.Month,
                IsHighlighted = postFormat.Highlighted,
                Message = postFormat.Message,
                PostId = (short)postFormat.PostId,
                Subject = postFormat.SubjectLine
            }).ToList();

            var boardInfo = new BoardInfo
            {
                BoardId = (ushort)Aisling.QuestManager.MailBoxNumber,
                Name = "Mail",
                Posts = postsCollection!
            };

            var args = new DisplayBoardArgs
            {
                Type = BoardOrResponseType.MailBoard,
                Board = boardInfo,
                StartPostId = short.MaxValue
            };

            Send(args);
            return true;
        }
        catch
        {
            SendBoardResponse(BoardOrResponseType.SubmitPostResponse, "Issue with mailbox, try again", false);
        }

        return false;
    }

    /// <summary>
    /// Show Posts and Letters
    /// </summary>
    public bool SendPost(PostTemplate post, bool isMail, bool enablePrevBtn = true)
    {
        try
        {
            var args = new DisplayBoardArgs
            {
                Type = isMail ? BoardOrResponseType.MailPost : BoardOrResponseType.PublicPost,
                Post = new PostInfo
                {
                    Author = post.Sender,
                    DayOfMonth = post.DatePosted.Day,
                    MonthOfYear = post.DatePosted.Month,
                    IsHighlighted = post.Highlighted,
                    Message = post.Message,
                    PostId = (short)post.PostId,
                    Subject = post.SubjectLine
                },
                EnablePrevBtn = enablePrevBtn
            };

            if (isMail)
                UpdateMailAsRead(args);

            Send(args);
            return true;
        }
        catch
        {
            SendBoardResponse(BoardOrResponseType.SubmitPostResponse, "Issue opening", false);
        }

        return false;
    }

    private void UpdateMailAsRead(DisplayBoardArgs args)
    {
        if (args?.Post is null) return;
        var letter = Aisling.PersonalLetters.Values.FirstOrDefault(c => c.PostId == args.Post.PostId);
        if (letter == null) return;
        letter.ReadPost = true;
        AislingStorage.UpdatePost(letter, Aisling.QuestManager.MailBoxNumber);
        SendAttributes(StatUpdateType.Secondary);
    }

    public void SendBoardResponse(BoardOrResponseType responseType, string message, bool success)
    {
        var args = new DisplayBoardArgs
        {
            Type = responseType,
            ResponseMessage = message,
            Success = success
        };

        Send(args);
    }

    /// <summary>
    /// 0x1A - Player Body Animation - Coalesced Send
    /// </summary>
    private void SendBodyAnimationCoalesced(BodyAnimationArgs args) => Send(args);

    /// <summary>
    /// 0x1A - Player Body Animation
    /// </summary>
    public void SendBodyAnimation(uint id, BodyAnimation bodyAnimation, ushort speed, byte? sound = null)
    {
        if (bodyAnimation is BodyAnimation.None) return;

        var args = new BodyAnimationArgs
        {
            SourceId = id,
            BodyAnimation = bodyAnimation,
            Sound = sound,
            AnimationSpeed = speed
        };

        _bodyAnimationCoalescer.Enqueue(args);
    }

    /// <summary>
    /// 0x0B - Player Move
    /// </summary>
    public void SendConfirmClientWalk(Position oldPoint, Direction direction)
    {
        var args = new ClientWalkResponseArgs
        {
            Direction = direction,
            OldPoint = new Point(oldPoint.X, oldPoint.Y)
        };

        Send(args);
    }

    /// <summary>
    /// 0x4C - Reconnect
    /// </summary>
    public async void SendConfirmExit()
    {
        try
        {
            // Decrement playerCount for current map
            Aisling.Map.OnPlayerLeave();

            // Close Popups
            this.CloseDialog();
            Aisling.CancelExchange();

            // Exit Party
            if (Aisling.GroupId != 0)
                Party.RemovePartyMember(Aisling);

            // Set Timestamps
            Aisling.LastLogged = DateTime.UtcNow;
            Aisling.LoggedIn = false;

            // Save
            ExitConfirmed = await Save().ConfigureAwait(false);

            // Cleanup
            Aisling.Remove(true);

            var args = new ExitResponseArgs
            {
                ExitConfirmed = ExitConfirmed
            };

            Send(args);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            CloseTransport();
        }
    }

    /// <summary>
    /// 0x3F - Cooldown
    /// </summary>
    public void SendCooldown(bool skill, byte slot, int cooldownSeconds)
    {
        if (Aisling.Overburden)
        {
            cooldownSeconds *= 2;
        }

        if (cooldownSeconds <= 0)
            cooldownSeconds = 0;

        var args = new CooldownArgs
        {
            IsSkill = skill,
            Slot = slot,
            CooldownSecs = (uint)cooldownSeconds
        };

        Send(args);
    }

    /// <summary>
    /// 0x11 - Sprite Direction
    /// </summary>
    public void SendCreatureTurn(uint id, Direction direction)
    {
        var args = new CreatureTurnArgs
        {
            SourceId = id,
            Direction = direction
        };

        Send(args);
    }

    /// <summary>
    /// 0x0C - NPC Move
    /// </summary>
    public void SendCreatureWalk(uint id, Point startPoint, Direction direction)
    {
        var args = new CreatureWalkArgs
        {
            SourceId = id,
            OldPoint = startPoint,
            Direction = direction
        };

        Send(args);
    }

    /// <summary>
    /// 0x33 - Display Player
    /// </summary>
    public void SendDisplayAisling(Aisling aisling)
    {
        ushort? monsterForm = null;
        if (aisling.MonsterForm != 0)
            monsterForm = aisling.MonsterForm;

        var args = new DisplayAislingArgs
        {
            AccessoryColor1 = (DisplayColor)aisling.Accessory1Color,
            AccessoryColor2 = (DisplayColor)aisling.Accessory2Color,
            AccessoryColor3 = (DisplayColor)aisling.Accessory3Color,
            AccessorySprite1 = (ushort)aisling.Accessory1Img,
            AccessorySprite2 = (ushort)aisling.Accessory2Img,
            AccessorySprite3 = (ushort)aisling.Accessory3Img,
            ArmorSprite1 = (ushort)aisling.ArmorImg,
            ArmorSprite2 = (ushort)aisling.ArmorImg,
            PantsColor = (DisplayColor?)aisling.Pants,
            BodyColor = (BodyColor)aisling.BodyColor,
            BootsColor = (DisplayColor)aisling.BootColor,
            BootsSprite = (byte)aisling.BootsImg,
            Direction = (Direction)aisling.Direction,
            FaceSprite = 0,
            Gender = (Gender)aisling.Gender,
            GroupBoxText = "",
            HeadColor = (DisplayColor)aisling.HairColor,
            Id = aisling.Serial,
            IsDead = aisling.IsDead(),
            IsTransparent = aisling.IsInvisible,
            LanternSize = (LanternSize)aisling.Lantern,
            Name = aisling.Username,
            OvercoatColor = (DisplayColor)aisling.OverCoatColor,
            OvercoatSprite = (ushort)aisling.OverCoatImg,
            RestPosition = (RestPosition)aisling.Resting,
            ShieldSprite = (byte)aisling.ShieldImg,
            Sprite = monsterForm,
            WeaponSprite = (ushort)aisling.WeaponImg,
            X = aisling.X,
            Y = aisling.Y
        };

        if (aisling.EquipmentManager.OverHelm != null && aisling.HeadAccessoryImg != 0)
            args.HeadSprite = (ushort)aisling.HeadAccessoryImg;
        else if (aisling.EquipmentManager.Helmet != null && aisling.HelmetImg != 0)
            args.HeadSprite = (ushort)aisling.HelmetImg;
        else
            args.HeadSprite = aisling.HairStyle;

        if (aisling.Gender == Enums.Gender.Male)
        {
            if (aisling.IsInvisible)
                args.BodySprite = BodySprite.MaleInvis;
            else
                args.BodySprite = aisling.IsDead() ? BodySprite.MaleGhost : BodySprite.Male;
        }
        else
        {
            if (aisling.IsInvisible)
                args.BodySprite = BodySprite.FemaleInvis;
            else
                args.BodySprite = aisling.IsDead() ? BodySprite.FemaleGhost : BodySprite.Female;
        }

        if (!Aisling.Equals(aisling))
        {
            if (Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
                args.NameTagStyle = !Aisling.Clan.IsNullOrEmpty() && Aisling.Clan == aisling.Clan ? NameTagStyle.Neutral : NameTagStyle.Hostile;
            else if (!Aisling.Clan.IsNullOrEmpty() && Aisling.Clan == aisling.Clan)
                args.NameTagStyle = NameTagStyle.FriendlyHover;
            else
                args.NameTagStyle = NameTagStyle.NeutralHover;
        }

        Send(args);
    }

    public void SendDoorsOnMap(ICollection<DoorInfo> doors)
    {
        var doorArgs = new DoorArgs
        {
            Doors = doors
        };

        if (doorArgs.Doors.Count == 0) return;

        Send(doorArgs);
    }

    /// <summary>
    /// 0x3A - Effect Duration
    /// </summary>
    public void SendEffect(EffectColor effectColor, byte effectIcon)
    {
        var args = new EffectArgs
        {
            EffectColor = effectColor,
            EffectIcon = effectIcon
        };

        Send(args);
    }

    /// <summary>
    /// 0x37 - Add Equipment
    /// </summary>
    public void SendEquipment(byte displaySlot, Item item)
    {
        if (displaySlot == 0) return;

        item.Slot = displaySlot;

        var args = new EquipmentArgs
        {
            Slot = (EquipmentSlot)displaySlot,
            Item = new ItemInfo
            {
                Color = (DisplayColor)item.Color,
                Cost = (int?)item.Template.Value,
                Count = item.Stacks,
                CurrentDurability = (int)item.Durability,
                MaxDurability = (int)item.MaxDurability,
                Name = item.NoColorDisplayName,
                Slot = displaySlot,
                Sprite = item.DisplayImage,
                Stackable = item.Template.CanStack
            }
        };

        Send(args);
    }

    /// <summary>
    /// 0x42 - Start Exchange 
    /// </summary>
    public void SendExchangeStart(Aisling fromAisling)
    {
        var args = new DisplayExchangeArgs
        {
            ExchangeResponseType = ExchangeResponseType.StartExchange,
            OtherUserId = fromAisling.Serial,
            OtherUserName = fromAisling.Username
        };

        Send(args);
    }

    /// <summary>
    /// 0x42 - Add Item to Exchange 
    /// </summary>
    public void SendExchangeAddItem(bool rightSide, byte index, Item item)
    {
        var args = new DisplayExchangeArgs
        {
            ExchangeResponseType = ExchangeResponseType.AddItem,
            RightSide = rightSide,
            ExchangeIndex = index,
            ItemSprite = item.Template.DisplayImage,
            ItemColor = (DisplayColor?)item.Template.Color,
            ItemName = item.DisplayName
        };

        if (item.Stacks > 1)
            args.ItemName = $"{item.DisplayName} [{item.Stacks}]";

        Send(args);
    }

    /// <summary>
    /// 0x42 - Add Gold to Exchange 
    /// </summary>
    public void SendExchangeSetGold(bool rightSide, uint amount)
    {
        var args = new DisplayExchangeArgs
        {
            ExchangeResponseType = ExchangeResponseType.SetGold,
            RightSide = rightSide,
            GoldAmount = (int)amount
        };

        Send(args);
    }

    /// <summary>
    /// 0x42 - Request To Exchange (Item | Money)
    /// </summary>
    public void SendExchangeRequestAmount(byte slot)
    {
        var args = new DisplayExchangeArgs
        {
            ExchangeResponseType = ExchangeResponseType.RequestAmount,
            FromSlot = slot
        };

        Send(args);
    }

    /// <summary>
    /// 0x42 - Accept Exchange 
    /// </summary>
    public void SendExchangeAccepted(bool persistExchange)
    {
        var args = new DisplayExchangeArgs
        {
            ExchangeResponseType = ExchangeResponseType.Accept,
            PersistExchange = persistExchange,
            Message = "Exchange completed."
        };

        Send(args);
    }

    /// <summary>
    /// 0x42 - Cancel Exchange 
    /// </summary>
    public void SendExchangeCancel(bool rightSide)
    {
        var args = new DisplayExchangeArgs
        {
            ExchangeResponseType = ExchangeResponseType.Cancel,
            RightSide = rightSide,
            Message = "Exchange cancelled."
        };

        Send(args);
    }

    /// <summary>
    /// Forced Client Packet
    /// </summary>
    public void SendForcedClientPacket(ref Packet clientPacket)
    {
        var args = new ForceClientPacketArgs
        {
            ClientOpCode = (ClientOpCode)clientPacket.OpCode,
            Data = clientPacket.Buffer.ToArray()
        };

        Send(args);
    }

    /// <summary>
    /// 0x63 - Group Request
    /// </summary>
    public void SendDisplayGroupInvite(ServerGroupSwitch serverGroupSwitch, string fromName, DisplayGroupBoxInfo groupBoxInfo = null)
    {
        var args = new DisplayGroupInviteArgs
        {
            ServerGroupSwitch = serverGroupSwitch,
            SourceName = fromName
        };

        if (serverGroupSwitch == ServerGroupSwitch.ShowGroupBox)
            args.GroupBoxInfo = groupBoxInfo;

        Send(args);
    }

    /// <summary>
    /// 0x13 - Health Bar - Coalesced Send
    /// </summary>
    private void SendHealthBarCoalesced(HealthBarArgs args) => Send(args);

    /// <summary>
    /// 0x13 - Health Bar
    /// </summary>
    public void SendHealthBar(Sprite creature, byte? sound = 0xFF)
    {
        var currentHealth = creature.CurrentHp.LongClamp(1);
        var pct = (byte)((double)100 * currentHealth / creature.MaximumHp);
        var kind = SpriteMaker.SpriteKind(creature);
        var args = new HealthBarArgs
        {
            SourceId = creature.Serial,
            Kind = kind,
            HealthPercent = pct,
            Sound = sound,
            Tail = creature is Aisling ? null : 0x00
        };

        _healthBarCoalescer.Enqueue(args);
    }

    /// <summary>
    /// 0x20 - Change Hour (Night - Day)
    /// </summary>
    /// <param name="lightLevel">
    /// Darkest = 0,
    /// Darker = 1,
    /// Dark = 2,
    /// Light = 3,
    /// Lighter = 4,
    /// Lightest = 5
    /// </param>
    public void SendLightLevel(LightLevel lightLevel)
    {
        var args = new LightLevelArgs
        {
            LightLevel = lightLevel
        };

        Send(args);
    }

    /// <summary>
    /// 0x04 - Location
    /// </summary>
    public void SendLocation()
    {
        var args = new LocationArgs
        {
            X = Aisling.X,
            Y = Aisling.Y
        };

        Send(args);
    }

    /// <summary>
    /// 0x3C - Map Data
    /// </summary>
    public void SendMapData()
    {
        var mapTemplate = Aisling.Map;

        for (byte y = 0; y < mapTemplate.Height; y++)
        {
            var args = new MapDataArgs
            {
                CurrentYIndex = y,
                Width = mapTemplate.Width,
                MapData = mapTemplate.GetRowData(y).ToArray()
            };

            Send(args);
        }
    }

    /// <summary>
    /// 0x15 - Map Information
    /// </summary>
    public void SendMapInfo()
    {
        var args = new MapInfoArgs
        {
            CheckSum = Aisling.Map.Hash,
            Flags = (byte)Aisling.Map.Flags,
            Height = Aisling.Map.Height,
            MapId = (short)Aisling.Map.ID,
            Name = Aisling.Map.Name,
            Width = Aisling.Map.Width
        };

        Send(args);
    }

    /// <summary>
    /// 0x6F - MetaData Send
    /// </summary>
    public void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore, string name = null)
    {
        var args = new MetaDataArgs
        {
            MetaDataRequestType = metaDataRequestType
        };

        switch (metaDataRequestType)
        {
            case MetaDataRequestType.DataByName:
                {
                    try
                    {
                        var metaData = ServerSetup.Instance.Game.Metafiles.Values.FirstOrDefault(file => file.Name == name);
                        if (metaData == null) break;
                        if (!name!.Contains("Class"))
                        {
                            args.MetaDataInfo = new MetaDataInfo
                            {
                                Name = metaData.Name,
                                Data = metaData.DeflatedData,
                                CheckSum = metaData.Hash
                            };

                            break;
                        }

                        var orgFileName = Aisling.Path switch
                        {
                            Class.Berserker => "SClass1",
                            Class.Defender => "SClass2",
                            Class.Assassin => "SClass3",
                            Class.Cleric => "SClass4",
                            Class.Arcanus => "SClass5",
                            Class.Monk => "SClass6",
                            _ => metaData.Name
                        };

                        args.MetaDataInfo = new MetaDataInfo
                        {
                            Name = orgFileName,
                            Data = metaData.DeflatedData,
                            CheckSum = metaData.Hash
                        };
                    }
                    catch (Exception ex)
                    {
                        ServerSetup.EventsLogger(ex.Message, LogLevel.Error);
                        ServerSetup.EventsLogger(ex.StackTrace, LogLevel.Error);
                        SentrySdk.CaptureException(ex);
                    }

                    break;
                }
            case MetaDataRequestType.AllCheckSums:
                {
                    try
                    {
                        args.MetaDataCollection = [];
                        foreach (var file in ServerSetup.Instance.Game.Metafiles.Values.Where(file => !file.Name.Contains("SClass")))
                        {
                            var metafileInfo = new MetaDataInfo
                            {
                                CheckSum = file.Hash,
                                Data = file.DeflatedData,
                                Name = file.Name
                            };

                            args.MetaDataCollection?.Add(metafileInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        ServerSetup.EventsLogger(ex.Message, LogLevel.Error);
                        ServerSetup.EventsLogger(ex.StackTrace, LogLevel.Error);
                        SentrySdk.CaptureException(ex);
                    }

                    break;
                }
        }

        Send(args);
    }

    public void SendNotepad(byte identifier, NotepadType type, byte height, byte width, string message)
    {
        var args = new NotepadArgs
        {
            Slot = identifier,
            NotepadType = type,
            Height = height,
            Width = width,
            Message = message ?? string.Empty
        };

        Send(args);
    }

    /// <summary>
    /// 0x34 - Player Profile
    /// </summary>
    /// <param name="aisling">Target Player</param>
    public void SendProfile(Aisling aisling)
    {
        var equipment = new Dictionary<EquipmentSlot, ItemInfo>();
        var partyOpen = aisling.PartyStatus == (GroupStatus)1;

        #region Gear

        if (aisling.EquipmentManager.Weapon != null)
        {
            var equip = aisling.EquipmentManager.Weapon;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Armor != null)
        {
            var equip = aisling.EquipmentManager.Armor;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Shield != null)
        {
            var equip = aisling.EquipmentManager.Shield;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Helmet != null)
        {
            var equip = aisling.EquipmentManager.Helmet;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Earring != null)
        {
            var equip = aisling.EquipmentManager.Earring;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Necklace != null)
        {
            var equip = aisling.EquipmentManager.Necklace;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.LHand != null)
        {
            var equip = aisling.EquipmentManager.LHand;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.RHand != null)
        {
            var equip = aisling.EquipmentManager.RHand;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.LArm != null)
        {
            var equip = aisling.EquipmentManager.LArm;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.RArm != null)
        {
            var equip = aisling.EquipmentManager.RArm;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Waist != null)
        {
            var equip = aisling.EquipmentManager.Waist;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Leg != null)
        {
            var equip = aisling.EquipmentManager.Leg;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.Foot != null)
        {
            var equip = aisling.EquipmentManager.Foot;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.FirstAcc != null)
        {
            var equip = aisling.EquipmentManager.FirstAcc;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.OverCoat != null)
        {
            var equip = aisling.EquipmentManager.OverCoat;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.OverHelm != null)
        {
            var equip = aisling.EquipmentManager.OverHelm;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.SecondAcc != null)
        {
            var equip = aisling.EquipmentManager.SecondAcc;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (aisling.EquipmentManager.ThirdAcc != null)
        {
            var equip = aisling.EquipmentManager.ThirdAcc;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        #endregion

        var args = new OtherProfileArgs
        {
            JobClass = (JobClass)ClassStrings.JobDisplayFlag(aisling.JobClass.ToString()),
            BaseClass = (BaseClass)ClassStrings.ClassDisplayInt(aisling.Path.ToString()),
            DisplayClass = aisling.Path.ToString(),
            Equipment = equipment,
            GroupOpen = partyOpen,
            GuildName = $"{aisling.Clan} - {aisling.ClanRank}",
            GuildRank = $"GearP.: {aisling.GamePoints}",
            Id = aisling.Serial,
            LegendMarks = aisling.Client._legendMarksPublic,
            Name = aisling.Username,
            Nation = (Nation)aisling.Nation,
            Portrait = aisling.PictureData,
            ProfileText = aisling.ProfileMessage,
            SocialStatus = (SocialStatus)aisling.ActiveStatus,
            Title = $"Lvl: {aisling.ExpLevel}  Rnk: {aisling.AbpLevel}"
        };

        Send(args);
    }

    private void ObtainProfileLegendMarks(object sender, NotifyCollectionChangedEventArgs args)
    {
        _legendMarksPublic.Clear();
        _legendMarksPrivate.Clear();

        try
        {
            var currentMarks = Aisling.LegendBook.LegendMarks.ToList();
            var legends = currentMarks.DistinctBy(m => m.Text);

            _legendMarksPublic.AddRange(legends
                .Where(legend => legend is { IsPublic: true })
                .Select(legend =>
                {
                    var markCount = currentMarks.Count(item => item.Text == legend.Text);
                    var legendText = $"{legend.Text} - {legend.Time.ToShortDateString()} ({markCount})";
                    return new LegendMarkInfo
                    {
                        Color = (MarkColor)legend.Color,
                        Icon = (MarkIcon)legend.Icon,
                        Key = legend.Key,
                        Text = legendText
                    };
                }));

            _legendMarksPrivate.AddRange(legends
                .Where(legend => legend is not null)
                .Select(legend =>
                {
                    var markCount = currentMarks.Count(item => item.Text == legend.Text);
                    var legendText = $"{legend.Text} - {legend.Time.ToShortDateString()} ({markCount})";
                    return new LegendMarkInfo
                    {
                        Color = (MarkColor)legend.Color,
                        Icon = (MarkIcon)legend.Icon,
                        Key = legend.Key,
                        Text = legendText
                    };
                }));
        }
        catch
        {
            // ignored
        }
    }

    public override void SendHeartBeat(byte first, byte second)
    {
        if (Interlocked.CompareExchange(ref HeartBeatInFlight, 1, 0) != 0) return;
        Volatile.Write(ref HeartBeatStartTimestamp, Stopwatch.GetTimestamp());

        var args = new HeartBeatResponseArgs
        {
            First = first,
            Second = second
        };

        Send(args);
    }

    /// <summary>
    /// 0x0D - Public Messages / Chant
    /// </summary>
    /// <param name="sourceId">Sprite Serial</param>
    /// <param name="publicMessageType">Message Type</param>
    /// <param name="message">Message</param>
    public void SendPublicMessage(uint sourceId, PublicMessageType publicMessageType, string message)
    {
        var args = new DisplayPublicMessageArgs
        {
            SourceId = sourceId,
            PublicMessageType = publicMessageType,
            Message = message
        };

        Send(args);
    }

    /// <summary>
    /// 0x10 - Remove Item from Inventory
    /// </summary>
    /// <param name="slot"></param>
    public void SendRemoveItemFromPane(byte slot)
    {
        var args = new RemoveItemFromPaneArgs
        {
            Slot = slot
        };

        Send(args);
    }

    /// <summary>
    /// 0x0E - Remove World Object
    /// </summary>
    /// <param name="id"></param>
    public async Task<bool> SendRemoveObject(uint id)
    {
        try
        {
            var args = new RemoveEntityArgs
            {
                SourceId = id
            };

            Send(args);
            return await Task.FromResult(true);
        }
        catch
        {
            return await Task.FromResult(false);
        }
    }

    /// <summary>
    /// 0x2D - Remove Skill
    /// </summary>
    /// <param name="slot"></param>
    public void SendRemoveSkillFromPane(byte slot)
    {
        var args = new RemoveSkillFromPaneArgs
        {
            Slot = slot
        };

        Send(args);
    }

    /// <summary>
    /// 0x18 - Remove Spell
    /// </summary>
    /// <param name="slot"></param>
    public void SendRemoveSpellFromPane(byte slot)
    {
        var args = new RemoveSpellFromPaneArgs
        {
            Slot = slot
        };

        Send(args);
    }

    /// <summary>
    /// 0x39 - Self Profile
    /// </summary>
    public void SendSelfProfile()
    {
        if (Aisling.ProfileOpen) return;

        var equipment = new Dictionary<EquipmentSlot, ItemInfo>();
        var partyOpen = Aisling.PartyStatus == (GroupStatus)1;

        #region Gear

        if (Aisling.EquipmentManager.Weapon != null)
        {
            var equip = Aisling.EquipmentManager.Weapon;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Armor != null)
        {
            var equip = Aisling.EquipmentManager.Armor;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Shield != null)
        {
            var equip = Aisling.EquipmentManager.Shield;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Helmet != null)
        {
            var equip = Aisling.EquipmentManager.Helmet;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Earring != null)
        {
            var equip = Aisling.EquipmentManager.Earring;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Necklace != null)
        {
            var equip = Aisling.EquipmentManager.Necklace;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.LHand != null)
        {
            var equip = Aisling.EquipmentManager.LHand;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.RHand != null)
        {
            var equip = Aisling.EquipmentManager.RHand;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.LArm != null)
        {
            var equip = Aisling.EquipmentManager.LArm;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.RArm != null)
        {
            var equip = Aisling.EquipmentManager.RArm;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Waist != null)
        {
            var equip = Aisling.EquipmentManager.Waist;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Leg != null)
        {
            var equip = Aisling.EquipmentManager.Leg;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.Foot != null)
        {
            var equip = Aisling.EquipmentManager.Foot;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.FirstAcc != null)
        {
            var equip = Aisling.EquipmentManager.FirstAcc;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.OverCoat != null)
        {
            var equip = Aisling.EquipmentManager.OverCoat;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.OverHelm != null)
        {
            var equip = Aisling.EquipmentManager.OverHelm;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.SecondAcc != null)
        {
            var equip = Aisling.EquipmentManager.SecondAcc;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        if (Aisling.EquipmentManager.ThirdAcc != null)
        {
            var equip = Aisling.EquipmentManager.ThirdAcc;

            var item = new ItemInfo
            {
                Color = (DisplayColor)equip.Item.Color,
                Cost = (int?)equip.Item.Template.Value,
                Count = equip.Item.Stacks,
                CurrentDurability = (int)equip.Item.Durability,
                MaxDurability = (int)equip.Item.MaxDurability,
                Name = equip.Item.NoColorDisplayName,
                Slot = (byte)equip.Slot,
                Sprite = equip.Item.DisplayImage,
                Stackable = equip.Item.Template.CanStack
            };

            equipment.Add((EquipmentSlot)item.Slot, item);
        }

        #endregion

        var args = new SelfProfileArgs
        {
            JobClass = (JobClass)ClassStrings.JobDisplayFlag(Aisling.JobClass.ToString()),
            BaseClass = (BaseClass)ClassStrings.ClassDisplayInt(Aisling.Path.ToString()),
            DisplayClass = Aisling.Path.ToString(),
            Equipment = equipment,
            GroupOpen = partyOpen,
            GroupString = Aisling.GroupParty?.PartyMemberString ?? "",
            GuildName = $"{Aisling.Clan} - {Aisling.ClanRank}",
            GuildRank = $"GearP.: {Aisling.GamePoints}",
            EnableMasterQuestMetaData = Aisling.Stage.StageFlagIsSet(ClassStage.Master),
            EnableMasterAbilityMetaData = Aisling.Stage.StageFlagIsSet(ClassStage.Master),
            LegendMarks = _legendMarksPrivate,
            Name = Aisling.Username,
            Nation = (Nation)Aisling.Nation,
            Portrait = Aisling.PictureData,
            ProfileText = Aisling.ProfileMessage,
            SpouseName = "",
            Title = $"Lvl: {Aisling.ExpLevel}  Rnk: {Aisling.AbpLevel}"
        };

        Send(args);
    }

    /// <summary>
    /// 0x0A - System Messages / Private Messages
    /// </summary>
    public void SendServerMessage(ServerMessageType serverMessageType, string message)
    {
        var args = new ServerMessageArgs
        {
            ServerMessageType = serverMessageType,
            Message = message
        };

        Send(args);
    }

    /// <summary>
    /// 0x19 - Send Sound - Bypasses Coalescer (And is used by Coalesced Send)
    /// </summary>
    /// <param name="sound">Sound Number</param>
    /// <param name="isMusic">Whether the sound is a song</param>
    public void SendSoundImmediate(byte sound, bool isMusic)
    {
        var args = new SoundArgs
        {
            Sound = sound,
            IsMusic = isMusic
        };

        Send(args);
    }

    /// <summary>
    /// 0x19 - Send Sound
    /// </summary>
    /// <param name="sound">Sound Number</param>
    /// <param name="isMusic">Whether the sound is a song</param>
    public void SendSound(byte sound, bool isMusic) => _soundCoalescer.Enqueue(sound, isMusic);

    /// <summary>
    /// 0x38 - Remove Equipment
    /// </summary>
    /// <param name="equipmentSlot"></param>
    public void SendUnequip(EquipmentSlot equipmentSlot)
    {
        var args = new DisplayUnequipArgs
        {
            EquipmentSlot = equipmentSlot
        };

        Send(args);
    }

    /// <summary>
    /// 0x05 - UserID, Direction, Rogue Map, Gender
    /// </summary>
    public void SendUserId()
    {
        var args = new UserIdArgs
        {
            BaseClass = (BaseClass)2,
            Direction = (Direction)Aisling.Direction,
            Id = Aisling.Serial
        };

        Send(args);
    }

    /// <summary>
    /// 0x07 - Add World Objects
    /// </summary>
    /// <param name="objects">Objects that are visible to a player</param>
    public void SendVisibleEntities(List<Sprite> objects)
    {
        if (objects.Count <= 0) return;

        // Split this into chunks so as not to crash the client
        foreach (var chunk in objects.Where(o => o != null).OrderBy(o => o.AbandonedDate).Chunk(500))
        {
            var args = new DisplayVisibleEntitiesArgs();
            var visibleArgs = new List<VisibleEntityInfo>();
            args.VisibleObjects = visibleArgs;

            foreach (var obj in chunk)
                switch (obj)
                {
                    case Item groundItem:
                        var groundItemInfo = new GroundItemInfo
                        {
                            Id = groundItem.ItemVisibilityId,
                            Sprite = groundItem.DisplayImage,
                            X = groundItem.X,
                            Y = groundItem.Y,
                            Color = (DisplayColor)groundItem.Template.Color
                        };

                        visibleArgs.Add(groundItemInfo);

                        break;
                    case Money money:
                        var moneyInfo = new GroundItemInfo
                        {
                            Id = money.Serial,
                            Sprite = money.Image,
                            X = money.X,
                            Y = money.Y,
                            Color = DisplayColor.Default
                        };

                        visibleArgs.Add(moneyInfo);

                        break;
                    case Monster creature:
                        var creatureInfo = new CreatureInfo
                        {
                            Id = creature.Serial,
                            Sprite = creature.Image,
                            X = creature.X,
                            Y = creature.Y,
                            CreatureType = CreatureType.Normal,
                            /*
                             * Normal = 0
                             * WalkThrough = 1
                             * Merchant = 2
                             * WhiteSquare = 3
                             * User = 4
                             */
                            Direction = (Direction)creature.Direction,
                            Name = creature.Template.BaseName
                        };

                        visibleArgs.Add(creatureInfo);

                        break;
                    case Mundane npc:
                        var npcInfo = new CreatureInfo
                        {
                            Id = npc.Serial,
                            Sprite = npc.Sprite,
                            X = npc.X,
                            Y = npc.Y,
                            CreatureType = CreatureType.Merchant,
                            Direction = (Direction)npc.Direction,
                            Name = npc.Template.Name
                        };

                        //none visible creature that can be seen
                        //if (creature.Visibility is not VisibilityType.Normal &&
                        //    (Aisling.IsAdmin || Aisling.Script.CanSee(creature)))
                        //    creatureInfo.Sprite = 405;

                        visibleArgs.Add(npcInfo);
                        break;
                }

            Send(args);
        }
    }

    /// <summary>
    /// 0x36 - World User List
    /// </summary>
    /// <param name="aislings"></param>
    public void SendWorldList(IEnumerable<Aisling> aislings)
    {
        var worldList = new List<WorldListMemberInfo>();
        var orderedAislings = aislings.OrderByDescending(aisling => aisling.BaseMp * 2 + aisling.BaseHp);

        var args = new WorldListArgs
        {
            CountryList = worldList
        };

        foreach (var aisling in orderedAislings)
        {
            var classList = aisling.Path switch
            {
                Class.Peasant => 0,
                Class.Berserker => 1,
                Class.Defender => 2,
                Class.Assassin => 3,
                Class.Cleric => 4,
                Class.Arcanus => 5,
                Class.Monk => 6,
                _ => 0
            };

            var jobClass = aisling.JobClass switch
            {
                Job.None => "",
                Job.Thief => "Thief",
                Job.DarkKnight => "Dark Knight",
                Job.Templar => "Templar",
                Job.Ninja => "Ninja",
                Job.SharpShooter => "Sharp Shooter",
                Job.Oracle => "Oracle",
                Job.Bard => "Bard",
                Job.Summoner => "Summoner",
                Job.Samurai => "Samurai",
                Job.ShaolinMonk => "Shaolin Monk",
                Job.Necromancer => "Necromancer",
                Job.Dragoon => "Dragoon",
                _ => ""
            };

            var vitality = $"Vit: {aisling.BaseHp + aisling.BaseMp * 2}";

            if (!jobClass.IsNullOrEmpty())
                vitality = jobClass;

            if (aisling.ExpLevel + aisling.AbpLevel == 1000)
                vitality = "Ascendant";

            var arg = new WorldListMemberInfo
            {
                BaseClass = (BaseClass)classList,
                Color = (WorldListColor)GetUserColor(aisling),
                IsMaster = aisling.Stage >= ClassStage.Master,
                Name = aisling.Username,
                SocialStatus = (SocialStatus)aisling.ActiveStatus,
                Title = aisling.GameMaster
                    ? "Game Master"
                    : vitality
            };

            worldList.Add(arg);
        }

        Send(args);
    }

    private ListColor GetUserColor(Player user)
    {
        var color = ListColor.White;
        // Players within level range are Orange
        if (Aisling.ExpLevel > user.ExpLevel)
            if (Aisling.ExpLevel - user.ExpLevel < 30)
                color = ListColor.Orange;
        // Players who have ascended are Teal
        if (user.ExpLevel + user.AbpLevel == 1000)
            color = ListColor.Teal;
        // Game Masters are Red
        if (user.GameMaster)
            color = ListColor.Red;
        //if (user.Knight)
        //    color = ListColor.Green;
        //if (user.ArenaHost)
        //    color = ListColor.Clan;
        return color;
    }

    /// <summary>
    /// 0x2E - Send Field Map
    /// </summary>
    /// <param name="worldMap"></param>
    public void SendWorldMap()
    {
        var mapExists = ServerSetup.Instance.GlobalWorldMapTemplateCache.TryGetValue(Aisling.World, out var portal);
        if (!mapExists) return;
        MapOpen = true;
        var name = $"field{portal.FieldNumber:000}";
        var warpsList = new List<WorldMapNodeInfo>();

        foreach (var warp in portal.Portals.Where(warps => warps?.Destination != null))
        {
            var map = ServerSetup.Instance.GlobalMapCache[warp.Destination.AreaID];
            var x = warp.Destination.Location.X;
            var y = warp.Destination.Location.Y;
            var addWarp = new WorldMapNodeInfo
            {
                CheckSum = EphemeralRandomIdGenerator<ushort>.Shared.NextId, // map.Hash
                DestinationPoint = new Point(x, y),
                MapId = (ushort)map.ID,
                ScreenPosition = new Point(warp.PointY, warp.PointX), // Client expects this backwards
                Text = warp.DisplayName,
            };

            warpsList.Add(addWarp);
        }

        var args = new WorldMapArgs
        {
            FieldIndex = (byte)portal.FieldNumber,
            FieldName = name,
            Nodes = warpsList
        };

        Send(args);
    }

    #endregion

    #region WorldClient Logic

    public WorldClient AislingToGhostForm()
    {
        Aisling.Flags = AislingFlags.Ghost;
        Aisling.CurrentHp = 0;
        Aisling.CurrentMp = 0;
        Aisling.RegenTimerDisabled = true;
        UpdateDisplay();
        Task.Delay(500).ContinueWith(ct => { ClientRefreshed(); });
        return this;
    }

    public WorldClient GhostFormToAisling()
    {
        Aisling.Flags = AislingFlags.Normal;
        Aisling.RegenTimerDisabled = false;
        UpdateDisplay();
        Task.Delay(500).ContinueWith(ct => { ClientRefreshed(); });
        return this;
    }

    public void LearnSkill(Mundane source, SkillTemplate subject, string message)
    {
        var canLearn = false;

        if (subject.Prerequisites != null) canLearn = PayPrerequisites(subject.Prerequisites);
        if (subject.LearningRequirements is { Count: > 0 }) canLearn = subject.LearningRequirements.TrueForAll(PayPrerequisites);
        if (!canLearn)
        {
            this.SendOptionsDialog(source, "You do not seem to possess what is necessary to learn this skill");
            return;
        }

        var skill = Skill.GiveTo(Aisling, subject.Name);
        if (skill) LoadSkillBook();

        // Recall message set in message variable back to the npc
        this.SendOptionsDialog(source, message);
        Aisling.SendAnimationNearby(subject.TargetAnimation, null, Aisling.Serial);

        // After learning, ensure player's modifiers are set
        var item = new Item();
        item.ReapplyItemModifiers(this);
    }

    public void LearnSpell(Mundane source, SpellTemplate subject, string message)
    {
        var canLearn = false;

        if (subject.Prerequisites != null) canLearn = PayPrerequisites(subject.Prerequisites);
        if (subject.LearningRequirements is { Count: > 0 }) canLearn = subject.LearningRequirements.TrueForAll(PayPrerequisites);
        if (!canLearn)
        {
            this.SendOptionsDialog(source, "You do not seem to possess what is necessary to learn this spell");
            return;
        }

        var spell = Spell.GiveTo(Aisling, subject.Name);
        if (spell) LoadSpellBook();

        // Recall message set in message variable back to the npc
        this.SendOptionsDialog(source, message);
        Aisling.SendAnimationNearby(subject.TargetAnimation, null, Aisling.Serial);

        // After learning, ensure player's modifiers are set
        var item = new Item();
        item.ReapplyItemModifiers(this);
    }

    public void ClientRefreshed()
    {
        if (Aisling.Map.ID != ServerSetup.Instance.Config.TransitionZone) MapOpen = false;
        if (MapOpen) return;
        if (!CanRefresh) return;

        SendMapInfo();
        SendLocation();
        SendAttributes(StatUpdateType.Full);
        UpdateDisplay(true);

        var objects = ObjectManager.GetObjects(Aisling.Map, s => s.WithinRangeOf(Aisling), ObjectManager.Get.AllButAislings).ToList();

        if (objects.Count != 0)
        {
            objects.Reverse();
            SendVisibleEntities(objects);
        }

        SendMapLoadComplete();
        SendDisplayAisling(Aisling);
        SendRefreshResponse();

        Aisling.Client.LastMapUpdated = DateTime.UtcNow;
        Aisling.Client.LastLocationSent = DateTime.UtcNow;
        Aisling.Client.LastClientRefresh = DateTime.UtcNow;
    }

    public WorldClient SystemMessage(string message)
    {
        SendServerMessage(ServerMessageType.ActiveMessage, message);
        return this;
    }

    public async Task<bool> Save()
    {
        if (Aisling == null) return false;
        if (Aisling.Serial == 0) return false;
        LastSave = DateTime.UtcNow;
        return await StorageManager.AislingBucket.Save(Aisling).ConfigureAwait(false);
    }

    public WorldClient UpdateDisplay(bool excludeSelf = false)
    {
        if (!excludeSelf)
            SendDisplayAisling(Aisling);

        var nearbyAislings = Aisling.AislingsNearby();

        if (nearbyAislings.Count == 0) return this;

        var self = Aisling;

        foreach (var nearby in nearbyAislings)
        {
            if (nearby is null) return this;
            if (self.Serial == nearby.Serial) continue;

            if (self.CanSeeSprite(nearby))
                nearby.ShowTo(self);
            else
                nearby.HideFrom(self);

            if (nearby.CanSeeSprite(self))
                self.ShowTo(nearby);
            else
                self.HideFrom(nearby);
        }

        return this;
    }

    public WorldClient PayItemPrerequisites(LearningPredicate prerequisites)
    {
        if (prerequisites.ItemsRequired is not { Count: > 0 }) return this;

        // Item Required
        foreach (var retainer in prerequisites.ItemsRequired)
        {
            // Inventory Fetch
            var items = Aisling.Inventory.Get(i => i.Template.Name == retainer.Item);

            // Loop for item
            foreach (var item in items)
            {
                // Loop for non-stacked item
                if (!item.Template.CanStack)
                {
                    for (var j = 0; j < retainer.AmountRequired; j++)
                    {
                        var itemLoop = Aisling.Inventory.Get(i => i.Template.Name == retainer.Item);
                        Aisling.Inventory.RemoveFromInventory(this, itemLoop.FirstOrDefault());
                    }

                    continue;
                }

                // Handle stacked item
                Aisling.Inventory.RemoveRange(Aisling.Client, item, retainer.AmountRequired);
            }
        }

        return this;
    }

    public bool PayPrerequisites(LearningPredicate prerequisites)
    {
        if (prerequisites == null) return false;
        if (Aisling.GameMaster) return true;

        PayItemPrerequisites(prerequisites);
        {
            if (prerequisites.GoldRequired > 0)
            {
                if (Aisling.GoldPoints < prerequisites.GoldRequired) return false;
                Aisling.GoldPoints -= prerequisites.GoldRequired;
            }

            SendAttributes(StatUpdateType.ExpGold);
            return true;
        }
    }

    public bool CheckReqs(WorldClient client, Item item)
    {
        // Game Master check
        if (client.Aisling.GameMaster)
        {
            if (item.Durability >= 1)
            {
                return true;
            }
        }

        // Durability check
        if (item.Durability <= 0 && item.Template.Flags.FlagIsSet(ItemFlags.Equipable))
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "I'll need to repair this before I can use it again.");
            return false;
        }

        // Level check
        if (client.Aisling.ExpLevel < item.Template.LevelRequired)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "This item is simply too powerful for me.");
            return false;
        }

        // Stage check
        if (client.Aisling.Stage < item.Template.StageRequired)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "I do not have the expertise for this.");
            return false;
        }

        // Job Level Check
        if (client.Aisling.AbpLevel < item.Template.JobLevelRequired)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, "I do not have the job level for this.");
            return false;
        }

        // Class check
        if (item.Template.Class != Class.Peasant)
        {
            // Past class check
            if (item.Template.Class != client.Aisling.PastClass)
            {
                // Current class check
                if (item.Template.Class != client.Aisling.Path)
                {
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "This doesn't fit my class.");
                    return false;
                }
            }
        }

        // Job Check
        if (item.Template.JobRequired != Job.None)
        {
            if (client.Aisling.JobClass != item.Template.JobRequired)
            {
                client.SendServerMessage(ServerMessageType.ActiveMessage, "This doesn't quite match my profession.");
                return false;
            }
        }

        switch (item.Template.Gender)
        {
            case Enums.Gender.Male:
                var canUseMale = Aisling.Gender is Enums.Gender.Male;
                if (canUseMale) return true;
                break;
            case Enums.Gender.Female:
                var canUseFemale = Aisling.Gender is Enums.Gender.Female;
                if (canUseFemale) return true;
                break;
            case Enums.Gender.Unisex:
                return true;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, "Doesn't seem to fit.");
        return false;
    }

    public WorldClient Insert(bool update, bool delete)
    {
        var obj = ObjectManager.GetObject<Aisling>(null, aisling => aisling.Serial == Aisling.Serial
                                                                    || string.Equals(aisling.Username, Aisling.Username, StringComparison.CurrentCultureIgnoreCase));

        obj?.Remove(update, delete);
        ObjectManager.AddObject(Aisling);
        return this;
    }

    public void Interrupt()
    {
        WorldServer.CancelIfCasting(this);
        ClientRefreshed();
    }

    public void WorldMapInterrupt()
    {
        WorldServer.CancelIfCasting(this);
    }

    public void ForgetSkill(string s)
    {
        var subject = Aisling.SkillBook.Skills.Values
            .FirstOrDefault(i =>
                i?.Template != null && !string.IsNullOrEmpty(i.Template.Name) &&
                string.Equals(i.Template.Name, s, StringComparison.CurrentCultureIgnoreCase));

        if (subject != null)
        {
            ForgetSkillSend(subject);
            DeleteSkillFromDb(subject);
        }

        LoadSkillBook();
    }

    public void ForgetSkills()
    {
        var skills = Aisling.SkillBook.Skills.Values
            .Where(i => i?.Template != null).ToList();

        foreach (var skill in skills)
        {
            Task.Delay(100).ContinueWith(_ => ForgetSkillSend(skill));
            DeleteSkillFromDb(skill);
        }

        LoadSkillBook();
    }

    private void ForgetSkillSend(Skill skill)
    {
        Aisling.SkillBook.Remove(this, skill.Slot);
    }

    public void ForgetSpell(string s)
    {
        var subject = Aisling.SpellBook.Spells.Values
            .FirstOrDefault(i =>
                i?.Template != null && !string.IsNullOrEmpty(i.Template.Name) &&
                string.Equals(i.Template.Name, s, StringComparison.CurrentCultureIgnoreCase));

        if (subject != null)
        {
            ForgetSpellSend(subject);
            DeleteSpellFromDb(subject);
        }

        LoadSpellBook();
    }

    public void ForgetSpells()
    {
        var spells = Aisling.SpellBook.Spells.Values
            .Where(i => i?.Template != null).ToList();

        foreach (var spell in spells)
        {
            Task.Delay(100).ContinueWith(_ => ForgetSpellSend(spell));
            DeleteSpellFromDb(spell);
        }

        LoadSpellBook();
    }

    public void ForgetSpellSend(Spell spell)
    {
        Aisling.SpellBook.Remove(this, spell.Slot);
    }

    public void TrainSkill(Skill skill)
    {
        if (skill.Level >= skill.Template.MaxLevel) return;

        var levelUpRand = Generator.RandomPercentPrecise();
        if (skill.Uses >= 40 && skill.Template.SkillType != SkillScope.Assail)
            levelUpRand += 0.1;

        switch (levelUpRand)
        {
            case <= 0.99:
                return;
            case <= 0.995:
                skill.Level++;
                skill.Uses = 0;
                break;
            case <= 1:
                skill.Level++;
                skill.Level++;
                skill.Uses = 0;
                break;
        }

        TrainSkillAnnounce(skill);
        SendAddSkillToPane(skill);
        skill.CurrentCooldown = skill.Template.Cooldown;
        SendCooldown(true, skill.Slot, skill.CurrentCooldown);
    }

    private void TrainSkillAnnounce(Skill skill)
    {
        if (Aisling.Stage < ClassStage.Master)
        {
            if (skill.Level > 100) skill.Level = 100;
            SendServerMessage(ServerMessageType.ActiveMessage,
                skill.Level >= 100
                    ? string.Format(CultureInfo.CurrentUICulture, "{0} locked until master", skill.Template.Name)
                    : string.Format(CultureInfo.CurrentUICulture, "{0}, Lv:{1}", skill.Template.Name, skill.Level));
            return;
        }

        switch (skill.Template.SkillType)
        {
            case SkillScope.Assail:
                {
                    if (skill.Level > 350) skill.Level = 350;
                    SendServerMessage(ServerMessageType.ActiveMessage,
                        skill.Level >= 350
                            ? string.Format(CultureInfo.CurrentUICulture, "{0} mastered!", skill.Template.Name)
                            : string.Format(CultureInfo.CurrentUICulture, "{0}, Lv:{1}", skill.Template.Name, skill.Level));
                    break;
                }
            case SkillScope.Ability:
                {
                    if (skill.Level > 500) skill.Level = 500;
                    SendServerMessage(ServerMessageType.ActiveMessage,
                        skill.Level >= 500
                            ? string.Format(CultureInfo.CurrentUICulture, "{0} mastered!", skill.Template.Name)
                            : string.Format(CultureInfo.CurrentUICulture, "{0}, Lv:{1}", skill.Template.Name, skill.Level));
                    break;
                }
        }
    }

    public void TrainSpell(Spell spell)
    {
        if (spell.Level >= spell.Template.MaxLevel) return;

        var levelUpRand = Generator.RandomPercentPrecise();
        if (spell.Casts >= 40)
            levelUpRand += 0.1;

        switch (levelUpRand)
        {
            case <= 0.93:
                return;
            case <= 0.98:
                spell.Level++;
                spell.Casts = 0;
                break;
            case <= 1:
                spell.Level++;
                spell.Level++;
                spell.Casts = 0;
                break;
        }

        SendAddSpellToPane(spell);
        spell.CurrentCooldown = spell.Template.Cooldown;
        SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        SendServerMessage(ServerMessageType.ActiveMessage,
            spell.Level >= 100
                ? string.Format(CultureInfo.CurrentUICulture, "{0} has been mastered.", spell.Template.Name)
                : string.Format(CultureInfo.CurrentUICulture, "{0} improved, Lv:{1}", spell.Template.Name, spell.Level));
    }

    public bool GiveItem(string itemName)
    {
        var item = new Item();
        item = item.Create(Aisling, itemName);

        return item.Template.Name != null && item.GiveTo(Aisling);
    }

    public void GiveQuantity(Aisling aisling, string itemName, int range)
    {
        var item = new Item();
        item = item.Create(aisling, itemName);
        item.Stacks = (ushort)range;
        var given = item.GiveTo(Aisling);
        if (given) return;
        Aisling.BankManager.Items.TryAdd(item.ItemId, item);
        SendServerMessage(ServerMessageType.ActiveMessage, "Issue with giving you the item directly, deposited to bank");
    }

    public void TakeAwayQuantity(Sprite owner, string item, int range)
    {
        var foundItem = Aisling.Inventory.Has(i => i.Template.Name.Equals(item, StringComparison.OrdinalIgnoreCase));
        if (foundItem == null) return;

        Aisling.Inventory.RemoveRange(Aisling.Client, foundItem, range);
    }

    public WorldClient LoggedIn(bool state)
    {
        Aisling.LoggedIn = state;

        return this;
    }

    private void Port(int i, int x = 0, int y = 0)
    {
        TransitionToMap(i, new Position(x, y));
    }

    public static void ResetLocation(WorldClient client)
    {
        var reset = 0;

        while (reset == 0)
        {
            client.Port(ServerSetup.Instance.Config.TransitionZone, ServerSetup.Instance.Config.TransitionPointX, ServerSetup.Instance.Config.TransitionPointY);
            reset++;
        }
    }

    public void Recover()
    {
        Revive();
    }

    public void GiveScar()
    {
        var item = new Legend.LegendItem
        {
            Key = $"Sp{RandomNumberGenerator.GetInt32(int.MaxValue)}ark{RandomNumberGenerator.GetInt32(int.MaxValue)}",
            IsPublic = true,
            Time = DateTime.UtcNow,
            Color = LegendColor.Red,
            Icon = (byte)LegendIcon.Warrior,
            Text = "Fragment of spark taken.."
        };

        Aisling.LegendBook.AddLegend(item, this);
    }

    public void RepairEquipment()
    {
        if (Aisling.Inventory.Items != null)
        {
            foreach (var inventory in Aisling.Inventory.Items.Where(i => i.Value != null && i.Value.Template.Flags.FlagIsSet(ItemFlags.Repairable)))
            {
                var item = inventory.Value;
                if (item.Template == null) continue;
                item.ItemQuality = item.OriginalQuality == Item.Quality.Damaged ? Item.Quality.Common : item.OriginalQuality;
                item.Tarnished = false;
                ItemQualityVariance.ItemDurability(item, item.ItemQuality);
                Aisling.Inventory.UpdateSlot(Aisling.Client, item);
            }
        }

        foreach (var (key, value) in Aisling.EquipmentManager.Equipment.Where(equip => equip.Value != null && equip.Value.Item.Template.Flags.FlagIsSet(ItemFlags.Repairable)))
        {
            var item = value.Item;
            if (item.Template == null) continue;
            item.ItemQuality = item.OriginalQuality == Item.Quality.Damaged ? Item.Quality.Common : item.OriginalQuality;
            item.Tarnished = false;
            ItemQualityVariance.ItemDurability(item, item.ItemQuality);
            SendEquipment((byte)key, item);
        }

        var reapplyMods = new Item();
        reapplyMods.ReapplyItemModifiers(Aisling.Client);

        SendAttributes(StatUpdateType.Full);
    }

    public bool Revive()
    {
        Aisling.Flags = AislingFlags.Normal;
        Aisling.RegenTimerDisabled = false;
        Aisling.CurrentHp = (long)(Aisling.MaximumHp * 0.80);
        Aisling.CurrentMp = (long)(Aisling.MaximumMp * 0.80);

        SendAttributes(StatUpdateType.Vitality);
        return Aisling.CurrentHp > 0;
    }

    public bool IsBehind(Sprite sprite)
    {
        var delta = sprite.Direction - Aisling.Direction;
        return Aisling.Position.IsNextTo(sprite.Position) && delta == 0;
    }

    public static void KillPlayer(Area map, string u)
    {
        if (u is null) return;
        var user = ObjectManager.GetObject<Aisling>(map, i => i.Username.Equals(u, StringComparison.OrdinalIgnoreCase));

        if (user != null)
        {
            user.CurrentHp = 0;
            user.Client.PlayerDeathStatusCheck(new Item());
        }
    }

    #endregion

    #region Client Work Queue

    private async Task ProcessPlayerWorkQueue()
    {
        while (ServerSetup.Instance.Running)
        {
            await _clientWorkSignal.WaitAsync(5000).ConfigureAwait(false);

            while (_clientWorkQueue.TryDequeue(out var work))
            {
                try
                {
                    work.Execute(this);
                }
                catch { }
            }
        }
    }

    public void EnqueueExperienceEvent(Aisling player, long exp, bool hunting)
    {
        _clientWorkQueue.Enqueue(new ExperienceEvent(player, exp, hunting));
        _clientWorkSignal.Release();
    }

    public void EnqueueAbilityEvent(Aisling player, int exp, bool hunting)
    {
        _clientWorkQueue.Enqueue(new AbilityEvent(player, exp, hunting));
        _clientWorkSignal.Release();
    }

    public void EnqueueDebuffAppliedEvent(Sprite affected, Debuff debuff)
    {
        _clientWorkQueue.Enqueue(new DebuffOnAppliedEvent(affected, debuff));
        _clientWorkSignal.Release();
    }

    public void EnqueueDebuffUpdatedEvent(Sprite affected, Debuff debuff)
    {
        _clientWorkQueue.Enqueue(new DebuffOnUpdatedEvent(affected, debuff));
        _clientWorkSignal.Release();
    }

    public void EnqueueBuffAppliedEvent(Sprite affected, Buff buff)
    {
        _clientWorkQueue.Enqueue(new BuffOnAppliedEvent(affected, buff));
        _clientWorkSignal.Release();
    }

    public void EnqueueBuffUpdatedEvent(Sprite affected, Buff buff)
    {
        _clientWorkQueue.Enqueue(new BuffOnUpdatedEvent(affected, buff));
        _clientWorkSignal.Release();
    }

    internal void ClientWorkExpEvent(Aisling player, long exp, bool hunting) => HandleExp(player, exp, hunting);
    internal void ClientWorkApEvent(Aisling player, int exp, bool hunting) => HandleAp(player, exp, hunting);
    internal void ClientWorkDebuffAppliedEvent(Sprite affected, Debuff debuff) => debuff.OnApplied(affected, debuff);
    internal void ClientWorkDebuffUpdatedEvent(Sprite affected, Debuff debuff) => debuff.Update(affected);
    internal void ClientWorkBuffAppliedEvent(Sprite affected, Buff buff) => buff.OnApplied(affected, buff);
    internal void ClientWorkBuffUpdatedEvent(Sprite affected, Buff buff) => buff.Update(affected);

    #endregion

    #region Events & Experience

    public void GiveExp(long exp)
    {
        if (exp <= 0) exp = 1;

        // Enqueue experience event
        EnqueueExperienceEvent(Aisling, exp, false);
    }

    private static void HandleExp(Aisling player, long baseExp, bool hunting)
    {
        if (baseExp <= 0) baseExp = 1;

        try
        {
            // --- 1. Compute total exp gain after all bonuses ---
            double total = baseExp;

            // Holiday multiplier
            double holiday = ServerSetup.Instance.Config.HolidayExpBonus;
            if (holiday > 1.0)
                total *= holiday;

            // Hunting buffs
            if (hunting)
            {
                if (player.HasBuff("Double XP")) total *= 2;
                if (player.HasBuff("Triple XP")) total *= 3;

                // Group multiplier
                if (player.GroupParty != null)
                {
                    int groupSize = player.GroupParty.PartyMembers.Count;
                    double adjustment = ServerSetup.Instance.Config.GroupExpBonus;

                    if (groupSize > 7)
                    {
                        double dyn = (groupSize - 7) * 0.05;
                        if (dyn < 0.75) dyn = 0.75;
                        adjustment = dyn;
                    }

                    total *= (1.0 + (groupSize - 1) * (adjustment / 100.0));
                }
            }

            // Clamp to safe long
            long totalExp = (long)Math.Min(total, long.MaxValue);

            // --- 2. Hard cap check ---
            if (long.MaxValue - player.ExpTotal < totalExp)
            {
                player.ExpTotal = long.MaxValue;
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                    "Your experience box is full, ascend to carry more");
                player.Client.SendAttributes(StatUpdateType.ExpGold);
                return;
            }

            // --- 3. Apply single-pass level-up logic ---
            long expToApply = totalExp;
            long currentNext = player.ExpNext;
            long totalAdded = 0;
            int levelsGained = 0;

            while (expToApply >= currentNext && player.ExpLevel < ServerSetup.Instance.Config.PlayerLevelCap)
            {
                expToApply -= currentNext;
                totalAdded += currentNext;
                player.ExpLevel++;
                levelsGained++;

                // recalc ExpNext for the new level (reuse your seed logic)
                double seed = player.ExpLevel * 0.1 + 0.5;
                if (player.ExpLevel > 99)
                {
                    int levelsAboveMaster = player.ExpLevel - 120;
                    int scalingFactor = Math.Min(5000 + levelsAboveMaster * 237, 100000);
                    currentNext = (long)(player.ExpLevel * seed * scalingFactor);
                }
                else
                {
                    currentNext = (long)(player.ExpLevel * seed * 5000);
                }

                if (currentNext <= 0)
                {
                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                        "Issue leveling up; Error: Mt. Everest");
                    return;
                }

                // apply stat gains inline (no recursion)
                player.StatPoints += (player.ExpLevel >= 250)
                    ? (short)1
                    : (short)ServerSetup.Instance.Config.StatsPerLevel;

                player.BaseHp += ((int)(ServerSetup.Instance.Config.HpGainFactor * player._Con * 0.65)).IntClamp(0, 300);
                player.BaseMp += ((int)(ServerSetup.Instance.Config.MpGainFactor * player._Wis * 0.45)).IntClamp(0, 300);
            }

            // apply remaining exp to next level progress
            player.ExpNext = currentNext - expToApply;
            player.ExpTotal += totalAdded + expToApply;

            // --- 4. Output feedback ---
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                $"Received {totalExp:n0} experience points!");

            if (levelsGained > 0)
            {
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                    $"You leveled up {levelsGained} time(s)! New Insight: {player.ExpLevel}");
                player.SendAnimationNearby(79, null, player.Serial, 64);

                player.CurrentHp = player.MaximumHp;
                player.CurrentMp = player.MaximumMp;
                player.Client.SendAttributes(StatUpdateType.Full);
            }
            else
            {
                player.Client.SendAttributes(StatUpdateType.ExpGold);
            }
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"Issue giving {player.Username} experience.");
            SentrySdk.CaptureException(ex);
        }
    }

    public void GiveAp(int exp)
    {
        if (exp <= 0) exp = 1;

        // Enqueue ap event
        EnqueueAbilityEvent(Aisling, exp, false);
    }

    private static void HandleAp(Aisling player, long baseAp, bool hunting)
    {
        if (baseAp <= 0) baseAp = 1;

        try
        {
            // -------- 1) Compute final AP after all bonuses (once) --------
            double total = baseAp;

            // Holiday multiplier
            double holiday = ServerSetup.Instance.Config.HolidayExpBonus;
            if (holiday > 1.0)
                total *= holiday;

            // Hunting buffs and group bonus
            if (hunting)
            {
                if (player.HasBuff("Double XP")) total *= 2.0;
                if (player.HasBuff("Triple XP")) total *= 3.0;

                if (player.GroupParty != null)
                {
                    int groupSize = player.GroupParty.PartyMembers.Count;
                    double adjustment = ServerSetup.Instance.Config.GroupExpBonus; // % value

                    if (groupSize > 7)
                    {
                        double dyn = (groupSize - 7) * 0.05;
                        if (dyn < 0.75) dyn = 0.75;
                        adjustment = dyn;
                    }

                    // multiply once: (1 + (groupSize-1) * %bonus)
                    total *= (1.0 + (groupSize - 1) * (adjustment / 100.0));
                }
            }

            // clamp to long
            long totalAp = (long)Math.Min(total, long.MaxValue);

            // -------- 2) Hard cap behavior --------
            if (long.MaxValue - player.AbpTotal < totalAp)
            {
                player.AbpTotal = long.MaxValue;
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                    "Your ability box is full, ascend to carry more");
                player.Client.SendAttributes(StatUpdateType.ExpGold);
                return;
            }

            // -------- 3) Paragon-style: 500+ just accrues --------
            if (player.AbpLevel >= 500)
            {
                player.AbpNext = 0;
                player.AbpTotal += totalAp;
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                    $"Received {totalAp:n0} ability points!");
                player.Client.SendAttributes(StatUpdateType.ExpGold);
                return;
            }

            // -------- 4) Single-pass rank ups --------
            long apToApply = totalAp;
            long currentNext = Math.Max(player.AbpNext, 1); // safety
            long totalAdded = 0;
            int ranksGained = 0;

            // local function for your AP next calc
            static long CalcApNext(int level)
            {
                // seed: L*0.5 + 0.8; Next = L * seed * 5000
                double seed = level * 0.5 + 0.8;
                double dnext = level * seed * 5000.0;
                long next = (long)Math.Round(Math.Clamp(dnext, 1.0, int.MaxValue)); // AbpNext is int
                return next;
            }

            while (apToApply >= currentNext && player.AbpLevel < ServerSetup.Instance.Config.PlayerLevelCap)
            {
                apToApply -= currentNext;
                totalAdded += currentNext;

                // apply AP rank gains inline (no recursion)
                player.AbpLevel++;
                ranksGained++;

                player.StatPoints += 1;

                player.BaseHp += ((int)(ServerSetup.Instance.Config.HpGainFactor * player._Con * 1.23)).IntClamp(0, 1000);
                player.BaseMp += ((int)(ServerSetup.Instance.Config.MpGainFactor * player._Wis * 0.90)).IntClamp(0, 1000);

                // compute next threshold for the NEW level
                currentNext = CalcApNext(player.AbpLevel);

                if (currentNext <= 0)
                {
                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                        "Issue leveling up; Error: Mt. Everest");
                    return;
                }
            }

            // leftover progress toward next rank
            long remainingTowardNext = apToApply;              // what we didn't spend leveling
            player.AbpNext = (int)Math.Clamp(currentNext - remainingTowardNext, 1, int.MaxValue);

            // total AP always increases by everything earned
            player.AbpTotal += totalAdded + remainingTowardNext;

            // -------- 5) Feedback (one message for AP, one summary for levels) --------
            player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                $"Received {totalAp:n0} ability points!");

            if (ranksGained > 0)
            {
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                    $"{ServerSetup.Instance.Config.AbilityUpMessage}, Job Level: {player.AbpLevel} " +
                    $"(+{ranksGained} rank{(ranksGained == 1 ? "" : "s")})");
                player.SendAnimationNearby(385, null, player.Serial, 75);

                if (player.AbpLevel == 500)
                {
                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage,
                        "You have reached the maximum level. Congratulations!");

                    var legend = new Legend.LegendItem
                    {
                        Key = $"{player.Username}{player.Serial}Maxed",
                        IsPublic = true,
                        Time = DateTime.UtcNow,
                        Color = LegendColor.RedPurpleG5,
                        Icon = (byte)LegendIcon.Victory,
                        Text = "Acendant of Chaos"
                    };

                    player.LegendBook.AddLegend(legend, player.Client);
                }

                player.CurrentHp = player.MaximumHp;
                player.CurrentMp = player.MaximumMp;
                player.Client.SendAttributes(StatUpdateType.Full);
            }
            else
            {
                player.Client.SendAttributes(StatUpdateType.ExpGold);
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"Issue giving {player.Username} ability points.");
            SentrySdk.CaptureException(e);
        }
    }

    #endregion

    #region Warping & Maps

    private WorldClient RefreshMap(bool updateView = false)
    {
        try
        {
            MapUpdating = true;

            if (Aisling.IsBlind)
                SendAttributes(StatUpdateType.Secondary);

            SendMapChangePending();
            SendMapInfo();
            SendLocation();

            if (Aisling.Map is not { Script.Item1: null }) return this;
            if (string.IsNullOrEmpty(Aisling.Map.ScriptKey)) return this;

            if (ScriptManager.TryCreate<AreaScript>(Aisling.Map.ScriptKey, out var script, Aisling.Map) && script != null)
                Aisling.Map.Script = Tuple.Create(Aisling.Map.ScriptKey, script);
        }
        catch
        {
            MapUpdating = false;
        }

        return this;
    }

    public WorldClient TransitionToMap(Area area, Position position)
    {
        if (area == null) return this;

        if (area.ID != Aisling.CurrentMapId)
        {
            LeaveArea(area.ID, true, true);

            Aisling.LastPosition = new Position(Aisling.Pos);
            Aisling.Pos = new Vector2(position.X, position.Y);
            Aisling.CurrentMapId = area.ID;

            // Increment playerCount for current map
            Aisling.Map.OnPlayerEnter();

            Enter();
        }
        else
        {
            Aisling.LastPosition = new Position(Aisling.Pos);
            Aisling.Pos = new Vector2(position.X, position.Y);
            Aisling.CurrentMapId = area.ID;
            WarpToAndRefresh(position);
        }

        // ToDo: Logic to only play this if a menu is opened.
        this.CloseDialog();

        return this;
    }

    public WorldClient TransitionToMap(int area, Position position)
    {
        if (!ServerSetup.Instance.GlobalMapCache.TryGetValue(area, out var target)) return this;
        if (target == null) return this;

        if (Aisling.LastMapId != target.ID)
        {
            LeaveArea(target.ID, true, true);

            Aisling.LastPosition = new Position(Aisling.Pos);
            Aisling.Pos = new Vector2(position.X, position.Y);
            Aisling.CurrentMapId = target.ID;

            // Increment playerCount for current map
            Aisling.Map.OnPlayerEnter();

            Enter();
        }
        else
        {
            Aisling.LastPosition = new Position(Aisling.Pos);
            Aisling.Pos = new Vector2(position.X, position.Y);
            Aisling.CurrentMapId = target.ID;
            WarpToAndRefresh(position);
        }

        // ToDo: Logic to only play this if a menu is opened.
        this.CloseDialog();

        return this;
    }

    private void WarpToAdjacentMap(WarpTemplate warps)
    {
        if (warps.WarpType == WarpType.World) return;

        if (!Aisling.GameMaster)
        {
            var totalLevel = Aisling.ExpLevel + Aisling.AbpLevel;
            if (warps.LevelRequired > 0 && totalLevel < warps.LevelRequired)
            {
                var msgTier = Math.Abs(totalLevel - warps.LevelRequired);

                SendServerMessage(ServerMessageType.ActiveMessage, msgTier <= 15
                    ? $"You're too afraid to enter. {{=c{warps.LevelRequired} REQ"
                    : $"{{=bNightmarish visions of your own death repel you. {{=c{warps.LevelRequired} REQ)");

                return;
            }
        }

        if (Aisling.Map.ID != warps.To.AreaID)
        {
            TransitionToMap(warps.To.AreaID, warps.To.Location);
        }
        else
        {
            WarpToAndRefresh(warps.To.Location);
        }
    }

    public void WarpTo(Position position)
    {
        Aisling.Pos = new Vector2(position.X, position.Y);
    }

    public void WarpToAndRefresh(Position position)
    {
        Aisling.Pos = new Vector2(position.X, position.Y);
        ClientRefreshed();
    }

    public void CheckWarpTransitions(WorldClient client)
    {
        foreach (var (_, value) in ServerSetup.Instance.GlobalWarpTemplateCache)
        {
            var breakOuterLoop = false;
            if (value.ActivationMapId != client.Aisling.CurrentMapId) continue;

            lock (_warpCheckLock)
            {
                foreach (var warp in value.Activations.Where(o =>
                             o.Location.X == (int)client.Aisling.Pos.X &&
                             o.Location.Y == (int)client.Aisling.Pos.Y))
                {
                    if (value.WarpType == WarpType.Map)
                    {
                        if (client.Aisling.Map.ID == value.To.AreaID)
                        {
                            WarpToAndRefresh(value.To.Location);
                            breakOuterLoop = true;
                            break;
                        }

                        // Decrement playerCount for current map
                        client.Aisling.Map.OnPlayerLeave();

                        client.WarpToAdjacentMap(value);
                        breakOuterLoop = true;
                        break;
                    }

                    if (value.WarpType != WarpType.World) continue;
                    if (!ServerSetup.Instance.GlobalWorldMapTemplateCache.ContainsKey(value.To.PortalKey)) return;
                    if (client.Aisling.World != value.To.PortalKey) client.Aisling.World = (byte)value.To.PortalKey;

                    // Decrement playerCount for current map on World Map transition
                    client.Aisling.Map.OnPlayerLeave();

                    var portal = new PortalSession();
                    PortalSession.TransitionToMap(client);
                    breakOuterLoop = true;
                    client.WorldMapInterrupt();
                    break;
                }
            }

            if (breakOuterLoop) break;
        }
    }

    public void CheckWarpTransitions(WorldClient client, int x, int y)
    {
        foreach (var (_, value) in ServerSetup.Instance.GlobalWarpTemplateCache)
        {
            var breakOuterLoop = false;
            if (value.ActivationMapId != client.Aisling.CurrentMapId) continue;

            lock (_warpCheckLock)
            {
                foreach (var _ in value.Activations.Where(o =>
                             o.Location.X == x &&
                             o.Location.Y == y))
                {
                    if (value.WarpType == WarpType.Map)
                    {
                        // Decrement playerCount for current map
                        client.Aisling.Map.OnPlayerLeave();

                        client.WarpToAdjacentMap(value);
                        breakOuterLoop = true;
                        break;
                    }

                    if (value.WarpType != WarpType.World) continue;
                    if (!ServerSetup.Instance.GlobalWorldMapTemplateCache.ContainsKey(value.To.PortalKey)) return;
                    if (client.Aisling.World != value.To.PortalKey) client.Aisling.World = (byte)value.To.PortalKey;

                    // Decrement playerCount for current map on World Map transition
                    client.Aisling.Map.OnPlayerLeave();

                    var portal = new PortalSession();
                    PortalSession.TransitionToMap(client);
                    breakOuterLoop = true;
                    client.WorldMapInterrupt();
                    break;
                }
            }

            if (breakOuterLoop) break;
        }
    }

    private void ReapplyKillCount()
    {
        var hasKills = ServerSetup.Instance.GlobalKillRecordCache.TryGetValue(Aisling.Serial, out var killRecords);
        if (hasKills)
        {
            Aisling.MonsterKillCounters = killRecords;
        }
    }

    public WorldClient Enter()
    {
        Insert(true, false);
        RefreshMap();
        UpdateDisplay(true);
        CompleteMapTransition();

        Aisling.Client.LastMapUpdated = DateTime.UtcNow;
        Aisling.Client.LastLocationSent = DateTime.UtcNow;
        Aisling.Map.Script.Item2.OnMapEnter(this);

        return this;
    }

    public WorldClient LeaveArea(int travelTo, bool update = false, bool delete = false)
    {
        if (Aisling.LastMapId == ushort.MaxValue) Aisling.LastMapId = Aisling.CurrentMapId;

        Aisling.Remove(update, delete);

        if (Aisling.LastMapId != travelTo && Aisling.Map.Script.Item2 != null)
            Aisling.Map.Script.Item2.OnMapExit(this);

        return this;
    }

    private void CompleteMapTransition()
    {
        try
        {
            foreach (var (_, area) in ServerSetup.Instance.GlobalMapCache)
            {
                if (Aisling.CurrentMapId != area.ID) continue;
                var mapFound = ServerSetup.Instance.GlobalMapCache.TryGetValue(area.ID, out var newMap);
                if (mapFound)
                {
                    Aisling.CurrentMapId = newMap.ID;

                    var onMap = Area.IsLocationOnMap(Aisling);
                    if (!onMap)
                    {
                        TransitionToMap(3052, new Position(27, 18));
                        SendServerMessage(ServerMessageType.OrangeBar1, "Something grabs your hand...");
                        return;
                    }

                    if (newMap.ID == 7000)
                    {
                        SendServerMessage(ServerMessageType.ScrollWindow,
                            "{=bLife{=a, all that you know, love, and cherish. Everything, and the very fabric of their being. \n\nThe aisling spark, creativity, passion. All of that lives within you." +
                            "\n\nThis story begins shortly after Anaman Pact successfully revives {=bChadul{=a. \n\n-{=cYou feel a sense of unease come over you{=a-");
                    }
                }
                else
                {
                    TransitionToMap(3052, new Position(27, 18));
                    SendServerMessage(ServerMessageType.OrangeBar1, "Something grabs your hand...");
                    return;
                }
            }

            var objects = ObjectManager.GetObjects(Aisling.Map, s => s.WithinRangeOf(Aisling), ObjectManager.Get.AllButAislings).ToList();

            if (objects.Count != 0)
            {
                objects.Reverse();
                SendVisibleEntities(objects);
            }

            SendMapChangeComplete();

            if (LastMap == null || LastMap.Music != Aisling.Map.Music)
            {
                SendSound((byte)Aisling.Map.Music, true);
            }

            Aisling.LastMapId = Aisling.CurrentMapId;
            LastMap = Aisling.Map;

            if (Aisling.DiscoveredMaps.All(i => i != Aisling.CurrentMapId))
                AddDiscoveredMapToDb();

            SendMapLoadComplete();
            SendDisplayAisling(Aisling);
        }
        catch { }
        finally
        {
            MapUpdating = false;
        }
    }

    #endregion

    #region SQL

    private static bool ExecuteWithRetry(Action action, int maxAttempts = 4, int baseDelayMs = 40)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                action();
                return true;
            }
            catch (SqlException ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                Thread.Sleep(Math.Min(500, baseDelayMs * (1 << (attempt - 1))));
                continue;
            }
        }

        return false;
    }

    private static bool IsTransient(SqlException ex)
    {
        foreach (SqlError err in ex.Errors)
        {
            // -2   = Command timeout
            // 1205 = Deadlock victim
            // 1222 = Lock request timeout
            if (err.Number is -2 or 1205 or 1222)
                return true;
        }

        return false;
    }

    public void DeleteSkillFromDb(Skill skill)
    {
        if (skill.SkillName is null)
            return;

        try
        {
            var success = ExecuteWithRetry(() =>
            {
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();

                const string sql =
                    "DELETE FROM dbo.PlayersSkillBook " +
                    "WHERE SkillName = @SkillName AND Serial = @AislingSerial";

                sConn.Execute(
                    sql,
                    new
                    {
                        skill.SkillName,
                        AislingSerial = (long)Aisling.Serial
                    },
                    commandTimeout: 5);
            });

            if (!success)
            {
                ServerSetup.EventsLogger($"DeleteSkillFromDb failed after retries for {skill.SkillName}", LogLevel.Error);
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }

    public void DeleteSpellFromDb(Spell spell)
    {
        if (spell.SpellName is null)
            return;

        try
        {
            var success = ExecuteWithRetry(() =>
            {
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();

                const string sql =
                    "DELETE FROM dbo.PlayersSpellBook " +
                    "WHERE SpellName = @SpellName AND Serial = @AislingSerial";

                sConn.Execute(
                    sql,
                    new
                    {
                        spell.SpellName,
                        AislingSerial = (long)Aisling.Serial
                    },
                    commandTimeout: 5);
            });

            if (!success)
            {
                ServerSetup.EventsLogger($"DeleteSpellFromDb failed after retries for {spell.SpellName}", LogLevel.Error);
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }

    public void AddDiscoveredMapToDb()
    {
        try
        {
            Aisling.DiscoveredMaps.Add(Aisling.CurrentMapId);

            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("FoundMap", sConn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)Aisling.Serial;
            cmd.Parameters.Add("@MapId", SqlDbType.Int).Value = Aisling.CurrentMapId;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                SendServerMessage(ServerMessageType.ActiveMessage, "Issue with saving new found map. Contact GM");
                SentrySdk.CaptureException(e);
                return;
            }

            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }

    public void AddToIgnoreListDb(string ignored)
    {
        try
        {
            Aisling.IgnoredList.Add(ignored);

            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("IgnoredSave", sConn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)Aisling.Serial;
            cmd.Parameters.Add("@PlayerIgnored", SqlDbType.VarChar).Value = ignored;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                SendServerMessage(ServerMessageType.ActiveMessage, "Issue with saving player to ignored list. Contact GM");
                SentrySdk.CaptureException(e);
                return;
            }

            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }

    public void RemoveFromIgnoreListDb(string ignored)
    {
        if (string.IsNullOrWhiteSpace(ignored))
            return;

        Aisling.IgnoredList.Remove(ignored);

        try
        {
            var success = ExecuteWithRetry(() =>
            {
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();

                const string sql =
                    "DELETE FROM dbo.PlayersIgnoreList " +
                    "WHERE Serial = @AislingSerial AND PlayerIgnored = @ignored";

                sConn.Execute(
                    sql,
                    new
                    {
                        AislingSerial = (long)Aisling.Serial,
                        ignored
                    },
                    commandTimeout: 5);
            });

            if (!success)
            {
                ServerSetup.EventsLogger($"RemoveFromIgnoreListDb failed after retries for '{ignored}'", LogLevel.Error);
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
    }

    #endregion
}