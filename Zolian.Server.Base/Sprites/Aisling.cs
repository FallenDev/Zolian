using Chaos.Common.Definitions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Managers;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace Darkages.Sprites;

public record KillRecord
{
    public int TotalKills { get; set; }
    public DateTime TimeKilled { get; set; }
}

public record IgnoredRecord
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
    public readonly ConcurrentDictionary<short, PostTemplate> PersonalLetters = new();
    public AislingTrackers AislingTrackers { get; }
    public Stopwatch LawsOfAosda { get; set; } = new();
    public bool BlessedShield;
    public uint MaximumWeight => GameMaster switch
    {
        true => 999,
        false => (uint)(Math.Round(ExpLevel / 2d) + _Str + ServerSetup.Instance.Config.WeightIncreaseModifer + 200)
    };

    public bool Overburden => CurrentWeight > MaximumWeight;
    public bool Dead => IsDead();
    public bool RegenTimerDisabled;
    public bool Skulled => HasDebuff("Skulled");
    public Party GroupParty => ServerSetup.Instance.GlobalGroupCache.GetValueOrDefault(GroupId);
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
        DiscoveredMaps = new List<int>();
        IgnoredList = new List<string>();
        GroupId = 0;
        AttackDmgTrack = new WorldServerTimer(TimeSpan.FromSeconds(1));
        ThreatTimer = new WorldServerTimer(TimeSpan.FromSeconds(60));
        ChantTimer = new ChantTimer(1500);
        TileType = TileContent.Aisling;
        AislingTrackers = new AislingTrackers(TimeSpan.FromSeconds(1));
    }

    public bool Loading { get; set; }
    public long DamageCounter { get; set; }
    public long ThreatMeter { get; set; }
    public DialogSequence ActiveSequence { get; set; }
    public ExchangeSession Exchange { get; set; }
    public NameDisplayStyle NameStyle { get; set; }
    public ElementManager.Element TempOffensiveHold { get; set; }
    public ElementManager.Element TempDefensiveHold { get; set; }
    public bool IsCastingSpell { get; set; }
    public bool ProfileOpen { get; set; }
    public Summon SummonObjects { get; set; }
    public bool UsingTwoHanded { get; set; }
    public int LastMapId { get; set; }
    public WorldServerTimer AttackDmgTrack { get; }
    public WorldServerTimer ThreatTimer { get; set; }
    public ChantTimer ChantTimer { get; }
    public UserOptions GameSettings { get; init; } = new();
    public Mail MailFlags { get; set; }
    public SkillBook SkillBook { get; set; }
    public SpellBook SpellBook { get; set; }
    public string ActionUsed { get; set; }
    public bool ThrewHealingPot { get; set; }
    public Death Remains { get; set; }
    public Legend LegendBook { get; set; }
    public byte[] PictureData { get; set; }
    public BankManager BankManager { get; set; }
    public InventoryManager Inventory { get; set; }
    public EquipmentManager EquipmentManager { get; set; }
    public ComboScroll ComboManager { get; set; }
    public Quests QuestManager { get; set; }
    public List<int> DiscoveredMaps { get; set; }
    public int Styling { get; set; }
    public int Coloring { get; set; }
    public byte OldColor { get; set; }
    public byte OldStyle { get; set; }
    public List<string> IgnoredList { get; set; }
    public ConcurrentDictionary<string, string> ExplorePositions { get; set; }
    public Vector2 DeathLocation { get; set; }
    public int DeathMapId { get; set; }

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
    public bool Lycanisim => Afflictions.AfflictionFlagIsSet(Afflictions.Lycanisim);
    public bool Vampirisim => Afflictions.AfflictionFlagIsSet(Afflictions.Vampirisim);
    public bool Plagued => Afflictions.AfflictionFlagIsSet(Afflictions.Plagued);
    public bool TheShakes => Afflictions.AfflictionFlagIsSet(Afflictions.TheShakes);
    public bool Stricken => Afflictions.AfflictionFlagIsSet(Afflictions.Stricken);
    public bool Rabies => Afflictions.AfflictionFlagIsSet(Afflictions.Rabies);
    public int RabiesCountDown { get; set; }
    public bool LockJoint => Afflictions.AfflictionFlagIsSet(Afflictions.LockJoint);
    public bool NumbFall => Afflictions.AfflictionFlagIsSet(Afflictions.NumbFall);
    public bool Diseased => Afflictions.AfflictionFlagIsSet(Afflictions.Plagued) && Afflictions.AfflictionFlagIsSet(Afflictions.TheShakes);
    public bool Hallowed => Afflictions.AfflictionFlagIsSet(Afflictions.Plagued) && Afflictions.AfflictionFlagIsSet(Afflictions.Stricken);
    public bool Petrified => Afflictions.AfflictionFlagIsSet(Afflictions.LockJoint) && Afflictions.AfflictionFlagIsSet(Afflictions.NumbFall);

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
        var selectedPlayers = new List<Aisling>();

        switch (op)
        {
            case Scope.NearbyAislingsExludingSelf:
                selectedPlayers.AddRange(GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers)).Where(player => player.Serial != Serial));
                break;
            case Scope.NearbyAislings:
                selectedPlayers.AddRange(GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers)));
                break;
            case Scope.Clan:
                selectedPlayers.AddRange(GetObjects<Aisling>(null, otherPlayers => otherPlayers != null && !string.IsNullOrEmpty(otherPlayers.Clan) && string.Equals(otherPlayers.Clan, Clan, StringComparison.CurrentCultureIgnoreCase)));
                break;
            case Scope.VeryNearbyAislings:
                selectedPlayers.AddRange(GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers, ServerSetup.Instance.Config.VeryNearByProximity)));
                break;
            case Scope.AislingsOnSameMap:
                selectedPlayers.AddRange(GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && CurrentMapId == otherPlayers.CurrentMapId));
                break;
            case Scope.GroupMembers:
                selectedPlayers.AddRange(GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && GroupParty.Has(otherPlayers)));
                break;
            case Scope.NearbyGroupMembersExcludingSelf:
                selectedPlayers.AddRange(GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers) && GroupParty.Has(otherPlayers)).Where(player => player.Serial != Serial));
                break;
            case Scope.NearbyGroupMembers:
                selectedPlayers.AddRange(GetObjects<Aisling>(Map, otherPlayers => otherPlayers != null && WithinRangeOf(otherPlayers) && GroupParty.Has(otherPlayers)));
                break;
            case Scope.DefinedAislings when definer == null:
                return;
            case Scope.DefinedAislings:
                selectedPlayers.AddRange(definer);
                break;
            case Scope.All:
                selectedPlayers.AddRange(ServerSetup.Instance.Game.Aislings);
                break;
            case Scope.Self:
            default:
                method(Client);
                return;
        }

        foreach (var player in selectedPlayers.Where(player => player?.Client != null))
        {
            if (method.Method.Name.Contains("CastAnimation") || method.Method.Name.Contains("OnSuccess") || method.Method.Name.Contains("OnApplied"))
            {
                if (!player.GameSettings.Animations) continue;
            }

            method(player.Client);
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

        if (trader.GoldPoints > ServerSetup.Instance.Config.MaxCarryGold)
            trader.GoldPoints = ServerSetup.Instance.Config.MaxCarryGold;
        if (GoldPoints > ServerSetup.Instance.Config.MaxCarryGold)
            GoldPoints = ServerSetup.Instance.Config.MaxCarryGold;

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
        if (!skill.CanUse() && skill.Template.SkillType != SkillScope.Assail)
            Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{skill.Name} not ready yet.");
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
        if (CurrentMp < spell.Template.ManaCost)
        {
            Client.SendServerMessage(ServerMessageType.ActiveMessage, "I need more mana!");
            return;
        }

        try
        {
            if (info != null)
            {
                if (!string.IsNullOrEmpty(info.Data))
                    foreach (var script in spell.Scripts.Values)
                        script.Arguments = info.Data;

                if (spell.Scripts != null)
                {
                    if (info.Target != 0)
                    {
                        if (IsInvisible && (spell.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.BreakInvisible) || spell.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.Both)))
                        {
                            if (Buffs.TryRemove("Hide", out var hide))
                            {
                                hide.OnEnded(Client.Aisling, hide);
                            }

                            if (Buffs.TryRemove("Shadowfade", out var shadowFade))
                            {
                                shadowFade.OnEnded(Client.Aisling, shadowFade);
                            }

                            Client.UpdateDisplay();
                        }

                        spell.InUse = true;

                        var target = GetObject(Map, i => i.Serial == info.Target, Get.Monsters | Get.Aislings);
                        CastAnimation(spell);
                        var script = spell.Scripts.Values.FirstOrDefault();
                        script?.OnUse(this, target);
                        spell.CurrentCooldown = spell.Template.Cooldown > 0 ? spell.Template.Cooldown : 0;
                        Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
                        spell.LastUsedSpell = DateTime.UtcNow;

                        spell.InUse = false;
                    }
                }
                else
                {
                    Client.SendServerMessage(ServerMessageType.OrangeBar2, "Issue casting spell; Code: Crocodile");
                    Client.SpellCastInfo = null;
                }
            }
        }
        catch (Exception e)
        {
            if (info == null)
                ServerSetup.Logger($"{Username} tried to cast {spell.Name} and info was null");
            if (info?.Target == 0)
                ServerSetup.Logger($"{Username} tried to cast {spell.Name} and target was 0");
            Crashes.TrackError(e);
        }

        IsCastingSpell = false;
        Client.SpellCastInfo = null;
    }

    /// <summary>
    /// Displays player's body animation, spell's sound
    /// </summary>
    public Aisling CastAnimation(Spell spell, byte actionSpeed = 30)
    {
        var bodyAnim = Path switch
        {
            Class.Cleric => (BodyAnimation)128,
            Class.Arcanus => (BodyAnimation)136,
            Class.Monster => (BodyAnimation)1,
            _ => (BodyAnimation)6
        };

        SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendBodyAnimation(Serial, bodyAnim, actionSpeed, spell.Template.Sound));

        return this;
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

        if (trader.GoldPoints > ServerSetup.Instance.Config.MaxCarryGold)
            trader.GoldPoints = ServerSetup.Instance.Config.MaxCarryGold;
        if (GoldPoints > ServerSetup.Instance.Config.MaxCarryGold)
            GoldPoints = ServerSetup.Instance.Config.MaxCarryGold;

        exchangeA.Items.Clear();
        exchangeB.Items.Clear();

        trader.Client?.SendAttributes(StatUpdateType.ExpGold);
        trader.Client?.SendAttributes(StatUpdateType.Primary);
        Client?.SendAttributes(StatUpdateType.ExpGold);
        Client?.SendAttributes(StatUpdateType.Primary);
    }

    public IEnumerable<Skill> GetAssails()
    {
        return SkillBook.GetSkills(i => i?.Template is { SkillType: SkillScope.Assail });
    }

    public Skill GetSkill(string s)
    {
        return SkillBook.GetSkills(i => i?.Template.Name == s).First();
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

        Client.LeaveArea(destinationMap, true, true);
        Client.Enter();
    }

    public bool HasInInventory(string item, int count)
    {
        var found = ServerSetup.Instance.GlobalItemTemplateCache.TryGetValue(item, out var template);
        if (!found) return false;
        var amount = Inventory.Has(template);
        return count <= amount;
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
            foreach (var player in players.Where(s => s != null && s.Serial != Serial))
            {
                player.Client.SendRemoveObject(Serial);
            }
        }

        try
        {
            if (!delete) return;
            var objs = GetObjects(Map,
                i => i.WithinRangeOf(this) && i.Target != null && i.Target.Serial == Serial,
                Get.Monsters).ToArray();

            if (objs.Length == 0) return;
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
            obj.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(5, null, obj.Serial));
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
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(5, null, aisling.Serial));

        ApplyDamage(this, 0, null);
        Client.SendSound(8, false);
        Client.SendBodyAnimation(Serial, BodyAnimation.Assail, 35);
    }

    public void PrepareForHell()
    {
        if (!ServerSetup.Instance.GlobalMapCache.ContainsKey(ServerSetup.Instance.Config.DeathMap)) return;
        if (CurrentMapId == ServerSetup.Instance.Config.DeathMap) return;

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
        Client.LeaveArea(ServerSetup.Instance.Config.DeathMap, true, true);
        Pos = new Vector2(ServerSetup.Instance.Config.DeathMapX, ServerSetup.Instance.Config.DeathMapY);
        Direction = 0;
        Client.Aisling.CurrentMapId = ServerSetup.Instance.Config.DeathMap;
        Client.Enter();

        UpdateStats();
    }

    public new bool Walk()
    {
        if (CantMove) return false;

        if (Resting != Enums.RestPosition.Standing)
        {
            Resting = Enums.RestPosition.Standing;
            Client.SendAttributes(StatUpdateType.Full);
            Client.UpdateDisplay();
        }

        var oldPosX = X;
        var oldPosY = Y;

        PendingX = X;
        PendingY = Y;

        var allowGhostWalk = GameMaster || Dead;

        // Check position before we add direction, add direction, check position to see if we can commit
        if (!allowGhostWalk)
        {
            if (Map.IsWall(oldPosX, oldPosY)) return false;
            if (Map.IsSpriteInLocationOnWalk(this, PendingX, PendingY)) return false;
        }

        switch (Direction)
        {
            case 0:
                PendingY--;
                break;
            case 1:
                PendingX++;
                break;
            case 2:
                PendingY++;
                break;
            case 3:
                PendingX--;
                break;
        }

        if (!allowGhostWalk)
        {
            if (Map.IsWall(PendingX, PendingY)) return false;
            if (Map.IsSpriteInLocationOnWalk(this, PendingX, PendingY)) return false;
        }

        foreach (var player in AislingsNearby())
        {
            if (player.Serial == Serial) continue;
            player.Client.SendCreatureWalk(Serial, new Point(oldPosX, oldPosY), (Direction)Direction);
        }

        LastPosition = new Position(oldPosX, oldPosY);
        Pos = new Vector2(PendingX, PendingY);
        LastMovementChanged = DateTime.UtcNow;

        Client.SendConfirmClientWalk(new Position(oldPosX, oldPosY), (Direction)Direction);

        // Reset our PendingX & PendingY
        PendingX = oldPosX;
        PendingY = oldPosY;

        return true;
    }

    public void AutoRoutine()
    {
        var monster = MonstersNearby().FirstOrDefault(i => i.WithinRangeOf(this, 2));
        if (monster == null) return;
        Target = monster;
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        while (NextTo(Target!.X, Target!.Y) && Client.Connected)
        {
            if (!(stopWatch.Elapsed.TotalMilliseconds > 1000)) continue;
            stopWatch.Restart();

            foreach (var skill in SkillBook.Skills.Values)
            {
                if (skill is null) continue;
                if (!skill.CanUse()) continue;
                if (skill.Scripts is null || skill.Scripts.IsEmpty) continue;

                skill.InUse = true;

                var script = skill.Scripts.Values.First();
                script?.OnUse(this);
                skill.CurrentCooldown = skill.Template.Cooldown;
                Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
                skill.LastUsedSkill = DateTime.UtcNow;

                if (skill.Template.SkillType == SkillScope.Assail)
                    Client.LastAssail = DateTime.UtcNow;

                skill.InUse = false;
            }
        }

        stopWatch.Stop();
    }
}