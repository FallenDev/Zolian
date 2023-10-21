using System.Net.Sockets;
using System.Numerics;
using Chaos.Common.Definitions;
using Chaos.Extensions.Networking;
using Chaos.Cryptography.Abstractions;
using Chaos.Geometry;
using Chaos.Networking.Abstractions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Darkages.Sprites;
using Darkages.Types;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Packets.Abstractions.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Meta;
using Darkages.Models;
using Darkages.Network.Client.Abstractions;
using BodyColor = Chaos.Common.Definitions.BodyColor;
using EquipmentSlot = Chaos.Common.Definitions.EquipmentSlot;
using Gender = Chaos.Common.Definitions.Gender;
using LanternSize = Chaos.Common.Definitions.LanternSize;
using RestPosition = Chaos.Common.Definitions.RestPosition;
using BodySprite = Chaos.Common.Definitions.BodySprite;
using Darkages.Database;
using Darkages.Templates;
using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;
using Darkages.GameScripts.Affects;
using Darkages.Network.Server;
using Darkages.Object;
using MapFlags = Darkages.Enums.MapFlags;
using Darkages.GameScripts.Formulas;
using System.Collections.Concurrent;
using System.Diagnostics;
using Chaos.Common.Identity;
using System.Globalization;
using ServiceStack;
using Darkages.ScriptingBase;
using Darkages.Events;

namespace Darkages.Network.Client
{
    public class WorldClient : SocketClientBase, IWorldClient
    {
        private readonly IWorldServer<WorldClient> Server;
        public readonly ObjectManager ObjectHandlers = new();
        public readonly WorldServerTimer SkillSpellTimer = new(TimeSpan.FromMilliseconds(1000));
        private readonly Stopwatch _skillControl = new();
        private readonly Stopwatch _statusControl = new();
        private readonly Stopwatch _aggroMessageControl = new();
        private readonly Stopwatch _lanternControl = new();
        private readonly Stopwatch _dayDreamingControl = new();
        private readonly WorldServerTimer _lanternCheckTimer = new(TimeSpan.FromSeconds(2));
        private readonly WorldServerTimer _aggroTimer = new(TimeSpan.FromSeconds(20));
        private readonly WorldServerTimer _dayDreamingTimer = new(TimeSpan.FromSeconds(5));
        public readonly object SyncClient = new();
        public bool ExitConfirmed;
        private static readonly SortedDictionary<long, string> AggroColors = new()
        {
            {100, "b"},
            {90, "s"},
            {75, "c"},
            {25, "g"}
        };

        public Aisling Aisling { get; set; }
        public bool MapUpdating { get; set; }
        public bool MapOpen { get; set; }
        private SemaphoreSlim LoadLock { get; } = new(1, 1);
        public DateTime BoardOpened { get; set; }
        public DialogSession DlgSession { get; set; }

        public bool CanSendLocation
        {
            get
            {
                var readyTime = DateTime.UtcNow;
                return readyTime - LastLocationSent < new TimeSpan(0, 0, 0, 2);
            }
        }

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

        public bool IsMoving
        {
            get
            {
                var readyTime = DateTime.UtcNow;
                return readyTime - LastMovement > new TimeSpan(0, 0, 0, 0, 850);
            }
        }

        public bool IsWarping
        {
            get
            {
                var readyTime = DateTime.UtcNow;
                return readyTime - LastWarp < new TimeSpan(0, 0, 0, 0, ServerSetup.Instance.Config.WarpCheckRate);
            }
        }

        public bool WasUpdatingMapRecently
        {
            get
            {
                var readyTime = DateTime.UtcNow;
                return readyTime - LastMapUpdated < new TimeSpan(0, 0, 0, 0, 100);
            }
        }

        public CastInfo SpellCastInfo { get; set; }
        public DateTime LastAssail { get; set; }
        public DateTime LastSpellCast { get; set; }
        public DateTime LastSelfProfileRequest { get; set; }
        public DateTime LastItemUsed { get; set; }
        public DateTime LastWorldListRequest { get; set; }
        public DateTime LastClientRefresh { get; set; }
        public DateTime LastWarp { get; set; }
        public Item LastItemDropped { get; set; }
        public DateTime LastLocationSent { get; set; }
        public DateTime LastMapUpdated { get; set; }
        public DateTime LastMessageSent { get; set; }
        public DateTime LastMovement { get; set; }
        public DateTime LastEquip { get; set; }
        public Stopwatch Latency { get; set; } = new();
        public DateTime LastSave { get; set; }
        public DateTime LastWhisperMessageSent { get; set; }
        public PendingBuy PendingBuySessions { get; set; }
        public PendingSell PendingItemSessions { get; set; }
        public bool ShouldUpdateMap { get; set; }
        public DateTime LastNodeClicked { get; set; }
        public WorldPortal PendingNode { get; set; }
        public Position LastKnownPosition { get; set; }
        public int MapClicks { get; set; }
        public uint EntryCheck { get; set; }
        private readonly object _warpCheckLock = new();
        private readonly Queue<ExperienceEvent> _expQueue = new();
        private readonly Queue<AbilityEvent> _apQueue = new();
        private readonly Queue<DebuffEvent> _debuffApplyQueue = new();
        private readonly Queue<BuffEvent> _buffApplyQueue = new();
        private readonly Queue<DebuffEvent> _debuffUpdateQueue = new();
        private readonly Queue<BuffEvent> _buffUpdateQueue = new();
        private readonly object _expQueueLock = new();
        private readonly object _apQueueLock = new();
        private readonly object _buffQueueLockApply = new();
        private readonly object _debuffQueueLockApply = new();
        private readonly object _buffQueueLockUpdate = new();
        private readonly object _debuffQueueLockUpdate = new();
        private Task _experienceTask;
        private Task _apTask;
        private Task _applyBuff;
        private Task _applyDebuff;
        private Task _updateBuff;
        private Task _updateDebuff;

