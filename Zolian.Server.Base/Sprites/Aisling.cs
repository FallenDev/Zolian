using System.Collections.Concurrent;
using System.Numerics;
using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Sprites;

public class KillRecord
{
    public int TotalKills { get; set; }
    public DateTime TimeKilled { get; set; }
}

public class IgnoredRecord
{
    public int Serial { get; init; }
    public string PlayerIgnored { get; init; }
}

public sealed class Aisling : Player, IAisling
{
    public WorldClient Client { get; set; }
    public int EquipmentDamageTaken = 0;
    public readonly ConcurrentDictionary<uint, Sprite> View = new();
    public ConcurrentDictionary<string, KillRecord> MonsterKillCounters = new();
    public new AislingTrackers Trackers
    {
        get => (AislingTrackers)base.Trackers;
        private set => base.Trackers = value;
    }

    public uint MaximumWeight => GameMaster switch
    {
        true => 999,
        false => (uint)(Math.Round(ExpLevel / 2d) + _Str + ServerSetup.Instance.Config.WeightIncreaseModifer + 200)
    };

    public bool Dead => IsDead();
    public bool RegenTimerDisabled;
    public bool Skulled => HasDebuff("Skulled");
    public Party GroupParty => ServerSetup.Instance.GlobalGroupCache.TryGetValue(GroupId, out var idParty)
        ? idParty
        : null;
    public List<Aisling> PartyMembers => GroupParty?.PartyMembers;
    public int AreaId => CurrentMapId;

    public Aisling()
    {
        OffenseElement = ElementManager.Element.None;
        SecondaryOffensiveElement = ElementManager.Element.None;
        DefenseElement = ElementManager.Element.None;
        SecondaryDefensiveElement = ElementManager.Element.None;
        LegendBook = new Legend();
        ClanTitle = string.Empty;
        ClanRank = string.Empty;
        LoggedIn = false;
        ActiveStatus = ActivityStatus.Awake;
        PartyStatus = GroupStatus.AcceptingRequests;
        Remains = new Death();
        Hacked = false;
        PasswordAttempts = 0;
        ActiveReactor = null;
        DiscoveredMaps = new List<int>();
        IgnoredList = new List<string>();
        GroupId = 0;
        AttackDmgTrack = new WorldServerTimer(TimeSpan.FromSeconds(1));
        ThreatTimer = new WorldServerTimer(TimeSpan.FromSeconds(60));
        ChantTimer = new ChantTimer(1500);
        TileType = TileContent.Aisling;
    }

    public bool Loading { get; set; }
    public long DamageCounter { get; set; }
    public uint ThreatMeter { get; set; }
    public ReactorTemplate ActiveReactor { get; set; }
    public DialogSequence ActiveSequence { get; set; }
    public ExchangeSession Exchange { get; set; }
    public NameDisplayStyle NameStyle { get; set; }
    public bool IsCastingSpell { get; set; }
    public bool ProfileOpen { get; set; }
    public Summon SummonObjects { get; set; }
    public bool UsingTwoHanded { get; set; }
    public int LastMapId { get; set; }
    public WorldServerTimer AttackDmgTrack { get; }
    public WorldServerTimer ThreatTimer { get; set; }
    public ChantTimer ChantTimer { get; }
    public UserOptions GameSettings { get; init; }
    public Mail MailFlags { get; set; }
    public SkillBook SkillBook { get; set; }
    public SpellBook SpellBook { get; set; }
    public string ActionUsed { get; set; }
    public bool ThrewHealingPot { get; set; }
    public Death Remains { get; set; }
    public Legend LegendBook { get; set; }
    public byte[] PictureData { get; set; }
    public Bank BankManager { get; set; }
    public Inventory Inventory { get; set; }
    public EquipmentManager EquipmentManager { get; set; }
    public Quests QuestManager { get; set; }
    public List<int> DiscoveredMaps { get; set; }
    public List<string> IgnoredList { get; set; }
    public ConcurrentDictionary<string, string> ExplorePositions { get; set; }

    public NationTemplate PlayerNation
    {
        get
        {
            if (Nation != null) return ServerSetup.Instance.GlobalNationTemplateCache[Nation];
            throw new InvalidOperationException();
        }
    }

