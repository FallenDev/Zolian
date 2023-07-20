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
using Darkages.Infrastructure;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Scripting;
using MapFlags = Darkages.Enums.MapFlags;
using Darkages.GameScripts.Formulas;
using System.Collections.Concurrent;
using Chaos.Common.Identity;
using System.Globalization;
using Darkages.Interfaces;
using ServiceStack.Text.Common;

namespace Darkages.Network.Client
{
    public class WorldClient : SocketClientBase, IWorldClient
    {
        public readonly IWorldServer<WorldClient> Server;
        public readonly ObjectManager ObjectHandlers = new();
        public readonly WorldServerTimer SkillSpellTimer = new(TimeSpan.FromSeconds(1));
        private readonly WorldServerTimer _dayDreamingTimer = new(TimeSpan.FromSeconds(5));
        public readonly object SyncClient = new();
        public Aisling Aisling { get; set; }
        public bool MapUpdating { get; set; }
        public bool MapOpen { get; set; }
        private SemaphoreSlim CreateLock { get; } = new(1, 1);
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
                return readyTime - LastClientRefresh > new TimeSpan(0, 0, 0, 0, 500);
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
        public DateTime LastClientRefresh { get; set; }
        public DateTime LastWarp { get; set; }
        public Item LastItemDropped { get; set; }
        public DateTime LastLocationSent { get; set; }
        public DateTime LastMapUpdated { get; set; }
        public DateTime LastMessageSent { get; set; }
        public DateTime LastMovement { get; set; }
        public DateTime LastEquip { get; set; }
        public DateTime LastPing { get; set; }
        public DateTime LastPingResponse { get; set; }
        public DateTime LastSave { get; set; }
        public DateTime LastWhisperMessageSent { get; set; }
        public PendingBuy PendingBuySessions { get; set; }
        public PendingSell PendingItemSessions { get; set; }
        public PendingBanked PendingBankedSession { get; set; }
        public bool ShouldUpdateMap { get; set; }
        public DateTime LastNodeClicked { get; set; }
        public WorldPortal PendingNode { get; set; }
        public Position LastKnownPosition { get; set; }
        public int MapClicks { get; set; }
        public uint EntryCheck { get; set; }

        public WorldClient([NotNull] IWorldServer<WorldClient> server, [NotNull] Socket socket,
            [NotNull] ICrypto crypto, [NotNull] IPacketSerializer packetSerializer,
            [NotNull] ILogger<SocketClientBase> logger) : base(socket, crypto, packetSerializer, logger)
        {
            Server = server;
        }

        public void Update(TimeSpan elapsedTime)
        {
            if (Aisling is not { LoggedIn: true }) return;

            lock (Trap.Traps)
            {
                foreach (var trap in Trap.Traps.Select(i => i.Value))
                {
                    trap?.Update();
                }
            }

            // Logic based on player's set ActiveStatus
            switch (Aisling.ActiveStatus)
            {
                case ActivityStatus.Awake:
                    DaydreamingRoutine(elapsedTime);
                    break;
                case ActivityStatus.DoNotDisturb:
                    break;
                case ActivityStatus.DayDreaming:
                    if (_dayDreamingTimer.Update(elapsedTime) & Aisling.Direction is 1 or 2)
                    {
                        SendBodyAnimation(Aisling.Serial, (BodyAnimation)16, 120);
                        SendAnimation(32, 200, 0, 0, 0, new Position(Aisling.Pos));
                    }
                    break;
                case ActivityStatus.NeedGroup:
                    DaydreamingRoutine(elapsedTime);
                    break;
                case ActivityStatus.Grouped:
                    DaydreamingRoutine(elapsedTime);
                    break;
                case ActivityStatus.LoneHunter:
                    DaydreamingRoutine(elapsedTime);
                    break;
                case ActivityStatus.GroupHunter:
                    DaydreamingRoutine(elapsedTime);
                    break;
                case ActivityStatus.NeedHelp:
                    DaydreamingRoutine(elapsedTime);
                    break;
            }

            // Normal players without ghost walking, check for walls
            if (!Aisling.GameMaster)
            {
                if (Aisling.Map.IsAStarWall(Aisling, Aisling.X, Aisling.Y))
                {
                    if (LastKnownPosition != null)
                    {
                        Aisling.X = LastKnownPosition.X;
                        Aisling.Y = LastKnownPosition.Y;
                    }

                    SendLocation();
                }
                else
                {
                    LastKnownPosition = new Position(Aisling.X, Aisling.Y);
                }
            }

            // ToDo: Enable this once stable
            // Lag disconnector and routine update for client
            //if (!Aisling.GameMaster)
                //VariableLagDisconnector(30);
            DoUpdate(elapsedTime);
        }

        public void DoUpdate(TimeSpan elapsedTime)
        {
            HandleBadTrades();
            DeathStatusCheck();
            UpdateStatusBarAndThreat(elapsedTime);
            UpdateSkillSpellCooldown(elapsedTime);
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
                Enter();
                SendProfileRequest();
                InitQuests();
                LoadEquipment().LoadInventory().InitSpellBar().InitDiscoveredMaps().InitIgnoreList().InitLegend();
                SendAttributes(StatUpdateType.Full);

                if (Aisling.IsDead())
                    AislingToGhostForm();
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
            LastPing = DateTime.UtcNow;

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
            Aisling.LastMapId = short.MaxValue;
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
                const string procedure = "[SelectEquipped]";
                var values = new { Serial = (long)Aisling.Serial };
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var itemList = sConn.Query<Item>(procedure, values, commandType: CommandType.StoredProcedure).ToList();
                var aislingEquipped = Aisling.EquipmentManager.Equipment;

                foreach (var item in itemList.Where(s => s is { Name: not null }))
                {
                    if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Name)) continue;