        public WorldClient([NotNull] IWorldServer<WorldClient> server, [NotNull] Socket socket,
            [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
            [NotNull] ILogger<SocketClientBase> logger) : base(socket, crypto, packetSerializer, logger)
        {
            Server = server;

            // Event-Driven Tasks
            _experienceTask = Task.Run(ProcessExperienceEvents);
            _apTask = Task.Run(ProcessAbilityEvents);
            _applyBuff = Task.Run(ProcessApplyingBuffsEvent);
            _applyDebuff = Task.Run(ProcessApplyingDebuffsEvent);
            _updateBuff = Task.Run(ProcessUpdatingBuffsEvent);
            _updateDebuff = Task.Run(ProcessUpdatingDebuffsEvent);
        }

        public void Update()
        {
            if (Aisling is not { LoggedIn: true }) return;
            EquipLantern();
            CheckDayDreaming();
            HandleBadTrades();
            DeathStatusCheck();
            UpdateStatusBarAndThreat();
            UpdateSkillSpellCooldown();
            ShowAggro();
        }

        #region Events

        private void ProcessExperienceEvents()
        {
            while (ServerSetup.Instance.Running)
            {
                ExperienceEvent? expEvent = null;

                lock (_expQueueLock)
                {
                    if (_expQueue.Count > 0)
                    {
                        expEvent = _expQueue.Dequeue();
                    }
                }

                if (expEvent.HasValue)
                {
                    HandleExp(expEvent.Value.Player, expEvent.Value.Exp, expEvent.Value.Hunting, expEvent.Value.Overflow);
                }
                else
                {
                    Task.Delay(10).Wait(); // Delay to avoid busy-waiting
                }
            }
        }

        private void ProcessAbilityEvents()
        {
            while (ServerSetup.Instance.Running)
            {
                AbilityEvent? apEvent = null;

                lock (_apQueueLock)
                {
                    if (_apQueue.Count > 0)
                    {
                        apEvent = _apQueue.Dequeue();
                    }
                }

                if (apEvent.HasValue)
                {
                    HandleAp(apEvent.Value.Player, apEvent.Value.Exp, apEvent.Value.Hunting, apEvent.Value.Overflow);
                }
                else
                {
                    Task.Delay(10).Wait(); // Delay to avoid busy-waiting
                }
            }
        }

        private void ProcessApplyingDebuffsEvent()
        {
            while (ServerSetup.Instance.Running)
            {
                DebuffEvent? debuffEvent = null;

                lock (_debuffQueueLockApply)
                {
                    if (_debuffApplyQueue.Count > 0)
                    {
                        debuffEvent = _debuffApplyQueue.Dequeue();
                    }
                }

                if (debuffEvent.HasValue)
                {
                    debuffEvent.Value.Debuff.OnApplied(debuffEvent.Value.Affected, debuffEvent.Value.Debuff);
                }
                else
                {
                    Task.Delay(10).Wait(); // Delay to avoid busy-waiting
                }
            }
        }

        private void ProcessApplyingBuffsEvent()
        {
            while (ServerSetup.Instance.Running)
            {
                BuffEvent? buffEvent = null;

                lock (_buffQueueLockApply)
                {
                    if (_buffApplyQueue.Count > 0)
                    {
                        buffEvent = _buffApplyQueue.Dequeue();
                    }
                }

                if (buffEvent.HasValue)
                {
                    buffEvent.Value.Buff.OnApplied(buffEvent.Value.Affected, buffEvent.Value.Buff);
                }
                else
                {
                    Task.Delay(10).Wait(); // Delay to avoid busy-waiting
                }
            }
        }

        private void ProcessUpdatingDebuffsEvent()
        {
            while (ServerSetup.Instance.Running)
            {
                DebuffEvent? debuffEvent = null;

                lock (_debuffQueueLockUpdate)
                {
                    if (_debuffUpdateQueue.Count > 0)
                    {
                        debuffEvent = _debuffUpdateQueue.Dequeue();
                    }
                }

                if (debuffEvent.HasValue)
                {
                    debuffEvent.Value.Debuff.Update(debuffEvent.Value.Affected, debuffEvent.Value.TimeLeft);
                }
                else
                {
                    Task.Delay(10).Wait(); // Delay to avoid busy-waiting
                }
            }
        }

        private void ProcessUpdatingBuffsEvent()
        {
            while (ServerSetup.Instance.Running)
            {
                BuffEvent? buffEvent = null;

                lock (_buffQueueLockUpdate)
                {
                    if (_buffUpdateQueue.Count > 0)
                    {
                        buffEvent = _buffUpdateQueue.Dequeue();
                    }
                }

                if (buffEvent.HasValue)
                {
                    buffEvent.Value.Buff.Update(buffEvent.Value.Affected, buffEvent.Value.TimeLeft);
                }
                else
                {
                    Task.Delay(10).Wait(); // Delay to avoid busy-waiting
                }
            }
        }

        #endregion

        public void EquipLantern()
        {
            if (!_lanternControl.IsRunning)
            {
                _lanternControl.Start();
            }

            if (_lanternControl.Elapsed.TotalMilliseconds < _lanternCheckTimer.Delay.TotalMilliseconds) return;
            _lanternControl.Restart();
            if (Aisling.Map == null) return;
            if (Aisling.Map.Flags.MapFlagIsSet(MapFlags.Darkness))
            {
                if (Aisling.Lantern == 2) return;
                Aisling.Lantern = 2;
                SendDisplayAisling(Aisling);
                return;
            }

            if (Aisling.Lantern != 2) return;
            Aisling.Lantern = 0;
            SendDisplayAisling(Aisling);
        }

        public void CheckDayDreaming()
        {
            // Logic based on player's set ActiveStatus
            switch (Aisling.ActiveStatus)
            {
                case ActivityStatus.Awake:
                case ActivityStatus.DayDreaming:
                case ActivityStatus.NeedGroup:
                case ActivityStatus.Grouped:
                case ActivityStatus.LoneHunter:
                case ActivityStatus.GroupHunter:
                case ActivityStatus.NeedHelp:
                    DaydreamingRoutine();
                    break;
                case ActivityStatus.DoNotDisturb:
                    break;
            }
        }

        public void HandleBadTrades()
        {
            if (Aisling.Exchange?.Trader == null) return;
            if (Aisling.Exchange.Trader.LoggedIn && Aisling.WithinRangeOf(Aisling.Exchange.Trader)) return;
            Aisling.CancelExchange();
        }

        public void DeathStatusCheck()
        {
            var proceed = false;

            if (Aisling.CurrentHp <= 0)
            {
                Aisling.CurrentHp = 1;
                proceed = true;
            }

            if (!proceed) return;
            SendAttributes(StatUpdateType.Vitality);

            if (Aisling.Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
            {
                for (var i = 0; i < 2; i++)
                    Aisling.RemoveBuffsAndDebuffs();

                // ToDo: Create a different method for player kills
                Aisling.CastDeath();

                var target = Aisling.Target;

                if (target != null)
                {
                    if (target is Aisling aisling)
                        aisling.SendTargetedClientMethod(Scope.All, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{Aisling.Username} has been killed by {aisling.Username}."));
                }
                else
                {
                    Aisling.SendTargetedClientMethod(Scope.All, c => c.SendServerMessage(ServerMessageType.ActiveMessage, $"{Aisling.Username} has died."));
                }

                return;
            }

            if (Aisling.CurrentMapId == ServerSetup.Instance.Config.DeathMap) return;
            if (Aisling.Skulled) return;

            var debuff = new DebuffReaping();
            EnqueueDebuffAppliedEvent(Aisling, debuff, TimeSpan.FromSeconds(debuff.Length));
        }

        public void UpdateStatusBarAndThreat()
        {
            if (!_statusControl.IsRunning)
            {
                _statusControl.Start();
            }

            if (_statusControl.Elapsed.TotalMilliseconds < Aisling.BuffAndDebuffTimer.Delay.TotalMilliseconds) return;

            Aisling.UpdateBuffs(_statusControl.Elapsed);
            Aisling.UpdateDebuffs(_statusControl.Elapsed);
            Aisling.ThreatGeneratedSubsided(Aisling);
            _statusControl.Restart();
        }

        public void UpdateSkillSpellCooldown()
        {
            if (!_skillControl.IsRunning)
            {
                _skillControl.Start();
            }

            // Checks in real-time if a player is overburdened
            if (Aisling.Overburden)
            {
                SkillSpellTimer.Delay = TimeSpan.FromMicroseconds(2000);
                // If overburdened, set the trigger to remove it when not
                Aisling.OverburdenDelayed = true;
            }
            else
            {
                // When not overburdened, check if the player was, and return the delay to normal
                if (Aisling.OverburdenDelayed)
                {
                    SkillSpellTimer.Delay = TimeSpan.FromMicroseconds(1000);
                    Aisling.OverburdenDelayed = false;
                }
            }

            if (_skillControl.Elapsed.TotalMilliseconds < SkillSpellTimer.Delay.TotalMilliseconds) return;

            foreach (var skill in Aisling.SkillBook.Skills.Values)
            {
                if (skill == null) continue;
                skill.CurrentCooldown--;
                if (skill.CurrentCooldown < 0)
                    skill.CurrentCooldown = 0;
            }

            foreach (var spell in Aisling.SpellBook.Spells.Values)
            {
                if (spell == null) continue;
                spell.CurrentCooldown--;
                if (spell.CurrentCooldown < 0)
                    spell.CurrentCooldown = 0;
            }

            _skillControl.Restart();
        }

        private void ShowAggro()
        {
            if (!_aggroMessageControl.IsRunning)
            {
                _aggroMessageControl.Start();
            }

            if (_aggroMessageControl.Elapsed.TotalMilliseconds < _aggroTimer.Delay.TotalMilliseconds) return;
            _aggroMessageControl.Restart();
            Aisling.ThreatTimer = Aisling.Camouflage ? new WorldServerTimer(TimeSpan.FromSeconds(30)) : new WorldServerTimer(TimeSpan.FromSeconds(60));
            var color = "a";
            var aggro = (long)(Aisling.ThreatMeter >= 1 ? 100 : 0);
            var group = Aisling.GroupParty?.PartyMembers;

            if (group?.Count > 0)
            {
                var target = group.MaxBy(dmg => dmg.ThreatMeter);
                if (!(target.ThreatMeter > 0 & Aisling.ThreatMeter > 0)) return;
                var percent = ((double)Aisling.ThreatMeter / target.ThreatMeter) * 100;
                aggro = (long)Math.Clamp(percent, 0, 100);
            }
            else
            {
                Aisling.Client.SendServerMessage(ServerMessageType.PersistentMessage, "");
                return;
            }

            foreach (var key in AggroColors.Keys.Reverse())
            {
                if (aggro < key) continue;
                color = AggroColors[key];
                break;
            }

            Aisling.Client.SendServerMessage(ServerMessageType.PersistentMessage, Aisling.ThreatMeter == 0 ? "" : $"{{=gThreat: {{={color}{aggro}%");
        }

        #region Player Load

        public async Task<WorldClient> Load()
        {
            if (Aisling == null || Aisling.AreaId == 0) return null;
            if (!ServerSetup.Instance.GlobalMapCache.ContainsKey(Aisling.AreaId)) return null;
            Aisling.Client = this;

            await LoadLock.WaitAsync().ConfigureAwait(false);

            try
            {
                SetAislingStartupVariables();
                SendUserId();
                SendProfileRequest();
                InitQuests();
                LoadEquipment().LoadInventory().LoadBank().InitSpellBar().InitDiscoveredMaps().InitIgnoreList().InitLegend();
                SendDisplayAisling(Aisling);
                Enter();
            }
            catch (Exception ex)
            {
                ServerSetup.Logger($"Unhandled Exception in {nameof(Load)}.");
                ServerSetup.Logger(ex.Message, LogLevel.Error);
                ServerSetup.Logger(ex.StackTrace, LogLevel.Error);
                Crashes.TrackError(ex);
            }
            finally
            {
                LoadLock.Release();
            }

            SendHeartBeat(0x14, 0x20);
            return null;
        }

        public void SetAislingStartupVariables()
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
            Aisling.MonsterKillCounters = new ConcurrentDictionary<string, KillRecord>();
            Aisling.Loading = true;

            // ToDo: Fix BuildSettings
            //BuildSettings();
        }

        public WorldClient LoadEquipment()
        {
            try
            {
                var itemList = ServerSetup.Instance.GlobalSqlItemCache.Values.Where(i => i.Owner == Aisling.Serial && i.ItemPane == Item.ItemPanes.Equip);
                var aislingEquipped = Aisling.EquipmentManager.Equipment;

                foreach (var item in itemList.Where(s => s.Template is { Name: not null }))
                {
                    if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Template.Name)) continue;
                    var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                    var newGear = new Item
                    {
                        ItemId = item.ItemId,
                        Template = item.Template,
                        Owner = item.Owner,
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
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }

            LoadSkillBook();
            LoadSpellBook();
            EquipGearAndAttachScripts();
            return this;
        }

        public WorldClient LoadInventory()
        {
            try
            {
                var itemList = ServerSetup.Instance.GlobalSqlItemCache.Values.Where(i => i.Owner == Aisling.Serial && i.ItemPane == Item.ItemPanes.Inventory);

                foreach (var item in itemList)
                {
                    if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Template.Name)) continue;
                    if (item.InventorySlot is <= 0 or >= 60)
                        item.InventorySlot = Aisling.Inventory.FindEmpty();

                    if (Aisling.Inventory.Items[item.InventorySlot] != null)
                    {
                        var itemCheckCount = 0;
                        var routineCheck = 0;

                        for (byte i = 1; i < 60; i++)
                        {
                            itemCheckCount++;
                            item.InventorySlot = i;

                            if (itemCheckCount == 59)
                            {
                                routineCheck++;
                                itemCheckCount = 0;
                            }

                            if (routineCheck != 4) continue;
                            ServerSetup.Logger($"{Aisling.Username} has somehow exceeded their inventory, and have hanging items.");
                            Disconnect();
                            break;
                        }
                    }

                    var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                    var newItem = new Item
                    {
                        ItemId = item.ItemId,
                        Template = item.Template,
                        Owner = item.Owner,
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
                        Image = item.Template.Image,
                        DisplayImage = item.Template.DisplayImage
                    };

                    ItemQualityVariance.SetMaxItemDurability(newItem, newItem.ItemQuality);
                    newItem.GetDisplayName();
                    newItem.NoColorGetDisplayName();

                    Aisling.Inventory.Items[newItem.InventorySlot] = newItem;
                }
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }

            var itemsAvailable = Aisling.Inventory.Items.Values;

            foreach (var item in itemsAvailable)
            {
                if (item == null) continue;
                if (string.IsNullOrEmpty(item.Template.Name)) continue;

                Aisling.CurrentWeight += item.Template.CarryWeight;
                Aisling.Inventory.Items.TryUpdate(item.InventorySlot, item, null);
                Aisling.Inventory.UpdateSlot(Aisling.Client, item);
                item.Scripts = ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);

                if (!string.IsNullOrEmpty(item.Template.WeaponScript))
                    item.WeaponScripts = ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);
            }