    public bool LeaderPrivileges
    {
        get
        {
            if (!ServerSetup.Instance.GlobalGroupCache.ContainsKey(GroupId))
                return false;

            var group = ServerSetup.Instance.GlobalGroupCache[GroupId];
            return group != null && string.Equals(@group.LeaderName.ToLower(), Username.ToLower(),
                StringComparison.InvariantCulture);
        }
    }

    public bool CantReact => CantAttack || CantCast || CantMove;
    public bool Camouflage => SkillBook.HasSkill("Camouflage");
    public bool PainBane => SkillBook.HasSkill("Pain Bane");

    public byte Blind
    {
        get
        {
            var blind = HasDebuff("Blind");
            return blind ? (byte)0x08 : (byte)0x00;
        }
    }

    public bool Poisoned
    {
        get
        {
            return HasDebuff(i => i.Name.Contains("Puinsein"));
        }
    }

    public bool TwoHandedBasher
    {
        get
        {
            var twoHandedUser = false;

            foreach (var (_, skill) in SkillBook.Skills)
            {
                switch (skill?.SkillName)
                {
                    case "Two-Handed Attack":
                    case "Kobudo":
                        twoHandedUser = true;
                        break;
                }
            }

            return twoHandedUser;
        }
    }

    public bool TwoHandedCaster
    {
        get
        {
            foreach (var (_, skill) in SkillBook.Skills)
            {
                if (skill?.SkillName is "Advanced Staff Training") return true;
            }

            return false;
        }
    }

    public bool DualWield
    {
        get
        {
            var dualWielder = false;

            foreach (var (_, skill) in SkillBook.Skills)
            {
                switch (skill?.SkillName)
                {
                    case "Dual Wield":
                    case "Ambidextrous":
                        dualWielder = true;
                        break;
                }
            }

            return dualWielder;
        }
    }

    public bool UseBows
    {
        get
        {
            var bowWielder = false;

            foreach (var (_, skill) in SkillBook.Skills)
            {
                switch (skill?.SkillName)
                {
                    case "Archery":
                    case "Aim":
                        bowWielder = true;
                        break;
                }
            }

            return bowWielder;
        }
    }