                    var equip = new Models.EquipmentSlot(item.Slot, item);
                    var itemName = item.Name;
                    var template = ServerSetup.Instance.GlobalItemTemplateCache[itemName];
                    {
                        equip.Item.Template = template;
                    }

                    var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                    var newGear = new Item
                    {
                        ItemId = item.ItemId,
                        Template = item.Template,
                        Owner = item.Serial,
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

            LoadSkillBook();
            LoadSpellBook();
            EquipGearAndAttachScripts();
            return this;
        }

        public WorldClient LoadSkillBook()
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

            return this;
        }

        public WorldClient LoadSpellBook()
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

            return this;
        }

        public WorldClient LoadInventory()
        {
            try
            {
                const string procedure = "[SelectInventory]";
                var values = new { Serial = (long)Aisling.Serial };
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var itemList = sConn.Query<Item>(procedure, values, commandType: CommandType.StoredProcedure).OrderBy(s => s.InventorySlot);

                foreach (var item in itemList)
                {
                    if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item.Name)) continue;
                    if (item.InventorySlot is 0 or 60) continue;

                    var itemName = item.Name;
                    var template = ServerSetup.Instance.GlobalItemTemplateCache[itemName];
                    {
                        item.Template = template;
                    }

                    if (Aisling.Inventory.Items[item.InventorySlot] != null)
                    {
                        var itemCheckCount = 0;

                        for (byte i = 1; i < 60; i++)
                        {
                            itemCheckCount++;

                            if (Aisling.Inventory.Items[i] == null)
                            {
                                item.Slot = i;
                            }

                            if (itemCheckCount == 59) break;
                        }
                    }

                    var color = (byte)ItemColors.ItemColorsToInt(item.Template.Color);

                    var newItem = new Item
                    {
                        ItemId = item.ItemId,
                        Template = item.Template,
                        Owner = item.Serial,
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

            var itemsAvailable = Aisling.Inventory.Items.Values;

            foreach (var item in itemsAvailable)
            {
                if (item == null) continue;
                if (string.IsNullOrEmpty(item.Template.Name)) continue;

                if (item.CanCarry(Aisling))
                {
                    Aisling.CurrentWeight += item.Template.CarryWeight;
                    Aisling.Inventory.Set(item);
                    Aisling.Inventory.UpdateSlot(Aisling.Client, item);
                }

                item.Scripts = ScriptManager.Load<ItemScript>(item.Template.ScriptName, item);

                if (!string.IsNullOrEmpty(item.Template.WeaponScript))
                    item.WeaponScripts = ScriptManager.Load<WeaponScript>(item.Template.WeaponScript, item);
            }

            return this;
        }

        public WorldClient InitSpellBar()
        {
            return InitBuffs()
                .InitDeBuffs();
        }

        public WorldClient InitBuffs()
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
                    buff.Animation = buffFromCache.Animation;
                    buff.BuffSpell = buffFromCache.BuffSpell;
                    buff.Icon = buffFromCache.Icon;
                    buff.Name = buffDb.Name;
                    buff.Cancelled = buffFromCache.Cancelled;
                    buff.Length = buffFromCache.Length;
                    // Apply Buff on login
                    buff.OnApplied(Aisling, buff);
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

        public WorldClient InitDeBuffs()
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
                    debuff.Animation = debuffFromCache.Animation;
                    debuff.DebuffSpell = debuffFromCache.DebuffSpell;
                    debuff.Icon = debuffFromCache.Icon;
                    debuff.Name = deBuffDb.Name;
                    debuff.Cancelled = debuffFromCache.Cancelled;
                    debuff.Length = debuffFromCache.Length;
                    // Apply Debuff on login
                    debuff.OnApplied(Aisling, debuff);
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

        public WorldClient InitDiscoveredMaps()
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

        public WorldClient InitIgnoreList()
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

        public WorldClient InitLegend()
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

        public WorldClient InitQuests()
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

        public void SkillsAndSpellsCleanup()
        {
            SkillCleanup();
            SpellCleanup();
        }

        public void SkillCleanup()
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
                    Aisling.SkillBook.Set(skill);
                }
            }

            if (hasAssail) return;

            Skill.GiveTo(Aisling, "Assail", 1);
        }

        public void SpellCleanup()
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
                    Aisling.SpellBook.Set(spell);
                }
            }
        }

        public void EquipGearAndAttachScripts()
        {
            foreach (var (_, equipment) in Aisling.EquipmentManager.Equipment)
            {
                if (equipment?.Item?.Template == null) continue;

                if (equipment.Item.CanCarry(Aisling))
                {
                    Aisling.CurrentWeight += equipment.Item.Template.CarryWeight;
                    SendEquipment(equipment.Item);
                }

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
        /// 0x0F - Add Inventory
        /// </summary>
        public void SendAddItemToPane([NotNull] Item item)
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
                    Sprite = (ushort)(item.DisplayImage - 32768),
                    Stackable = item.Template.CanStack
                }
            };

            Send(args);
        }

        /// <summary>
        /// 0x2C - Add Skill
        /// </summary>
        public void SendAddSkillToPane([NotNull] Skill skill)
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
        public void SendAddSpellToPane([NotNull] Spell spell)
        {
            var args = new AddSpellToPaneArgs
            {
                Spell = new SpellInfo
                {
                    Name = spell.SpellName,
                    PanelName = spell.Name,
                    Slot = spell.Slot,
                    Sprite = spell.Icon,
                    CastLines = (byte)spell.Lines,
                    Prompt = string.Empty,
                    SpellType = (SpellType)spell.Template.TargetType
                }
            };

            Send(args);
        }

        /// <summary>
        /// 0x29 - Animation
        /// </summary>
        public void SendAnimation(ushort targetEffect, uint? targetSerial = 0, ushort speed = 100, ushort casterEffect = 0, uint? casterSerial = 0, [CanBeNull] Position position = null)
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
            var args = new AttributesArgs
            {
                Ability = (byte)Aisling.AbpLevel,
                Ac = (sbyte)Aisling.Ac,
                Blind = Aisling.IsBlind,
                Con = (byte)Aisling.Con,
                CurrentHp = (uint)Aisling.CurrentHp,
                CurrentMp = (uint)Aisling.CurrentMp,
                CurrentWeight = (short)Aisling.CurrentWeight,
                DefenseElement = (Element)Aisling.DefenseElement,
                Dex = (byte)Aisling.Dex,
                Dmg = Aisling.Dmg,
                GamePoints = Aisling.GamePoints,
                Gold = Aisling.GoldPoints,
                Hit = Aisling.Hit,
                Int = (byte)Aisling.Int,
                IsAdmin = Aisling.GameMaster,
                Level = (byte)Aisling.ExpLevel,
                MagicResistance = Aisling.Mr,
                HasUnreadMail = Aisling.MailFlags == (Mail.Letter),
                MaximumHp = (uint)Aisling.MaximumHp,
                MaximumMp = (uint)Aisling.MaximumMp,
                MaxWeight = (short)Aisling.MaximumWeight,
                OffenseElement = (Element)Aisling.OffenseElement,
                StatUpdateType = statUpdateType,
                Str = (byte)Aisling.Str,
                ToNextAbility = Aisling.AbpNext,
                ToNextLevel = (uint)Aisling.ExpNext,
                TotalAbility = Aisling.AbpTotal,
                TotalExp = Aisling.ExpTotal,
                UnspentPoints = (byte)Aisling.StatPoints,
                Wis = (byte)Aisling.Wis
            };

            Send(args);
        }

        /// <summary>
        /// 0x31 - Show Board
        /// </summary>
        public void SendBoard(string boardName)
        {
            if (ServerSetup.Instance.GlobalBoardCache.TryGetValue(boardName, out var boardList))
            {
                foreach (var board in boardList)
                {
                    var post = new PostInfo();
                    ICollection<PostInfo> postsCollection = new List<PostInfo>();

                    foreach (var postFormat in board.Posts)
                    {
                        post = new PostInfo
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
                        StartPostId = postsCollection.First().PostId
                    };

                    Send(args);
                }
            }
        }

        public void SendBoardList(IEnumerable<Board> boards)
        {
            IEnumerable<Board> boardList;

            foreach (var board in ServerSetup.Instance.GlobalBoardCache.Values)
            {

            }

            //var args = new BoardArgs
            //{
            //    Type = BoardOrResponseType.BoardList,
            //    Boards = ServerSetup.Instance.GlobalBoardCache
            //};

            //Send(args);
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
        /// Displays player's body animation, sends sound, messages, and displays spells animation on target
        /// </summary>
        public void PlayerCastBodyAnimationSoundAndMessage(Spell spell, Sprite target, byte actionSpeed = 30)
        {
            switch (target)
            {
                case null:
                    return;
                case Aisling aislingTarget:
                    aislingTarget.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{aislingTarget.Username} cast {spell.Template.Name} on you.");
                    break;
            }

            SendServerMessage(ServerMessageType.ActiveMessage, $"You've cast {spell.Template.Name}.");
            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(Aisling.Serial, BodyAnimation.HandsUp, actionSpeed, spell.Template.Sound));
            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Serial, 100, spell.Template.Animation, Aisling.Serial));
        }

        /// <summary>
        /// Displays player's body animation, sends sound, messages, and displays spells animation on target position
        /// </summary>
        public void PlayerCastBodyAnimationSoundAndMessageOnPosition(Spell spell, Sprite target, byte actionSpeed = 30)
        {
            switch (target)
            {
                case null:
                    return;
                case Aisling aislingTarget:
                    aislingTarget.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{aislingTarget.Username} cast {spell.Template.Name} on you.");
                    break;
            }

            SendServerMessage(ServerMessageType.ActiveMessage, $"You've cast {spell.Template.Name}.");
            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(Aisling.Serial, BodyAnimation.HandsUp, actionSpeed, spell.Template.Sound));
            Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.TargetAnimation, target.Serial, 100, spell.Template.Animation, Aisling.Serial, SpellCastInfo.Position));
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

        public void SendConfirmExit()
        {
            var args = new ConfirmExitArgs
            {
                ExitConfirmed = true
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
        /// 0x2F - Send Dialog
        /// </summary>
        public void SendDialog(Dialog dialog)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 0x2F - Send Dialog
        /// </summary>
        public void SendDialog(DialogArgs dialog)
        {
            if (dialog == null) return;
            Send(dialog);
        }

        /// <summary>
        /// 0x33 - Display Player
        /// </summary>
        public void SendDisplayAisling(Aisling aisling)
        {
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
                BodyColor = (BodyColor)aisling.BodyColor,
                BodySprite = (BodySprite)aisling.BodySprite,
                BootsColor = (DisplayColor)aisling.BootColor,
                BootsSprite = (byte)aisling.BootsImg,
                Direction = (Direction)aisling.Direction,
                FaceSprite = aisling.FaceSprite,
                Gender = (Gender)aisling.Gender,
                GroupBoxText = null, // Not built out
                HeadColor = (DisplayColor)aisling.HairColor,
                HeadSprite = aisling.HairStyle,
                Id = (uint)aisling.Serial,
                IsDead = aisling.IsDead(),
                IsHidden = false, // Not sure the difference between hidden and transparent (perhaps GM hide?)
                IsTransparent = aisling.IsInvisible,
                LanternSize = (LanternSize)aisling.Lantern,
                Name = aisling.Username,
                NameTagStyle = (NameTagStyle)aisling.NameStyle,
                OvercoatColor = (DisplayColor)aisling.OverCoatColor,
                OvercoatSprite = (ushort)aisling.OverCoatImg,
                RestPosition = (RestPosition)aisling.Resting,
                ShieldSprite = (byte)aisling.ShieldImg,
                Sprite = aisling.MonsterForm,
                WeaponSprite = (ushort)aisling.WeaponImg,
                X = aisling.X,
                Y = aisling.Y
            };

            //we can always see ourselves, and we're never hostile to our self
            if (!Aisling.Equals(aisling))
            {
                if (Aisling.Map.Flags.MapFlagIsSet(Darkages.Enums.MapFlags.PlayerKill))
                    args.NameTagStyle = NameTagStyle.Hostile;
                //if we're not an admin, and the aisling is not visible
                if (!Aisling.GameMaster && aisling.IsInvisible)
                {
                    //remove the name
                    args.Name = string.Empty;

                    //if we cant see the aisling, hide it (it is otherwise transparent)
                    if (!Aisling.CanSeeInvisible)
                        args.IsHidden = true;
                }
            }

            Send(args);
        }

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
        public void SendEquipment(Item item)
        {
            var args = new EquipmentArgs
            {
                Slot = (EquipmentSlot)item.Slot,
                Item = new ItemInfo
                {
                    Color = (DisplayColor)item.Color,
                    Cost = (int?)item.Template.Value,
                    Count = item.Stacks,
                    CurrentDurability = (int)item.Durability,
                    MaxDurability = (int)item.MaxDurability,
                    Name = item.NoColorDisplayName,
                    Slot = item.Slot,
                    Sprite = item.DisplayImage,
                    Stackable = item.Template.CanStack
                }
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
        /// 0x42 - Request Gold Exchange 
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
        public void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore,
            string? name = null)
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
                        args.MetaDataInfo = new MetaDataInfo
                        {
                            Name = metaData.Name,
                            Data = metaData.DeflatedData,
                            CheckSum = metaData.Hash
                        };
                        break;
                    }
                case MetaDataRequestType.AllCheckSums:
                    {
                        args.MetaDataCollection = metaDataStore.GetMetaFiles().Select(i => new MetaDataInfo
                        {
                            Name = i.Name,
                            Data = i.DeflatedData,
                            CheckSum = i.Hash
                        }).ToList();
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(metaDataRequestType), metaDataRequestType,
                        "Unknown enum value");
            }

            Send(args);
        }

        public void SendNotepad(byte identifier, NotepadType type, byte height, byte width, string? message)
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

            if (aisling.EquipmentManager.Weapon != null)
            {
                var equip = aisling.EquipmentManager.Weapon.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Armor != null)
            {
                var equip = aisling.EquipmentManager.Armor.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Shield != null)
            {
                var equip = aisling.EquipmentManager.Shield.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Helmet != null)
            {
                var equip = aisling.EquipmentManager.Helmet.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Earring != null)
            {
                var equip = aisling.EquipmentManager.Earring.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Necklace != null)
            {
                var equip = aisling.EquipmentManager.Necklace.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.LHand != null)
            {
                var equip = aisling.EquipmentManager.LHand.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.RHand != null)
            {
                var equip = aisling.EquipmentManager.RHand.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.LArm != null)
            {
                var equip = aisling.EquipmentManager.LArm.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.RArm != null)
            {
                var equip = aisling.EquipmentManager.RArm.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Waist != null)
            {
                var equip = aisling.EquipmentManager.Waist.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Leg != null)
            {
                var equip = aisling.EquipmentManager.Leg.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.Foot != null)
            {
                var equip = aisling.EquipmentManager.Foot.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.FirstAcc != null)
            {
                var equip = aisling.EquipmentManager.FirstAcc.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.OverCoat != null)
            {
                var equip = aisling.EquipmentManager.OverCoat.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.OverHelm != null)
            {
                var equip = aisling.EquipmentManager.OverHelm.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.SecondAcc != null)
            {
                var equip = aisling.EquipmentManager.SecondAcc.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (aisling.EquipmentManager.ThirdAcc != null)
            {
                var equip = aisling.EquipmentManager.ThirdAcc.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            foreach (var legendItem in aisling.LegendBook.LegendMarks)
            {
                if (legendItem == null) continue;
                var legends = new LegendMarkInfo
                {
                    Color = (MarkColor)legendItem.Color,
                    Icon = (MarkIcon)legendItem.Icon,
                    Key = legendItem.Category,
                    Text = legendItem.Value
                };

                legendMarks.Add(legends);
            }

            var args = new ProfileArgs
            {
                AdvClass = AdvClass.None,
                BaseClass = (BaseClass)Aisling.Path,
                Equipment = equipment,
                GroupOpen = partyOpen,
                GuildName = aisling.Clan,
                GuildRank = aisling.ClanRank,
                Id = (uint)aisling.Serial,
                LegendMarks = legendMarks,
                Name = aisling.Username,
                Nation = Nation.Mileth,
                Portrait = aisling.PictureData,
                ProfileText = aisling.ProfileMessage,
                SocialStatus = (SocialStatus)aisling.ActiveStatus,
                Title = aisling.ClanTitle
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

        /// <summary>
        /// 0x0A - Message
        /// </summary>
        /// <param name="sourceId">Sprite Serial</param>
        /// <param name="publicMessageType">Message Type</param>
        /// <param name="message">Value</param>
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

            if (Aisling.EquipmentManager.Weapon != null)
            {
                var equip = Aisling.EquipmentManager.Weapon.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Armor != null)
            {
                var equip = Aisling.EquipmentManager.Armor.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Shield != null)
            {
                var equip = Aisling.EquipmentManager.Shield.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Helmet != null)
            {
                var equip = Aisling.EquipmentManager.Helmet.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Earring != null)
            {
                var equip = Aisling.EquipmentManager.Earring.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Necklace != null)
            {
                var equip = Aisling.EquipmentManager.Necklace.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.LHand != null)
            {
                var equip = Aisling.EquipmentManager.LHand.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.RHand != null)
            {
                var equip = Aisling.EquipmentManager.RHand.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.LArm != null)
            {
                var equip = Aisling.EquipmentManager.LArm.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.RArm != null)
            {
                var equip = Aisling.EquipmentManager.RArm.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Waist != null)
            {
                var equip = Aisling.EquipmentManager.Waist.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Leg != null)
            {
                var equip = Aisling.EquipmentManager.Leg.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.Foot != null)
            {
                var equip = Aisling.EquipmentManager.Foot.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.FirstAcc != null)
            {
                var equip = Aisling.EquipmentManager.FirstAcc.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.OverCoat != null)
            {
                var equip = Aisling.EquipmentManager.OverCoat.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.OverHelm != null)
            {
                var equip = Aisling.EquipmentManager.OverHelm.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.SecondAcc != null)
            {
                var equip = Aisling.EquipmentManager.SecondAcc.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            if (Aisling.EquipmentManager.ThirdAcc != null)
            {
                var equip = Aisling.EquipmentManager.ThirdAcc.Item;

                var item = new ItemInfo
                {
                    Color = (DisplayColor)equip.Color,
                    Cost = (int?)equip.Template.Value,
                    Count = equip.Stacks,
                    CurrentDurability = (int)equip.Durability,
                    MaxDurability = (int)equip.MaxDurability,
                    Name = equip.NoColorDisplayName,
                    Slot = equip.Slot,
                    Sprite = equip.DisplayImage,
                    Stackable = equip.Template.CanStack
                };

                equipment.Add((EquipmentSlot)item.Slot, item);
            }

            foreach (var legendItem in Aisling.LegendBook.LegendMarks)
            {
                if (legendItem == null) continue;
                var legends = new LegendMarkInfo
                {
                    Color = (MarkColor)legendItem.Color,
                    Icon = (MarkIcon)legendItem.Icon,
                    Key = legendItem.Category,
                    Text = legendItem.Value
                };

                legendMarks.Add(legends);
            }

            var args = new SelfProfileArgs
            {
                AdvClass = AdvClass.None,
                BaseClass = (BaseClass)Aisling.Path,
                Equipment = equipment,
                GroupOpen = partyOpen,
                GroupString = Aisling.PartyMembers?.ToString(),
                GuildName = Aisling.Clan,
                GuildRank = Aisling.ClanRank,
                IsMaster = Aisling.Stage >= ClassStage.Master,
                LegendMarks = legendMarks,
                Name = Aisling.Username,
                Nation = Nation.Mileth,
                Portrait = Aisling.PictureData,
                ProfileText = Aisling.ProfileMessage,
                SocialStatus = (SocialStatus)Aisling.ActiveStatus,
                SpouseName = null,
                Title = Aisling.ClanTitle
            };

            Send(args);
        }


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

            //split this into chunks so as not to crash the client
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
                                Id = (uint)groundItem.Serial,
                                Sprite = groundItem.Image,
                                X = groundItem.X,
                                Y = groundItem.Y,
                                Color = (DisplayColor)groundItem.Template.Color
                            };

                            //non visible item that can be seen
                            //if (groundItem.Visibility is not VisibilityType.Normal &&
                            //    (Aisling.IsAdmin || Aisling.Script.CanSee(groundItem)))
                            //{
                            //    groundItemInfo.Sprite = 11978;
                            //    groundItemInfo.Color = DisplayColor.Black;
                            //}

                            visibleArgs.Add(groundItemInfo);

                            break;
                        case Money money:
                            var moneyInfo = new GroundItemInfo
                            {
                                Id = (uint)money.Serial,
                                Sprite = money.Image,
                                X = money.X,
                                Y = money.Y,
                                Color = DisplayColor.Default
                            };

                            //non visible money that can be seen
                            //if (money.Visibility is not VisibilityType.Normal &&
                            //    (Aisling.IsAdmin || Aisling.Script.CanSee(money)))
                            //    moneyInfo.Sprite = 138;

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

                            //none visible creature that can be seen
                            //if (creature.Visibility is not VisibilityType.Normal &&
                            //    (Aisling.IsAdmin || Aisling.Script.CanSee(creature)))
                            //    creatureInfo.Sprite = 405;

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
                    Class.Assassin => 2,
                    Class.Arcanus => 3,
                    Class.Cleric => 4,
                    Class.Defender => 5,
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
                    Title = aisling.ClanTitle
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
            var portal = ServerSetup.Instance.GlobalWorldMapTemplateCache[Aisling.World];
            var warpsList = new List<WorldMapNodeInfo>();
            MapOpen = true;

            foreach (var warp in portal.Portals.Where(warps => warps?.Destination != null))
            {
                var map = warp.Destination.AreaID.ToString();
                var x = warp.Destination.Location.X;
                var y = warp.Destination.Location.Y;
                var addWarp = new WorldMapNodeInfo
                {
                    Destination = new Location(map, x, y),
                    ScreenPosition = new Point(warp.PointX, warp.PointY),
                    Text = warp.DisplayName,
                    UniqueId = (ushort)EphemeralRandomIdGenerator<uint>.Shared.NextId
                };

                warpsList.Add(addWarp);
            }

            var args = new WorldMapArgs
            {
                FieldIndex = (byte)portal.FieldNumber,
                FieldName = portal.Name,
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

        public WorldClient LearnSkill(Mundane source, SkillTemplate subject, string message)
        {
            var canLearn = false;

            if (subject.Prerequisites != null) canLearn = PayPrerequisites(subject.Prerequisites);
            if (subject.LearningRequirements != null && subject.LearningRequirements.Any()) canLearn = subject.LearningRequirements.TrueForAll(PayPrerequisites);
            if (!canLearn) return this;

            var skill = Skill.GiveTo(this, subject.Name);
            if (skill) LoadSkillBook();
            //SendOptionsDialog(source, message);

            //Aisling.Show(Scope.NearbyAislings,
            //    new ServerFormat29((uint)Aisling.Serial, (uint)source.Serial,
            //        subject.TargetAnimation,
            //        subject.TargetAnimation, 100));

            return this;
        }

        public WorldClient LearnSpell(Mundane source, SpellTemplate subject, string message)
        {
            var canLearn = false;

            if (subject.Prerequisites != null) canLearn = PayPrerequisites(subject.Prerequisites);
            if (subject.LearningRequirements != null && subject.LearningRequirements.Any()) canLearn = subject.LearningRequirements.TrueForAll(PayPrerequisites);
            if (!canLearn) return this;

            var spell = Spell.GiveTo(this, subject.Name);
            if (spell) LoadSpellBook();
            //SendOptionsDialog(source, message);

            //Aisling.Show(Scope.NearbyAislings,
            //    new ServerFormat29((uint)Aisling.Serial, (uint)source.Serial,
            //        subject.TargetAnimation,
            //        subject.TargetAnimation, 100));

            return this;
        }

        public void ClientRefreshed()
        {
            if (!CanRefresh) return;

            SendMapInfo();
            SendLocation();
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

            if (Aisling.Blind == 0x08)
                SendAttributes(StatUpdateType.Secondary);

            Aisling.Client.LastMapUpdated = DateTime.UtcNow;
            Aisling.Client.LastLocationSent = DateTime.UtcNow;
            Aisling.Client.LastClientRefresh = DateTime.UtcNow;
        }

        public void DaydreamingRoutine(TimeSpan elapsedTime)
        {
            var readyTime = DateTime.UtcNow;

            if (Aisling.ActiveStatus == ActivityStatus.DayDreaming) return;
            if (!((readyTime - LastMovement).TotalMinutes > 2)) return;
            if (!(_dayDreamingTimer.Update(elapsedTime) & Aisling.Direction is 1 or 2)) return;
            if (!Socket.Connected || !IsDayDreaming) return;

            SendBodyAnimation(Aisling.Serial, (BodyAnimation)16, 120);
            SendAnimation(32, 200, 0, 0, 0, new Position(Aisling.Pos));
        }

        public void VariableLagDisconnector(int delay)
        {
            var readyTime = DateTime.UtcNow;

            if (!((readyTime - LastPingResponse).TotalSeconds > delay)) return;
            Aisling?.Remove(true);
            Disconnect();
        }

        public WorldClient SystemMessage(string message)
        {
            SendServerMessage(ServerMessageType.ActiveMessage, message);
            return this;
        }

        public async Task<WorldClient> Save()
        {
            if (Aisling == null) return this;

            var saved = await StorageManager.AislingBucket.Save(Aisling);

            if (!saved) return this;
            LastSave = DateTime.UtcNow;

            return this;
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

            var debuff = new debuff_reaping();
            {
                debuff.OnApplied(Aisling, debuff);
            }
        }

        public WorldClient UpdateDisplay(bool excludeSelf = false)
        {
            if (!excludeSelf) SendDisplayAisling(Aisling);

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

        public void UpdateStatusBarAndThreat(TimeSpan elapsedTime)
        {
            lock (SyncClient)
            {
                Aisling.UpdateBuffs(elapsedTime);
                Aisling.UpdateDebuffs(elapsedTime);
                Aisling.ThreatGeneratedSubsided(Aisling, elapsedTime);
            }
        }

        public void UpdateSkillSpellCooldown(TimeSpan elapsedTime)
        {
            if (!SkillSpellTimer.Update(elapsedTime)) return;

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
        }

        public WorldClient PayItemPrerequisites(LearningPredicate prerequisites)
        {
            if (prerequisites.ItemsRequired is not { Count: > 0 }) return this;

            foreach (var retainer in prerequisites.ItemsRequired)
            {
                var item = Aisling.Inventory.Get(i => i.Template.Name == retainer.Item);

                foreach (var i in item)
                {
                    if (!i.Template.Flags.FlagIsSet(ItemFlags.Stackable))
                    {
                        for (var j = 0; j < retainer.AmountRequired; j++)
                        {
                            Aisling.Inventory.RemoveFromInventory(this, i);
                        }
                        break;
                    }

                    Aisling.Inventory.RemoveRange(Aisling.Client, i, retainer.AmountRequired);
                    break;
                }
            }

            return this;
        }

        public bool PayPrerequisites(LearningPredicate prerequisites)
        {
            if (prerequisites == null) return false;

            PayItemPrerequisites(prerequisites);
            {
                if (prerequisites.GoldRequired > 0)
                {
                    Aisling.GoldPoints -= prerequisites.GoldRequired;
                    if (Aisling.GoldPoints <= 0)
                        Aisling.GoldPoints = 0;
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

            // Gender neutral check
            if (item.Template.Gender == Aisling.Gender)
            {
                return true;
            }

            // Gender check
            if (item.Template.Gender == Aisling.Gender)
            {
                return true;
            }

            client.SendServerMessage(ServerMessageType.OrangeBar1, "I can't seem to use this");
            return false;
        }

        //ToDo; Fix Trading -- Handle Bad Trades
        public void HandleBadTrades()
        {
            //if (Aisling.Exchange?.Trader2 == null) return;

            //if (!Aisling.Exchange.Trader2.LoggedIn || !Aisling.WithinRangeOf(Aisling.Exchange.Trader2))
            //    Aisling.Client.SendExchangeCancel(true);
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
            SendLocation();
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
            {
                SendRemoveSkillFromPane(skill.Slot);
            }
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
            {
                SendRemoveSpellFromPane(spell.Slot);
            }
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

            SendAddSkillToPane(skill);
            skill.CurrentCooldown = skill.Template.Cooldown;
            SendCooldown(true, skill.Slot, skill.CurrentCooldown);
            SendServerMessage(ServerMessageType.ActiveMessage,
                skill.Level >= 100
                    ? string.Format(CultureInfo.CurrentUICulture, "{0} has been mastered.", skill.Template.Name)
                    : string.Format(CultureInfo.CurrentUICulture, "{0} improved, Lv:{1}", skill.Template.Name, skill.Level));
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
            var map = client.Aisling.CurrentMapId;
            var x = (int)client.Aisling.Pos.X;
            var y = (int)client.Aisling.Pos.Y;
            var reset = 0;

            while (reset == 0)
            {
                client.Aisling.Abyss = true;
                client.Port(ServerSetup.Instance.Config.TransitionZone, ServerSetup.Instance.Config.TransitionPointX, ServerSetup.Instance.Config.TransitionPointY);
                client.Port(map, x, y);
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
                SendEquipment(item);
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

        public void GiveExp(uint exp)
        {
            if (exp <= 0) exp = 1;

            SendServerMessage(ServerMessageType.ActiveMessage, $"Received {exp:n0} experience points!");
            Aisling.ExpTotal += exp;
            Aisling.ExpNext -= exp;

            if (Aisling.ExpNext >= int.MaxValue) Aisling.ExpNext = 0;

            var seed = Aisling.ExpLevel * 0.1 + 0.5;
            {
                if (Aisling.ExpLevel >= ServerSetup.Instance.Config.PlayerLevelCap)
                    return;
            }

            while (Aisling.ExpNext <= 0 && Aisling.ExpLevel < 500)
            {
                Aisling.ExpNext = (uint)(Aisling.ExpLevel * seed * 5000);

                if (Aisling.ExpLevel == 500)
                    break;

                if (Aisling.ExpTotal <= 0)
                    Aisling.ExpTotal = uint.MaxValue;

                if (Aisling.ExpTotal >= uint.MaxValue)
                    Aisling.ExpTotal = uint.MaxValue;

                if (Aisling.ExpNext <= 0)
                    Aisling.ExpNext = 1;

                if (Aisling.ExpNext >= int.MaxValue)
                    Aisling.ExpNext = int.MaxValue;

                Aisling.Client.LevelUp(Aisling);
            }

            SendAttributes(StatUpdateType.ExpGold);
        }

        public void LevelUp(Player player)
        {
            if (player.ExpLevel >= ServerSetup.Instance.Config.PlayerLevelCap) return;
            player.BaseHp += (int)(ServerSetup.Instance.Config.HpGainFactor * player._Con * 0.65);
            player.BaseMp += (int)(ServerSetup.Instance.Config.MpGainFactor * player._Wis * 0.45);
            player.StatPoints += ServerSetup.Instance.Config.StatsPerLevel;
            player.ExpLevel++;
            player.CurrentHp = player.MaximumHp;
            player.CurrentMp = player.MaximumMp;

            player.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{ServerSetup.Instance.Config.LevelUpMessage}, Insight:{player.ExpLevel}");
            player.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(79, player.Serial, 64));
            player.Client.SendAttributes(StatUpdateType.ExpGold);
        }

        public void GiveAp(uint a)
        {
            if (a <= 0) a = 1;
            SendServerMessage(ServerMessageType.ActiveMessage, $"Received {a:n0} ability points!");
            Aisling.AbpTotal += a;
            Aisling.AbpNext -= a;

            if (Aisling.AbpNext >= int.MaxValue) Aisling.AbpNext = 0;

            var seed = Aisling.AbpLevel * 0.1 + 0.5;
            {
                if (Aisling.AbpLevel >= ServerSetup.Instance.Config.PlayerLevelCap)
                    return;
            }

            while (Aisling.AbpNext <= 0 && Aisling.AbpLevel < 500)
            {
                Aisling.AbpNext = (uint)(Aisling.AbpLevel * seed * 5000);

                if (Aisling.AbpLevel == 500)
                    break;

                if (Aisling.AbpNext <= 0)
                    Aisling.AbpNext = uint.MaxValue;

                if (Aisling.AbpNext >= uint.MaxValue)
                    Aisling.AbpNext = uint.MaxValue;

                if (Aisling.AbpNext <= 0)
                    Aisling.AbpNext = 1;

                if (Aisling.AbpNext >= int.MaxValue)
                    Aisling.AbpNext = int.MaxValue;

                Aisling.AbpLevel++;
            }

            SendAttributes(StatUpdateType.ExpGold);
        }

        #endregion

        #region Warping & Maps

        public WorldClient RefreshMap(bool updateView = false)
        {
            MapUpdating = true;

            if (Aisling.Blind == 0x08)
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
                LeaveArea(true, true);

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
                LeaveArea(true, true);

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

        public void CheckWarpTransitions(WorldClient client, int x, int y)
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

        public WorldClient LeaveArea(bool update = false, bool delete = false)
        {
            if (Aisling.LastMapId == short.MaxValue) Aisling.LastMapId = Aisling.CurrentMapId;

            Aisling.Remove(update, delete);

            if (Aisling.LastMapId != Aisling.CurrentMapId && Aisling.Map.Script.Item2 != null)
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
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSkillBook WHERE SkillName = @SkillName AND Serial = @Serial";
                sConn.Execute(cmd, new { skill.SkillName, Aisling.Serial });
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
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSpellBook WHERE SpellName = @SpellName AND Serial = @Serial";
                sConn.Execute(cmd, new { spell.SpellName, Aisling.Serial });
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

        public void LoadBank()
        {
            Aisling.BankManager = new Bank();

            try
            {
                const string procedure = "[SelectBanked]";
                var values = new { Aisling.Serial };
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var itemList = sConn.Query<Item>(procedure, values, commandType: CommandType.StoredProcedure).OrderBy(s => s.InventorySlot);

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
                        Template = item.Template,
                        ItemId = item.ItemId,
                        Image = item.Template.Image,
                        DisplayImage = item.Template.DisplayImage,
                        Durability = item.Durability,
                        Owner = item.Serial,
                        Identified = item.Identified,
                        Stacks = item.Stacks,
                        ItemQuality = item.ItemQuality,
                        OriginalQuality = item.OriginalQuality,
                        ItemVariance = item.ItemVariance,
                        WeapVariance = item.WeapVariance,
                        Enchantable = item.Template.Enchantable,
                        Color = color
                    };

                    ItemQualityVariance.SetMaxItemDurability(newItem, newItem.ItemQuality);
                    newItem.GetDisplayName();
                    newItem.NoColorGetDisplayName();

                    Aisling.BankManager.Items[newItem.ItemId] = newItem;
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
        }

        public async Task AddDiscoveredMapToDb()
        {
            await CreateLock.WaitAsync().ConfigureAwait(false);

            try
            {
                Aisling.DiscoveredMaps.Add(Aisling.CurrentMapId);

                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var cmd = new SqlCommand("FoundMap", sConn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = Aisling.Serial;
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
            finally
            {
                CreateLock.Release();
            }
        }

        public async Task AddToIgnoreListDb(string ignored)
        {
            await CreateLock.WaitAsync().ConfigureAwait(false);

            try
            {
                Aisling.IgnoredList.Add(ignored);

                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var cmd = new SqlCommand("IgnoredSave", sConn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = Aisling.Serial;
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
            finally
            {
                CreateLock.Release();
            }
        }

        public void RemoveFromIgnoreListDb(string ignored)
        {
            try
            {
                Aisling.IgnoredList.Remove(ignored);

                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                const string playerIgnored = "DELETE FROM ZolianPlayers.dbo.PlayersIgnoreList WHERE Serial = @Serial AND PlayerIgnored = @ignored";
                sConn.Execute(playerIgnored, new
                {
                    Aisling.Serial,
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
}