            return this;
        }

        public WorldClient LoadBank()
        {
            Aisling.BankManager = new Bank();

            try
            {
                var itemList = ServerSetup.Instance.GlobalSqlItemCache.Values.Where(i => i.Owner == Aisling.Serial && i.ItemPane == Item.ItemPanes.Bank);

                foreach (var item in itemList)
                {
                    if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Template.Name)) continue;

                    var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                    var newItem = new Item
                    {
                        ItemId = item.ItemId,
                        Template = item.Template,
                        Owner = item.Owner,
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
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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

                    var newSpell = new Spell()
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
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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
                    // Apply Buff on login
                    EnqueueBuffAppliedEvent(Aisling, buff, TimeSpan.FromSeconds(buff.Length));
                    // Set Timer & Time left
                    buff.TimeLeft = buffDb.TimeLeft;
                    buff.Timer = new WorldServerTimer(TimeSpan.FromSeconds(1))
                    {
                        Tick = buff.Length - buff.TimeLeft
                    };
                }

                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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
                    // Apply Debuff on login
                    EnqueueDebuffAppliedEvent(Aisling, debuff, TimeSpan.FromSeconds(debuff.Length));
                    // Set Timer & Time left
                    debuff.TimeLeft = deBuffDb.TimeLeft;
                    debuff.Timer = new WorldServerTimer(TimeSpan.FromSeconds(1))
                    {
                        Tick = debuff.Length - debuff.TimeLeft
                    };
                }

                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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
                    var temp = new DiscoveredMap()
                    {
                        Serial = map.Serial,
                        MapId = map.MapId
                    };

                    Aisling.DiscoveredMaps.Add(temp.MapId);
                }

                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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

                    var temp = new IgnoredRecord()
                    {
                        Serial = ignored.Serial,
                        PlayerIgnored = ignored.PlayerIgnored
                    };

                    Aisling.IgnoredList.Add(temp.PlayerIgnored);
                }

                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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
                    var newLegend = new Legend.LegendItem()
                    {
                        LegendId = legend.LegendId,
                        Category = legend.Category,
                        Time = legend.Time,
                        Color = legend.Color,
                        Icon = legend.Icon,
                        Value = legend.Value
                    };

                    Aisling.LegendBook.LegendMarks.Add(newLegend);
                }

                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }

            Aisling.Loading = false;
            return this;
        }

        private WorldClient InitQuests()
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
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }

            return this;
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
                    Aisling.UsedSkill(skill);
                }

                Skill.AttachScript(skill);
                {
                    Aisling.SkillBook.Set(skill.Slot, skill, null);
                }
            }

            if (hasAssail) return;

            Skill.GiveTo(Aisling, "Assail", 1);
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
                equipment.Item.Scripts = ScriptManager.Load<ItemScript>(equipment.Item.Template.ScriptName, equipment.Item);

                if (!string.IsNullOrEmpty(equipment.Item.Template.WeaponScript))
                    equipment.Item.WeaponScripts = ScriptManager.Load<WeaponScript>(equipment.Item.Template.WeaponScript, equipment.Item);

                var script = equipment.Item.Scripts.Values.FirstOrDefault();
                script?.Equipped(Aisling, equipment.Item.Slot);

            }

            var item = new Item();
            item.ReapplyItemModifiers(this);
        }

        #endregion

        #region Handlers

        protected override ValueTask HandlePacketAsync(Span<byte> span)
        {
            var opCode = span[3];
            var isEncrypted = Crypto.ShouldBeEncrypted(opCode);
            var packet = new ClientPacket(ref span, isEncrypted);

            if (isEncrypted)
                Crypto.Decrypt(ref packet);

            return Server.HandlePacketAsync(this, in packet);
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

            var args = new AttributesArgs
            {
                Ability = abCap,
                Ac = (sbyte)Math.Clamp(Aisling.SealedAc, sbyte.MinValue, sbyte.MaxValue),
                Blind = Aisling.IsBlind,
                Con = (byte)Math.Clamp(Aisling.Con, byte.MinValue, byte.MaxValue),
                CurrentHp = (uint)Aisling.CurrentHp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.CurrentHp,
                CurrentMp = (uint)Aisling.CurrentMp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.CurrentMp,
                CurrentWeight = (short)Aisling.CurrentWeight,
                DefenseElement = (Element)Aisling.DefenseElement,
                Dex = (byte)Math.Clamp(Aisling.Dex, 0, 255),
                Dmg = (byte)Math.Clamp((sbyte)Aisling.Dmg, sbyte.MinValue, sbyte.MaxValue),
                GamePoints = (uint)Aisling.GamePoints,
                Gold = (uint)Aisling.GoldPoints,
                Hit = (byte)Math.Clamp((sbyte)Aisling.Hit, sbyte.MinValue, sbyte.MaxValue),
                Int = (byte)Math.Clamp(Aisling.Int, 0, 255),
                IsAdmin = Aisling.GameMaster,
                CanSwim = true,
                Level = levelCap,
                MagicResistance = (byte)(Aisling.Regen / 10),
                HasUnreadMail = Aisling.MailFlags == (Mail.Letter),
                MaximumHp = (uint)Aisling.MaximumHp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.MaximumHp,
                MaximumMp = (uint)Aisling.MaximumMp is >= uint.MaxValue or <= 0 ? 1 : (uint)Aisling.MaximumMp,
                MaxWeight = (short)Aisling.MaximumWeight,
                OffenseElement = (Element)Aisling.OffenseElement,
                StatUpdateType = statUpdateType,
                Str = (byte)Math.Clamp(Aisling.Str, byte.MinValue, byte.MaxValue),
                ToNextAbility = (uint)Aisling.AbpNext,
                ToNextLevel = (uint)Aisling.ExpNext,
                TotalAbility = Aisling.AbpTotal,
                TotalExp = Aisling.ExpTotal,
                UnspentPoints = (byte)Aisling.StatPoints,
                Wis = (byte)Math.Clamp(Aisling.Wis, byte.MinValue, byte.MaxValue)
            };

            Send(args);
        }

        /// <summary>
        /// 0x31 - Show Board
        /// </summary>
        public void SendBoard(string boardName, short? startPost, int index = 0)
        {
            if (ServerSetup.Instance.GlobalBoardCache.TryGetValue(boardName, out var boardIndex))
            {
                ICollection<PostInfo> postsCollection = new List<PostInfo>();
                var board = boardIndex[index];

                foreach (var postFormat in board.Posts)
                {
                    var post = new PostInfo
                    {
                        Author = postFormat.Sender,
                        CreationDate = postFormat.DatePosted,
                        IsHighlighted = postFormat.HighLighted,
                        Message = postFormat.Message,
                        PostId = (short)postFormat.PostId,
                        Subject = postFormat.Subject
                    };

                    postsCollection.Add(post);
                }

                var boardInfo = new BoardInfo
                {
                    BoardId = board.Index,
                    Name = boardName,
                    Posts = postsCollection
                };

                var args = new BoardArgs
                {
                    Type = board.IsMail ? BoardOrResponseType.MailBoard : BoardOrResponseType.PublicBoard,
                    Board = boardInfo,
                    StartPostId = startPost
                };

                Send(args);
            }
        }

        /// <summary>
        /// Sends a list of boards
        /// </summary>
        /// <param name="boards"></param>
        public void SendBoardList(IEnumerable<Board> boards)
        {
            var boardInfoList = new List<BoardInfo>();

            foreach (var board in boards)
            {
                ICollection<PostInfo> postsCollection = new List<PostInfo>();

                foreach (var postFormat in board.Posts)
                {
                    var post = new PostInfo
                    {
                        Author = postFormat.Sender,
                        CreationDate = postFormat.DatePosted,
                        IsHighlighted = postFormat.HighLighted,
                        Message = postFormat.Message,
                        PostId = (short)postFormat.PostId,
                        Subject = postFormat.Subject
                    };

                    postsCollection.Add(post);
                }

                var boardInfo = new BoardInfo
                {
                    BoardId = board.Index,
                    Name = board.Subject,
                    Posts = postsCollection
                };

                boardInfoList.Add(boardInfo);
            }

            var args = new BoardArgs
            {
                Boards = boardInfoList,
                Type = BoardOrResponseType.BoardList
            };

            Send(args);
        }

        /// <summary>
        /// Used to display an embedded board - Example: "Personal" Board array (Mail, News, etc)
        /// </summary>
        public void SendEmbeddedBoard(int index, short? startPost)
        {
            try
            {
                ICollection<PostInfo> postsCollection = new List<PostInfo>();
                var personalBoard = ObjectHandlers.PersonalBoardJsonConvert<Board>(ServerSetup.PersonalBoards[index]);

                foreach (var postFormat in personalBoard.Posts)
                {
                    var post = new PostInfo
                    {
                        Author = postFormat.Sender,
                        CreationDate = postFormat.DatePosted,
                        IsHighlighted = postFormat.HighLighted,
                        Message = postFormat.Message,
                        PostId = (short)postFormat.PostId,
                        Subject = postFormat.Subject
                    };

                    postsCollection.Add(post);
                }

                var boardInfo = new BoardInfo
                {
                    BoardId = personalBoard.Index,
                    Name = personalBoard.Subject,
                    Posts = postsCollection
                };

                var args = new BoardArgs
                {
                    Type = personalBoard.IsMail ? BoardOrResponseType.MailBoard : BoardOrResponseType.PublicBoard,
                    Board = boardInfo,
                    StartPostId = startPost
                };

                Send(args);
            }
            catch (Exception e)
            {
                ServerSetup.Logger($"{Aisling.Username}: Issue with opening board {index}");
                Crashes.TrackError(e);
                Aisling.Client.CloseDialog();
            }
        }

        public void SendBoardResponse(BoardOrResponseType responseType, string message, bool success)
        {
            var args = new BoardArgs
            {
                Type = responseType,
                ResponseMessage = message,
                Success = success
            };

            Send(args);
        }

        public void SendPost(PostFormat post, bool isMail, bool enablePrevBtn = true)
        {
            var args = new BoardArgs
            {
                Type = isMail ? BoardOrResponseType.MailPost : BoardOrResponseType.PublicPost,
                Post = new PostInfo
                {
                    Author = post.Owner,
                    CreationDate = post.DatePosted,
                    IsHighlighted = post.HighLighted,
                    Message = post.Message,
                    PostId = (short)post.PostId,
                    Subject = post.Subject
                },
                EnablePrevBtn = enablePrevBtn
            };

            Send(args);
        }

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

            Send(args);
        }

        /// <summary>
        /// Attempts to cast a spell from cache, creating a temporary copy of it
        /// </summary>
        /// <param name="spellName">Used for finding the spell in cache</param>
        /// <param name="caster">Sprite that cast the spell</param>
        /// <param name="target">Sprite the spell is cast on</param>
        /// <returns>Spell with an attached script was found and called</returns>
        public bool AttemptCastSpellFromCache(string spellName, Sprite caster, Sprite target = null)
        {
            if (!ServerSetup.Instance.GlobalSpellTemplateCache.TryGetValue(spellName, out var value)) return false;

            var scripts = ScriptManager.Load<SpellScript>(spellName, Spell.Create(1, value));
            if (scripts == null) return false;

            scripts.Values.First().OnUse(caster, target);

            return true;
        }

        /// <summary>
        /// 0x48 - Cancel Casting
        /// </summary>
        public void SendCancelCasting()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.CancelCasting, PacketSerializer.Encoding);
            Send(ref packet);
        }

        /// <summary>
        /// 0x0B - Player Move
        /// </summary>
        public void SendConfirmClientWalk(Position oldPoint, Direction direction)
        {
            var args = new ConfirmClientWalkArgs
            {
                Direction = direction,
                OldPoint = new Point(oldPoint.X, oldPoint.Y)
            };

            Send(args);
        }

        /// <summary>
        /// 0x4C - Reconnect
        /// </summary>
        public void SendConfirmExit()
        {
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
            var saved = Save();
            ExitConfirmed = saved.Result;

            // Cleanup
            Aisling.Remove(true);

            var args = new ConfirmExitArgs
            {
                ExitConfirmed = ExitConfirmed
            };

            Send(args);
        }

        /// <summary>
        /// 0x3F - Cooldown
        /// </summary>
        public void SendCooldown(bool skill, byte slot, int cooldownSeconds)
        {
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

        // ToDo: Create Doors Class, and Implement a Dictionary with the values 
        //public void SendDoors(IEnumerable<Door> doors)
        //{
        //    var args = new DoorArgs
        //    {
        //        Doors = Mapper.MapMany<DoorInfo>(doors).ToList()
        //    };

        //    if (args.Doors.Any())
        //        Send(args);
        //}

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
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.StartExchange,
                OtherUserId = (uint?)fromAisling.Serial,
                OtherUserName = fromAisling.Username
            };

            Send(args);
        }

        /// <summary>
        /// 0x42 - Add Item to Exchange 
        /// </summary>
        public void SendExchangeAddItem(bool rightSide, byte index, Item item)
        {
            var args = new ExchangeArgs
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
            var args = new ExchangeArgs
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
            var args = new ExchangeArgs
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
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.Accept,
                PersistExchange = persistExchange
            };

            Send(args);
        }

        /// <summary>
        /// 0x42 - Cancel Exchange 
        /// </summary>
        public void SendExchangeCancel(bool rightSide)
        {
            var args = new ExchangeArgs
            {
                ExchangeResponseType = ExchangeResponseType.Cancel,
                RightSide = rightSide
            };

            Send(args);
        }

        /// <summary>
        /// Forced Client Packet
        /// </summary>
        public void SendForcedClientPacket(ref ClientPacket clientPacket)
        {
            var args = new ForceClientPacketArgs
            {
                ClientOpCode = clientPacket.OpCode,
                Data = clientPacket.Buffer.ToArray()
            };

            Send(args);
        }

        /// <summary>
        /// 0x63 - Group Request
        /// </summary>
        public void SendGroupRequest(GroupRequestType groupRequestType, string fromName)
        {
            var args = new GroupRequestArgs
            {
                GroupRequestType = groupRequestType,
                SourceName = fromName
            };

            Send(args);
        }

        /// <summary>
        /// 0x13 - Health Bar
        /// </summary>
        public void SendHealthBar(Sprite creature, byte? sound = null)
        {
            var args = new HealthBarArgs
            {
                SourceId = (uint)creature.Serial,
                HealthPercent = (byte)((double)100 * creature.CurrentHp / creature.MaximumHp),
                Sound = sound
            };

            Send(args);
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
        /// 0x1F - Map Change Complete
        /// </summary>
        public void SendMapChangeComplete()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.MapChangeComplete, PacketSerializer.Encoding, new byte[2]);

            Send(ref packet);
        }

        /// <summary>
        /// 0x67 - Map Change Pending
        /// </summary>
        public void SendMapChangePending()
        {
            var packet = ServerPacketEx.FromData(
                ServerOpCode.MapChangePending,
                PacketSerializer.Encoding,
                3,
                0,
                0,
                0,
                0,
                0);

            Send(ref packet);
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
                    Width = (byte)mapTemplate.Width,
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
                Height = (byte)Aisling.Map.Height,
                MapId = (short)Aisling.Map.ID,
                Name = Aisling.Map.Name,
                Width = (byte)Aisling.Map.Width
            };

            Send(args);
        }

        /// <summary>
        /// 0x58 - Map Load Complete
        /// </summary>
        public void SendMapLoadComplete()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.MapLoadComplete, PacketSerializer.Encoding, 0);

            Send(ref packet);
        }

        /// <summary>
        /// 0x6F - MapData Send
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
                        ArgumentNullException.ThrowIfNull(name);
                        var metaData = metaDataStore.GetMetaFile(name);

                        if (!name.Contains("Class"))
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

                        break;
                    }
                case MetaDataRequestType.AllCheckSums:
                    {
                        args.MetaDataCollection = new List<MetaDataInfo>();
                        var metaFiles = metaDataStore.GetMetaFilesWithoutExtendedClasses();

                        foreach (var metafileInfo in metaFiles.Select(metaFile => new MetaDataInfo
                        {
                            CheckSum = metaFile.Hash,
                            Data = metaFile.DeflatedData,
                            Name = metaFile.Name
                        }))
                        {
                            args.MetaDataCollection.Add(metafileInfo);
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
            var legendMarks = new List<LegendMarkInfo>();

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

            var legends = aisling.LegendBook.LegendMarks.DistinctBy(m => m.Value).ToList();
            var nullLegends = legends.Where(legend => legend.Value == null).ToList();
            var legendCount = aisling.LegendBook.LegendMarks;

            foreach (var removeLegend in nullLegends)
            {
                legends.Remove(removeLegend);
            }

            foreach (var legendItem in legends)
            {
                if (legendItem == null) continue;
                var markCount = legendCount.Count(item => item.Value == legendItem.Value);
                var legendText = "";
                if (!legendItem.Value.IsNullOrEmpty())
                {
                    legendText = $"{legendItem.Value} - {legendItem.Time?.ToShortDateString()} ({markCount})";
                }

                var legend = new LegendMarkInfo
                {
                    Color = (MarkColor)legendItem.Color,
                    Icon = (MarkIcon)legendItem.Icon,
                    Key = legendItem.Category,
                    Text = legendText
                };

                legendMarks.Add(legend);
            }

            legendMarks.AddRange(from legendItem in nullLegends where legendItem != null select new LegendMarkInfo { Color = (MarkColor)legendItem.Color, Icon = (MarkIcon)legendItem.Icon, Key = legendItem.Category, Text = "" });

            var args = new ProfileArgs
            {
                AdvClass = AdvClass.None,
                BaseClass = (BaseClass)ClassStrings.ClassDisplayInt(ClassStrings.ClassValue(aisling.Path)),
                Equipment = equipment,
                GroupOpen = partyOpen,
                GuildName = $"{aisling.Clan} - {aisling.ClanRank}",
                GuildRank = aisling.GameMaster
                    ? "Game Master"
                    : $"Vit: {aisling.BaseHp + aisling.BaseMp * 2}",
                Id = aisling.Serial,
                LegendMarks = legendMarks,
                Name = aisling.Username,
                Nation = Nation.Mileth,
                Portrait = aisling.PictureData,
                ProfileText = aisling.ProfileMessage,
                SocialStatus = (SocialStatus)aisling.ActiveStatus,
                Title = $"Level: {aisling.ExpLevel}  DR: {aisling.AbpLevel}"
            };

            Send(args);
        }

        /// <summary>
        /// 0x49 - Request Portrait
        /// </summary>
        public void SendProfileRequest()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.ProfileRequest, PacketSerializer.Encoding);

            Send(ref packet);
        }

        public override void SendHeartBeat(byte first, byte second)
        {
            var args = new HeartBeatResponseArgs
            {
                First = first,
                Second = second
            };

            Latency.Restart();
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
            var args = new PublicMessageArgs
            {
                SourceId = sourceId,
                PublicMessageType = publicMessageType,
                Message = message
            };

            Send(args);
        }

        /// <summary>
        /// 0x22 - Client Refresh
        /// </summary>
        public void SendRefreshResponse()
        {
            var packet = ServerPacketEx.FromData(ServerOpCode.RefreshResponse, PacketSerializer.Encoding);

            Send(ref packet);
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
        public void SendRemoveObject(uint id)
        {
            var args = new RemoveObjectArgs
            {
                SourceId = id
            };

            Send(args);
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
            var legendMarks = new List<LegendMarkInfo>();

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

            var legends = Aisling.LegendBook.LegendMarks.DistinctBy(m => m.Value).ToList();
            var nullLegends = legends.Where(legend => legend.Value == null).ToList();
            var legendCount = Aisling.LegendBook.LegendMarks;

            foreach (var removeLegend in nullLegends)
            {
                legends.Remove(removeLegend);
            }

            foreach (var legendItem in legends)
            {
                if (legendItem == null) continue;
                var markCount = legendCount.Count(item => item.Value == legendItem.Value);
                var legendText = "";
                if (!legendItem.Value.IsNullOrEmpty())
                {
                    legendText = $"{legendItem.Value} - {legendItem.Time?.ToShortDateString()} ({markCount})";
                }

                var legend = new LegendMarkInfo
                {
                    Color = (MarkColor)legendItem.Color,
                    Icon = (MarkIcon)legendItem.Icon,
                    Key = legendItem.Category,
                    Text = legendText
                };

                legendMarks.Add(legend);
            }

            legendMarks.AddRange(from legendItem in nullLegends where legendItem != null select new LegendMarkInfo { Color = (MarkColor)legendItem.Color, Icon = (MarkIcon)legendItem.Icon, Key = legendItem.Category, Text = "" });

            var args = new SelfProfileArgs
            {
                AdvClass = AdvClass.None,
                BaseClass = (BaseClass)ClassStrings.ClassDisplayInt(ClassStrings.ClassValue(Aisling.Path)),
                Equipment = equipment,
                GroupOpen = partyOpen,
                GroupString = Aisling.GroupParty?.PartyMemberString ?? "",
                GuildName = $"{Aisling.Clan} - {Aisling.ClanRank}",
                GuildRank = Aisling.GameMaster
                    ? "Game Master"
                    : $"Vit: {Aisling.BaseHp + Aisling.BaseMp * 2}",
                IsMaster = Aisling.Stage >= ClassStage.Master,
                LegendMarks = legendMarks,
                Name = Aisling.Username,
                Nation = Nation.Mileth,
                Portrait = Aisling.PictureData,
                ProfileText = Aisling.ProfileMessage,
                SpouseName = null,
                Title = $"Level: {Aisling.ExpLevel}  DR: {Aisling.AbpLevel}"
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
        /// 0x19 - Send Sound
        /// </summary>
        /// <param name="sound">Sound Number</param>
        /// <param name="isMusic">Whether or not the sound is a song</param>
        public void SendSound(byte sound, bool isMusic)
        {
            var args = new SoundArgs
            {
                Sound = sound,
                IsMusic = isMusic
            };

            Send(args);
        }

        /// <summary>
        /// 0x38 - Remove Equipment
        /// </summary>
        /// <param name="equipmentSlot"></param>
        public void SendUnequip(EquipmentSlot equipmentSlot)
        {
            var args = new UnequipArgs
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
                Gender = (Gender)Aisling.Gender,
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
            foreach (var chunk in objects.OrderBy(o => o.AbandonedDate).Chunk(500))
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
                                Id = groundItem.Serial,
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
                                Id = (uint)creature.Serial,
                                Sprite = creature.Template.Image,
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
                                Sprite = npc.Template.Image,
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
                WorldList = worldList
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

                var arg = new WorldListMemberInfo
                {
                    BaseClass = (BaseClass)classList,
                    Color = (WorldListColor)GetUserColor(aisling),
                    IsMaster = aisling.Stage >= ClassStage.Master,
                    Name = aisling.Username,
                    SocialStatus = (SocialStatus)aisling.ActiveStatus,
                    Title = aisling.GameMaster
                        ? "Game Master"
                        : $"Vit: {aisling.BaseHp + aisling.BaseMp * 2}"
                };

                worldList.Add(arg);
            }

            Send(args);
        }

        private ListColor GetUserColor(Player user)
        {
            var color = ListColor.White;
            if (Aisling.ExpLevel > user.ExpLevel)
                if (Aisling.ExpLevel - user.ExpLevel < 15)
                    color = ListColor.Orange;
            if (!string.IsNullOrEmpty(user.Clan) && user.Clan == Aisling.Clan)
                color = ListColor.Clan;
            if (user.GameMaster)
                color = ListColor.Red;
            if (user.Ranger)
                color = ListColor.Green;
            if (user.Knight)
                color = ListColor.Green;
            if (user.ArenaHost)
                color = ListColor.Teal;
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

            var skill = Skill.GiveTo(this, subject.Name);
            if (skill) LoadSkillBook();

            // Recall message set in message variable back to the npc
            this.SendOptionsDialog(source, message);
            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(subject.TargetAnimation, null, Aisling.Serial));

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

            var spell = Spell.GiveTo(this, subject.Name);
            if (spell) LoadSpellBook();

            // Recall message set in message variable back to the npc
            this.SendOptionsDialog(source, message);
            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(subject.TargetAnimation, null, Aisling.Serial));

            // After learning, ensure player's modifiers are set
            var item = new Item();
            item.ReapplyItemModifiers(this);
        }

        public void ClientRefreshed()
        {
            if (!CanRefresh) return;

            SendMapInfo();
            SendLocation();
            SendAttributes(StatUpdateType.Full);
            UpdateDisplay(true);

            var objects = ObjectHandlers.GetObjects(Aisling.Map, s => s.WithinRangeOf(Aisling), ObjectManager.Get.AllButAislings).ToList();

            if (objects.Any())
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

        public void DaydreamingRoutine()
        {
            if (!_dayDreamingControl.IsRunning)
            {
                _dayDreamingControl.Start();
            }

            if (_dayDreamingControl.Elapsed.TotalMilliseconds < _dayDreamingTimer.Delay.TotalMilliseconds) return;
            _dayDreamingControl.Restart();
            if (!(Aisling.Direction is 1 or 2)) return;
            if (!((DateTime.UtcNow - Aisling.AislingTrackers.LastManualAction).TotalMinutes > 2)) return;
            if (!Socket.Connected || !IsDayDreaming) return;

            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(Aisling.Serial, (BodyAnimation)16, 100));
            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(32, Aisling.Position));
            if (Aisling.Resting == Enums.RestPosition.RestPosition1) return;
            Aisling.Resting = Enums.RestPosition.RestPosition1;
            Aisling.Client.UpdateDisplay();
            Aisling.Client.SendDisplayAisling(Aisling);
        }

        public WorldClient SystemMessage(string message)
        {
            SendServerMessage(ServerMessageType.ActiveMessage, message);
            return this;
        }

        public async Task<bool> Save()
        {
            if (Aisling == null) return false;

            var saved = await StorageManager.AislingBucket.Save(Aisling);

            if (!saved) return false;
            LastSave = DateTime.UtcNow;

            return true;
        }

        public WorldClient UpdateDisplay(bool excludeSelf = false)
        {
            if (!excludeSelf)
                SendDisplayAisling(Aisling);

            var nearbyAislings = Aisling.AislingsNearby();

            if (!nearbyAislings.Any()) return this;

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
                            Aisling.Inventory.RemoveFromInventory(this, itemLoop.First());
                        }

                        break;
                    }

                    // Handle stacked item
                    Aisling.Inventory.RemoveRange(Aisling.Client, item, retainer.AmountRequired);
                    break;
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
                if (item.Durability > 1)
                {
                    return true;
                }
            }

            // Durability check
            if (item.Durability <= 0 && item.Template.Flags.FlagIsSet(ItemFlags.Equipable))
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, "I'll need to repair this before I can use it again.");
                return false;
            }

            // Level check
            if (client.Aisling.ExpLevel < item.Template.LevelRequired)
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, "This item is simply too powerful for me.");
                return false;
            }

            // Stage check
            if (client.Aisling.Stage < item.Template.StageRequired)
            {
                client.SendServerMessage(ServerMessageType.OrangeBar1, "I do not have the current expertise for this.");
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
                        client.SendServerMessage(ServerMessageType.OrangeBar1, "This doesn't quite fit me.");
                        return false;
                    }
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

            client.SendServerMessage(ServerMessageType.OrangeBar1, "I can't seem to use this");
            return false;
        }

        public WorldClient Insert(bool update, bool delete)
        {
            var obj = ObjectHandlers.GetObject<Aisling>(null, aisling => aisling.Serial == Aisling.Serial
                                                                         || string.Equals(aisling.Username, Aisling.Username, StringComparison.CurrentCultureIgnoreCase));

            if (obj == null)
            {
                ObjectHandlers.AddObject(Aisling);
            }
            else
            {
                obj.Remove(update, delete);
                ObjectHandlers.AddObject(Aisling);
            }

            return this;
        }

        public void Interrupt()
        {
            WorldServer.CancelIfCasting(this);
            ClientRefreshed();
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

            var levelUpRand = Generator.RandomNumPercentGen();
            if (skill.Uses >= 200)
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

            var levelUpRand = Generator.RandomNumPercentGen();
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

        public WorldClient ApproachGroup(Aisling targetAisling, IReadOnlyList<string> allowedMaps)
        {
            if (targetAisling.GroupParty?.PartyMembers == null) return this;
            foreach (var member in targetAisling.GroupParty.PartyMembers.Where(member => member.Serial != Aisling.Serial).Where(member => allowedMaps.ListContains(member.Map.Name)))
            {
                member.Client.SendAnimation(67);
                member.Client.TransitionToMap(targetAisling.Map, targetAisling.Position);
            }

            return this;
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
            item.GiveTo(aisling);
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

        public void Port(int i, int x = 0, int y = 0)
        {
            TransitionToMap(i, new Position(x, y));
        }

        public void ResetLocation(WorldClient client)
        {
            var reset = 0;

            while (reset == 0)
            {
                client.Aisling.Abyss = true;
                client.Port(ServerSetup.Instance.Config.TransitionZone, ServerSetup.Instance.Config.TransitionPointX, ServerSetup.Instance.Config.TransitionPointY);
                client.Aisling.Abyss = false;
                reset++;
            }
        }

        public void Recover()
        {
            Revive();
        }

        public void RevivePlayer(string u)
        {
            if (u is null) return;
            var user = ObjectHandlers.GetObject<Aisling>(null, i => i.Username.Equals(u, StringComparison.OrdinalIgnoreCase));

            if (user is { LoggedIn: true })
                user.Client.Revive();
        }

        public void GiveScar()
        {
            var item = new Legend.LegendItem
            {
                Category = "Event",
                Time = DateTime.UtcNow,
                Color = LegendColor.Red,
                Icon = (byte)LegendIcon.Warrior,
                Value = "Fragment of spark taken.."
            };

            Aisling.LegendBook.AddLegend(item, this);
        }

        public void RepairEquipment()
        {
            if (Aisling.Inventory.Items != null)
            {
                foreach (var inventory in Aisling.Inventory.Items.Where(i => i.Value != null && i.Value.Template.Flags.FlagIsSet(ItemFlags.Repairable) && i.Value.Durability < i.Value.MaxDurability))
                {
                    var item = inventory.Value;
                    if (item.Template == null) continue;
                    item.ItemQuality = item.OriginalQuality == Item.Quality.Damaged ? Item.Quality.Common : item.OriginalQuality;
                    item.Tarnished = false;
                    ItemQualityVariance.ItemDurability(item, item.ItemQuality);
                    Aisling.Inventory.UpdateSlot(Aisling.Client, item);
                }
            }

            foreach (var (key, value) in Aisling.EquipmentManager.Equipment.Where(equip => equip.Value != null && equip.Value.Item.Template.Flags.FlagIsSet(ItemFlags.Repairable) && equip.Value.Item.Durability < equip.Value.Item.MaxDurability))
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
            Aisling.CurrentHp = (int)(Aisling.MaximumHp * 0.80);
            Aisling.CurrentMp = (int)(Aisling.MaximumMp * 0.80);

            SendAttributes(StatUpdateType.Vitality);
            return Aisling.CurrentHp > 0;
        }

        public bool IsBehind(Sprite sprite)
        {
            var delta = sprite.Direction - Aisling.Direction;
            return Aisling.Position.IsNextTo(sprite.Position) && delta == 0;
        }

        public void KillPlayer(string u)
        {
            if (u is null) return;
            var user = ObjectHandlers.GetObject<Aisling>(null, i => i.Username.Equals(u, StringComparison.OrdinalIgnoreCase));

            if (user != null)
                user.CurrentHp = 0;
        }


        #endregion

        #region Give Base Stats

        public void GiveHp(int v = 1)
        {
            Aisling.BaseHp += v;

            if (Aisling.BaseHp > ServerSetup.Instance.Config.MaxHP)
                Aisling.BaseHp = ServerSetup.Instance.Config.MaxHP;

            SendAttributes(StatUpdateType.Primary);
        }

        public void GiveMp(int v = 1)
        {
            Aisling.BaseMp += v;

            if (Aisling.BaseMp > ServerSetup.Instance.Config.MaxHP)
                Aisling.BaseMp = ServerSetup.Instance.Config.MaxHP;

            SendAttributes(StatUpdateType.Primary);
        }

        public void GiveStr(byte v = 1)
        {
            Aisling._Str += v;
            SendAttributes(StatUpdateType.Primary);
        }

        public void GiveInt(byte v = 1)
        {
            Aisling._Int += v;
            SendAttributes(StatUpdateType.Primary);
        }

        public void GiveWis(byte v = 1)
        {
            Aisling._Wis += v;
            SendAttributes(StatUpdateType.Primary);
        }

        public void GiveCon(byte v = 1)
        {
            Aisling._Con += v;
            SendAttributes(StatUpdateType.Primary);
        }

        public void GiveDex(byte v = 1)
        {
            Aisling._Dex += v;
            SendAttributes(StatUpdateType.Primary);
        }

        public void EnqueueExperienceEvent(Aisling player, int exp, bool hunting, bool overflow)
        {
            lock (_expQueueLock)
            {
                _expQueue.Enqueue(new ExperienceEvent(player, exp, hunting, overflow));
            }
        }

        public void EnqueueAbilityEvent(Aisling player, int exp, bool hunting, bool overflow)
        {
            lock (_apQueueLock)
            {
                _apQueue.Enqueue(new AbilityEvent(player, exp, hunting, overflow));
            }
        }

        public void EnqueueDebuffAppliedEvent(Sprite affected, Debuff debuff, TimeSpan timeLeft)
        {
            lock (_debuffQueueLockApply)
            {
                _debuffApplyQueue.Enqueue(new DebuffEvent(affected, debuff, timeLeft));
            }
        }

        public void EnqueueBuffAppliedEvent(Sprite affected, Buff buff, TimeSpan timeLeft)
        {
            lock (_buffQueueLockApply)
            {
                _buffApplyQueue.Enqueue(new BuffEvent(affected, buff, timeLeft));
            }
        }

        public void EnqueueDebuffUpdatedEvent(Sprite affected, Debuff debuff, TimeSpan timeLeft)
        {
            lock (_debuffQueueLockUpdate)
            {
                _debuffUpdateQueue.Enqueue(new DebuffEvent(affected, debuff, timeLeft));
            }
        }

        public void EnqueueBuffUpdatedEvent(Sprite affected, Buff buff, TimeSpan timeLeft)
        {
            lock (_buffQueueLockUpdate)
            {
                _buffUpdateQueue.Enqueue(new BuffEvent(affected, buff, timeLeft));
            }
        }

        public void GiveExp(int exp, bool overflow = false)
        {
            if (exp <= 0) exp = 1;

            // Enqueue experience event
            EnqueueExperienceEvent(Aisling, exp, false, overflow);
        }

        private void HandleExp(Aisling player, int exp, bool hunting, bool overflow)
        {
            if (exp <= 0) exp = 1;

            if (hunting)
            {
                if (player.GroupParty != null)
                {
                    var groupSize = player.GroupParty.PartyMembers.Count;
                    var adjustment = ServerSetup.Instance.Config.GroupExpBonus;

                    if (groupSize > 7)
                    {
                        adjustment = ServerSetup.Instance.Config.GroupExpBonus = (groupSize - 7) * 0.05;
                        if (adjustment < 0.75)
                        {
                            adjustment = 0.75;
                        }
                    }

                    var bonus = exp * (1 + player.GroupParty.PartyMembers.Count - 1) * adjustment / 100;
                    if (bonus > 0)
                        exp += (int)bonus;
                }
            }

            if (uint.MaxValue - player.ExpTotal < exp)
            {
                if (!overflow)
                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your experience box is full, ascend to carry more");
            }
            else
            {
                if (!overflow)
                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Received {exp:n0} experience points!");
                player.ExpTotal += (uint)exp;
            }

            try
            {
                if (player.ExpLevel >= 500)
                {
                    player.ExpNext = 0;
                    player.Client.SendAttributes(StatUpdateType.ExpGold);
                    return;
                }

                var expNext = player.ExpNext;
                expNext -= exp;

                if (expNext <= 0)
                {
                    var extraExp = Math.Abs(expNext);
                    player.Client.LevelUp(player, extraExp);
                }
                else
                {
                    player.ExpNext = expNext;
                    player.Client.SendAttributes(StatUpdateType.ExpGold);
                }
            }
            catch (Exception e)
            {
                ServerSetup.Logger($"Issue giving {player.Username} experience.");
                Crashes.TrackError(e);
            }
        }

        public void LevelUp(Aisling player, int extraExp)
        {
            // Set next level
            player.ExpLevel++;

            var seed = player.ExpLevel * 0.1 + 0.5;
            {
                if (player.ExpLevel >= ServerSetup.Instance.Config.PlayerLevelCap) return;
            }
            player.ExpNext = (int)(player.ExpLevel * seed * 5000);

            if (player.ExpNext <= 0)
            {
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue leveling up; Error: Mt. Everest");
                return;
            }

            if (extraExp > 0)
                GiveExp(extraExp, true);

            if (player.ExpLevel >= 250)
                player.StatPoints += 1;
            else
                player.StatPoints += ServerSetup.Instance.Config.StatsPerLevel;

            // Set vitality
            player.BaseHp += (int)(ServerSetup.Instance.Config.HpGainFactor * player._Con * 0.65);
            player.BaseMp += (int)(ServerSetup.Instance.Config.MpGainFactor * player._Wis * 0.45);
            player.CurrentHp = player.MaximumHp;
            player.CurrentMp = player.MaximumMp;
            player.Client.SendAttributes(StatUpdateType.Full);

            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{ServerSetup.Instance.Config.LevelUpMessage}, Insight:{player.ExpLevel}");
            player.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(79, null, player.Serial, 64));
        }

        public void GiveAp(int exp, bool overflow = false)
        {
            if (exp <= 0) exp = 1;

            // Enqueue ap event
            EnqueueAbilityEvent(Aisling, exp, false, overflow);
        }

        private void HandleAp(Aisling player, int exp, bool hunting, bool overflow)
        {
            if (exp <= 0) exp = 1;

            if (hunting)
            {
                if (player.GroupParty != null)
                {
                    var groupSize = player.GroupParty.PartyMembers.Count;
                    var adjustment = ServerSetup.Instance.Config.GroupExpBonus;

                    if (groupSize > 7)
                    {
                        adjustment = ServerSetup.Instance.Config.GroupExpBonus = (groupSize - 7) * 0.05;
                        if (adjustment < 0.75)
                        {
                            adjustment = 0.75;
                        }
                    }

                    var bonus = exp * (1 + player.GroupParty.PartyMembers.Count - 1) * adjustment / 100;
                    if (bonus > 0)
                        exp += (int)bonus;
                }
            }

            if (uint.MaxValue - player.AbpTotal < exp)
            {
                if (!overflow)
                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your ability box is full, ascend to carry more");
            }
            else
            {
                if (!overflow)
                    player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Received {exp:n0} ability points!");
                player.AbpTotal += (uint)exp;
            }

            try
            {
                if (player.AbpLevel >= 500)
                {
                    player.AbpNext = 0;
                    player.Client.SendAttributes(StatUpdateType.ExpGold);
                    return;
                }

                var expNext = player.AbpNext;
                expNext -= exp;

                if (expNext <= 0)
                {
                    var extraExp = Math.Abs(expNext);
                    player.Client.DarkRankUp(player, extraExp);
                }
                else
                {
                    player.AbpNext = expNext;
                    player.Client.SendAttributes(StatUpdateType.ExpGold);
                }
            }
            catch (Exception e)
            {
                ServerSetup.Logger($"Issue giving {player.Username} ability points.");
                Crashes.TrackError(e);
            }
        }

        public void DarkRankUp(Aisling player, int extraExp)
        {
            player.AbpLevel++;

            var seed = player.AbpLevel * 0.5 + 0.8;
            {
                if (player.AbpLevel >= ServerSetup.Instance.Config.PlayerLevelCap) return;
            }
            player.AbpNext = (int)(player.AbpLevel * seed * 5000);

            if (player.AbpNext <= 0)
            {
                player.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue leveling up; Error: Mt. Everest");
                return;
            }

            if (extraExp > 0)
                GiveAp(extraExp, true);

            // Set next level
            player.StatPoints += 1;

            // Set vitality
            player.BaseHp += (int)(ServerSetup.Instance.Config.HpGainFactor * player._Con * 1.23);
            player.BaseMp += (int)(ServerSetup.Instance.Config.MpGainFactor * player._Wis * 0.90);
            player.CurrentHp = player.MaximumHp;
            player.CurrentMp = player.MaximumMp;
            player.Client.SendAttributes(StatUpdateType.Full);

            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{ServerSetup.Instance.Config.AbilityUpMessage}, Dark Rank:{player.AbpLevel}");
            player.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(385, null, player.Serial, 75));
        }

        #endregion

        #region Warping & Maps

        public WorldClient RefreshMap(bool updateView = false)
        {
            MapUpdating = true;

            if (Aisling.IsBlind)
                SendAttributes(StatUpdateType.Secondary);

            SendMapChangePending();
            SendMapInfo();
            SendLocation();

            if (Aisling.Map is not { Script.Item1: null }) return this;

            if (string.IsNullOrEmpty(Aisling.Map.ScriptKey)) return this;
            var scriptToType = ScriptManager.Load<AreaScript>(Aisling.Map.ScriptKey, Aisling.Map);
            var scriptFoundGetValue = scriptToType.TryGetValue(Aisling.Map.ScriptKey, out var script);
            if (scriptFoundGetValue)
                Aisling.Map.Script = new Tuple<string, AreaScript>(Aisling.Map.ScriptKey, script);

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

                Enter();
            }
            else
            {
                WarpTo(position, false);
            }

            // ToDo: Logic to only play this if a menu is opened.
            this.CloseDialog();

            return this;
        }

        public WorldClient TransitionToMap(int area, Position position)
        {
            if (!ServerSetup.Instance.GlobalMapCache.ContainsKey(area)) return this;
            var target = ServerSetup.Instance.GlobalMapCache[area];
            if (target == null) return this;

            if (Aisling.LastMapId != target.ID)
            {
                LeaveArea(target.ID, true, true);

                Aisling.LastPosition = new Position(Aisling.Pos);
                Aisling.Pos = new Vector2(position.X, position.Y);
                Aisling.CurrentMapId = target.ID;

                Enter();
            }
            else
            {
                Aisling.LastPosition = new Position(Aisling.Pos);
                Aisling.Pos = new Vector2(position.X, position.Y);
                Aisling.CurrentMapId = target.ID;

                WarpTo(new Position(Aisling.Pos), false);
            }

            return this;
        }

        public void WarpToAdjacentMap(WarpTemplate warps)
        {
            if (warps.WarpType == WarpType.World) return;

            if (!Aisling.GameMaster)
            {
                if (warps.LevelRequired > 0 && Aisling.ExpLevel < warps.LevelRequired)
                {
                    var msgTier = Math.Abs(Aisling.ExpLevel - warps.LevelRequired);

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
                WarpTo(warps.To.Location, false);
            }
        }

        public void WarpTo(Position position, bool overrideRefresh)
        {
            Aisling.Pos = new Vector2(position.X, position.Y);
            if (overrideRefresh) return;
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

            Aisling.View.Clear();
            return this;
        }

        public void CompleteMapTransition()
        {
            var oldMap = new Area();
            var newMap = new Area();

            foreach (var (_, area) in ServerSetup.Instance.GlobalMapCache)
            {
                if (area.ID == Aisling.CurrentMapId)
                {
                    var onMap = Aisling.Map.IsLocationOnMap(Aisling);

                    if (!onMap)
                    {
                        TransitionToMap(136, new Position(5, 7));
                        SendServerMessage(ServerMessageType.OrangeBar1, "Something grabs your hand...");
                    }

                    newMap = area;

                    if (area.ID == 7000 && oldMap != newMap)
                    {
                        SendServerMessage(ServerMessageType.ScrollWindow,
                            "{=bLife{=a, all that you know, love, and cherish. Everything, and the very fabric of their being. \n\nThe aisling spark, creativity, passion. All of that lives within you." +
                            "\n\nThis story begins shortly after Anaman Pact successfully revives {=bChadul{=a. \n\n-{=cYou feel a sense of unease come over you{=a-");
                    }
                }

                if (area.ID == Aisling.LastMapId)
                {
                    oldMap = area;
                }
            }

            if (Aisling.CurrentMapId == Aisling.LastMapId) return;
            Aisling.LastMapId = Aisling.CurrentMapId;

            if (Aisling.DiscoveredMaps.All(i => i != Aisling.CurrentMapId))
                AddDiscoveredMapToDb();

            var objects = ObjectHandlers.GetObjects(Aisling.Map, s => s.WithinRangeOf(Aisling), ObjectManager.Get.AllButAislings).ToList();

            if (objects.Any())
            {
                objects.Reverse();
                SendVisibleEntities(objects);
            }

            SendMapChangeComplete();

            if (oldMap.Music != newMap.Music)
            {
                SendSound((byte)Aisling.Map.Music, true);
            }

            SendMapLoadComplete();
            SendDisplayAisling(Aisling);
            MapUpdating = false;
        }

        #endregion

        #region SQL

        public void DeleteSkillFromDb(Skill skill)
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            if (skill.SkillName is null) return;

            try
            {
                sConn.Open();
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSkillBook WHERE SkillName = @SkillName AND Serial = @AislingSerial";
                sConn.Execute(cmd, new
                {
                    skill.SkillName,
                    AislingSerial = (long)Aisling.Serial
                });
                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
        }

        public void DeleteSpellFromDb(Spell spell)
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            if (spell.SpellName is null) return;

            try
            {
                sConn.Open();
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSpellBook WHERE SpellName = @SpellName AND Serial = @AislingSerial";
                sConn.Execute(cmd, new
                {
                    spell.SpellName,
                    AislingSerial = (long)Aisling.Serial
                });
                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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
                    Crashes.TrackError(e);
                    return;
                }

                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
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
                    Crashes.TrackError(e);
                    return;
                }

                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
        }

        public void RemoveFromIgnoreListDb(string ignored)
        {
            try
            {
                Aisling.IgnoredList.Remove(ignored);

                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                const string playerIgnored = "DELETE FROM ZolianPlayers.dbo.PlayersIgnoreList WHERE Serial = @AislingSerial AND PlayerIgnored = @ignored";
                sConn.Execute(playerIgnored, new
                {
                    AislingSerial = (long)Aisling.Serial,
                    ignored
                });
                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
            catch (Exception e)
            {
                ServerSetup.Logger(e.Message, LogLevel.Error);
                ServerSetup.Logger(e.StackTrace, LogLevel.Error);
                Crashes.TrackError(e);
            }
        }

        #endregion
    }

    public class NullsLastComparer<T> : IComparer<T?> where T : struct
    {
        public int Compare(T? x, T? y)
        {
            return Nullable.Compare(x, y);
        }
    }
}