        /// <summary>
    /// Sends a ServerFormat to target players using a scope or definer
    /// </summary>
    /// <param name="op">Scope of the method call</param>
    /// <param name="method">IWorldClient method to send</param>
    /// <param name="definer">Specific users, Scope must also be "DefinedAislings"</param>
    public void SendTargetedClientMethod(Scope op, Action<IWorldClient> method, IEnumerable<Aisling> definer = null)
    {
        switch (op)
        {
            case Scope.Self:
                method(Client);
                return;
            case Scope.NearbyAislingsExludingSelf:
                {
                    foreach (var gc in GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers)))
                    {
                        if (gc == null || gc.Serial == Serial) continue;
                        if (gc.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.NearbyAislings:
                {
                    foreach (var gc in GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers)))
                    {
                        if (gc?.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.Clan:
                {
                    if (this is not Aisling playerTalking) return;

                    foreach (var gc in GetObjects<Aisling>(null, otherPlayers => otherPlayers != null && !string.IsNullOrEmpty(otherPlayers.Clan) && string.Equals(otherPlayers.Clan, playerTalking.Clan, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        if (gc?.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.VeryNearbyAislings:
                {
                    foreach (var gc in GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers, ServerSetup.Instance.Config.VeryNearByProximity)))
                    {
                        if (gc?.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.AislingsOnSameMap:
                {
                    foreach (var gc in GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && CurrentMapId == otherPlayers.CurrentMapId))
                    {
                        if (gc?.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.GroupMembers:
                {
                    if (this is not Aisling playersTalking) return;

                    foreach (var gc in GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && playersTalking.GroupParty.Has(otherPlayers)))
                    {
                        if (gc?.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.NearbyGroupMembersExcludingSelf:
                {
                    if (this is not Aisling playersTalking) return;

                    foreach (var gc in GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && otherPlayers.WithinRangeOf(playersTalking) && playersTalking.GroupParty.Has(otherPlayers)))
                    {
                        if (gc == null || gc.Serial == Serial) continue;
                        if (gc.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.NearbyGroupMembers:
                {
                    if (this is not Aisling playersTalking) return;

                    foreach (var gc in GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && otherPlayers.WithinRangeOf(playersTalking) && playersTalking.GroupParty.Has(otherPlayers)))
                    {
                        if (gc?.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.DefinedAislings when definer == null:
                return;
            case Scope.DefinedAislings:
                {
                    foreach (var gc in definer)
                    {
                        if (gc?.Client == null) continue;
                        method(gc.Client);
                    }

                    return;
                }
            case Scope.All:
                var players = ServerSetup.Instance.Game.Aislings;
                foreach (var p in players)
                {
                    if (p?.Client == null) continue;
                    method(p.Client);
                }

                return;
            default:
                method(Client);
                return;
        }
    }

    public void AStarPath(List<Vector2> pathList)
    {
        if (pathList == null) return;
        if (pathList.Count == 0) return;

        var nodeX = pathList[0].X;
        var nodeY = pathList[0].Y;

        WalkTo((int)nodeX, (int)nodeY);
    }

    public void CancelExchange()
    {
        if (Exchange?.Trader == null)
            return;

        var trader = Exchange.Trader;

        var exchangeA = Exchange;
        var exchangeB = trader.Exchange;

        var itemsA = exchangeA.Items.ToArray();
        var weightCheckA = itemsA.Aggregate(0, (current, item) => current + item.Template.CarryWeight);

        var itemsB = exchangeB.Items.ToArray();
        var weightCheckB = itemsB.Aggregate(0, (current, item) => current + item.Template.CarryWeight);

        var goldA = exchangeA.Gold;
        var goldB = exchangeB.Gold;

        Exchange = null;
        trader.Exchange = null;

        foreach (var item in itemsB)
            if (item.GiveTo(trader))
            {
            }

        foreach (var item in itemsA)
            if (item.GiveTo(this))
            {
            }

        GoldPoints += goldA;
        trader.GoldPoints += goldB;

        if (trader.GoldPoints > (uint)ServerSetup.Instance.Config.MaxCarryGold)
            trader.GoldPoints = (uint)ServerSetup.Instance.Config.MaxCarryGold;
        if (GoldPoints > (uint)ServerSetup.Instance.Config.MaxCarryGold)
            GoldPoints = (uint)ServerSetup.Instance.Config.MaxCarryGold;

        trader.Client.SendAttributes(StatUpdateType.ExpGold);
        Client.SendAttributes(StatUpdateType.ExpGold);
        trader.Client.SendExchangeCancel(true);
        Client.SendExchangeCancel(false);
    }

    public bool CanSeeGhosts()
    {
        return IsDead();
    }

    public void UsedSkill(Skill skill)
    {
        if (skill == null) return;
        if (!skill.Ready && skill.Template.SkillType != SkillScope.Assail)
            Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{skill.Name} not ready yet.");
    }

    public Aisling CastAnimation(Spell spell, Sprite target, byte actionSpeed = 30)
    {
        var bodyAnim = Path switch
        {
            Class.Cleric => (BodyAnimation)128,
            Class.Arcanus => (BodyAnimation)136,
            Class.Monster => (BodyAnimation)1,
            _ => (BodyAnimation)6
        };

        Client.SendBodyAnimation(Serial, bodyAnim, actionSpeed);

        switch (target)
        {
            case null:
                return this;
            case Aisling:
                Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{Username} Attacks you with {spell.Template.Name}.");
                break;
        }

        Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You've cast {spell.Template.Name}.");
        SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(spell.Template.Animation, target.Serial, 100, 0, Serial, new Position(target.Pos.X, target.Pos.Y)));

        return this;
    }

    public void CastDeath()
    {
        if (Client.Aisling.Flags.PlayerFlagIsSet(AislingFlags.Ghost)) return;
        LastMapId = CurrentMapId;
        LastPosition = Position;

        Client.CloseDialog();
        Client.AislingToGhostForm();
    }

    public void CastSpell(Spell spell, CastInfo info)
    {
        if (info != null)
        {
            if (!string.IsNullOrEmpty(info.Data))
                foreach (var script in spell.Scripts.Values)
                    script.Arguments = info.Data;

            var target = GetObject(Map, i => i.Serial == info.Target, Get.Monsters | Get.Aislings);

            if (spell.Scripts != null)
            {
                if (target != null)
                {
                    spell.InUse = true;
                    
                    Client.PlayerCastBodyAnimationSoundAndMessageOnPosition(spell, target);

                    foreach (var script in spell.Scripts.Values)
                        script.OnUse(this, target);

                    spell.InUse = false;
                }
                else
                {
                    if (spell.Template.TargetType != SpellTemplate.SpellUseType.NoTarget)
                    {
                        Client.SpellCastInfo = null;
                        return;
                    }

                    spell.InUse = true;

                    Client.PlayerCastBodyAnimationSoundAndMessage(spell, this);

                    foreach (var script in spell.Scripts.Values)
                        script.OnUse(this, this);

                    spell.InUse = false;
                }

                spell.CurrentCooldown = spell.Template.Cooldown > 0 ? spell.Template.Cooldown : 0;
                Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
            }
            else
            {
                Client.SendServerMessage(ServerMessageType.OrangeBar2, "Issue casting spell; Code: Crocodile");
                Client.SpellCastInfo = null;
            }
        }

        Client.Aisling.IsCastingSpell = false;
        Client.SpellCastInfo = null;
    }

    public void FinishExchange()
    {
        var trader = Exchange.Trader;
        var exchangeA = Exchange;
        var exchangeB = trader.Exchange;
        var itemsA = exchangeA.Items.ToArray();
        var itemsB = exchangeB.Items.ToArray();
        var goldA = exchangeA.Gold;
        var goldB = exchangeB.Gold;

        Exchange = null;
        trader.Exchange = null;

        foreach (var item in itemsB)
            item.GiveTo(this);

        foreach (var item in itemsA)
            item.GiveTo(trader);

        GoldPoints += goldB;
        trader.GoldPoints += goldA;

        if (trader.GoldPoints > (uint)ServerSetup.Instance.Config.MaxCarryGold)
            trader.GoldPoints = (uint)ServerSetup.Instance.Config.MaxCarryGold;
        if (GoldPoints > (uint)ServerSetup.Instance.Config.MaxCarryGold)
            GoldPoints = (uint)ServerSetup.Instance.Config.MaxCarryGold;

        exchangeA.Items.Clear();
        exchangeB.Items.Clear();

        trader.Client?.SendAttributes(StatUpdateType.ExpGold);
        trader.Client?.SendAttributes(StatUpdateType.Primary);
        Client?.SendAttributes(StatUpdateType.ExpGold);
        Client?.SendAttributes(StatUpdateType.Primary);
    }

    public IEnumerable<Skill> GetAssails()
    {
        return SkillBook.GetSkills(i => i?.Template != null && i.Template.SkillType == SkillScope.Assail);
    }

    public bool GiveGold(uint offer, bool sendClientUpdate = true)
    {
        if (GoldPoints + offer < ServerSetup.Instance.Config.MaxCarryGold)
        {
            GoldPoints += offer;
            return true;
        }

        if (sendClientUpdate) Client?.SendAttributes(StatUpdateType.ExpGold);

        return false;
    }

    public Aisling GiveHealth(Sprite target, int value)
    {
        target.CurrentHp += value;

        if (target.CurrentHp > target.MaximumHp) target.CurrentHp = target.MaximumHp;

        return this;
    }

    public void GoHome()
    {
        var destinationMap = ServerSetup.Instance.Config.TransitionZone;

        if (ServerSetup.Instance.GlobalMapCache.ContainsKey(destinationMap))
        {
            Client.Aisling.Pos = new Vector2(ServerSetup.Instance.Config.TransitionPointX, ServerSetup.Instance.Config.TransitionPointY);
            Client.Aisling.CurrentMapId = destinationMap;
        }

        Client.LeaveArea(true, true);
        Client.Enter();
    }

    public bool HasInInventory(string item, int count, out int found)
    {
        var template = ServerSetup.Instance.GlobalItemTemplateCache[item];
        found = 0;

        if (!ServerSetup.Instance.GlobalItemTemplateCache.ContainsKey(item))
            return false;

        if (template == null) return false;
        found = Inventory.Has(template);

        return count == found;
    }

    public bool HasItem(string item)
    {
        var itemObj = Inventory.Has(i => i.Template.Name.Equals(item, StringComparison.OrdinalIgnoreCase));

        return itemObj != null;
    }

    public Item HasItemReturnItem(string item)
    {
        return Inventory.Has(i => i.Template.Name.Equals(item, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasStacks(string item, ushort amount)
    {
        var itemObj = Inventory.Has(i => i.Template.Name.Equals(item, StringComparison.OrdinalIgnoreCase));

        return itemObj != null && itemObj.Stacks >= amount;
    }

    public bool HasKilled(string value, int number)
    {
        if (value == null || number == 0) return false;

        if (MonsterKillCounters.TryGetValue(value, out var killRecord)) return killRecord.TotalKills >= number;

        return false;
    }

    public Aisling HasManaFor(Spell spell)
    {
        if (CurrentMp >= spell.Template.ManaCost)
            return this;
        Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.NoManaMessage}");

        return null;
    }

    public bool HasVisitedMap(int mapId)
    {
        return DiscoveredMaps.Contains(mapId);
    }

    public void ExplorePosition(string quest)
    {
        ExplorePositions.TryAdd($"{Pos}{CurrentMapId}", quest);
    }

    public bool IsDead() => Flags.PlayerFlagIsSet(AislingFlags.Ghost);

    public bool IsWearing(string item)
    {
        return EquipmentManager.Equipment.Any(i => i.Value != null && i.Value.Item.Template.Name.Contains(item));
    }

    public void Recover()
    {
        if (Client == null) return;

        CurrentHp = MaximumHp;
        CurrentMp = MaximumMp;

        Client.SendAttributes(StatUpdateType.Vitality);
    }

    public void Remove(bool update = false, bool delete = true)
    {
        if (update)
        {
            var players = AislingsNearby();
            foreach (var player in players.Where(s => s.Serial != Serial))
            {
                player.Client.SendRemoveObject(Serial);
            }
        }

        try
        {
            if (!delete) return;
            var objs = GetObjects(Map,
                i => i.WithinRangeOf(this, false) && i.Target != null && i.Target.Serial == Serial,
                Get.Monsters);

            if (objs == null) return;
            foreach (var obj in objs)
                obj.Target = null;
        }
        finally
        {
            if (delete)
                DelObject(this);
        }
    }

    public void ReviveInFront()
    {
        var infront = DamageableGetInFront().OfType<Aisling>();

        foreach (var obj in infront)
        {
            if (obj.Serial == Serial)
                continue;

            if (!obj.LoggedIn)
                continue;

            if (obj.HasDebuff("Skulled"))
                obj.RemoveDebuff("Skulled", true);

            obj.Client.Revive();
            obj.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(5, obj.Serial));
        }

        ApplyDamage(this, 0, null);
        Client.SendSound(8, false);
        Client.SendBodyAnimation(Serial, BodyAnimation.Assail, 35);
    }

    public void ReviveFromAfar(Aisling aisling)
    {
        if (aisling.HasDebuff("Skulled"))
            aisling.RemoveDebuff("Skulled", true);

        aisling.Client.Revive();
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(5, aisling.Serial));

        ApplyDamage(this, 0, null);
        Client.SendSound(8, false);
        Client.SendBodyAnimation(Serial, BodyAnimation.Assail, 35);
    }

    public void PrepareForHell()
    {
        if (!ServerSetup.Instance.GlobalMapCache.ContainsKey(ServerSetup.Instance.Config.DeathMap)) return;
        if (CurrentMapId == ServerSetup.Instance.Config.DeathMap) return;

        Remains.Owner = this;
        Remains.Reap(this);
        RemoveBuffsAndDebuffs();
        WarpToHell();
    }

    public override string ToString() => Username;

    public Aisling TrainSpell(Spell lpSpell)
    {
        Client.TrainSpell(lpSpell);
        return this;
    }

    public Aisling UpdateStats(Spell lpSpell)
    {
        Client.SendAttributes(StatUpdateType.Full);
        return this;
    }

    public void UpdateStats()
    {
        Client.SendAttributes(StatUpdateType.Full);
    }

    public void WarpToHell()
    {
        Client.LeaveArea(true, true);
        Pos = new Vector2(ServerSetup.Instance.Config.DeathMapX, ServerSetup.Instance.Config.DeathMapY);
        Direction = 0;
        Client.Aisling.CurrentMapId = ServerSetup.Instance.Config.DeathMap;
        Client.Enter();

        UpdateStats();
    }
}