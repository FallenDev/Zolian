using System.Collections.Concurrent;
using System.Numerics;

using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Formats.Models.ServerFormats;
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
    public int Id { get; init; }
    public int Serial { get; init; }
    public string PlayerIgnored { get; init; }
}

public sealed class Aisling : Player, IAisling
{
    public int EquipmentDamageTaken = 0;
    public readonly ConcurrentDictionary<int, Sprite> View = new();
    public ConcurrentDictionary<string, KillRecord> MonsterKillCounters = new();

    public uint MaximumWeight => GameMaster switch
    {
        true => 999,
        false => (uint)(ExpLevel / 2 + _Str + ServerSetup.Instance.Config.WeightIncreaseModifer)
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
        AttackDmgTrack = new GameServerTimer(TimeSpan.FromSeconds(1));
        ThreatTimer = new GameServerTimer(TimeSpan.FromSeconds(60));
        EntityType = TileContent.Aisling;
    }

    public bool Loading { get; set; }
    public long DamageCounter { get; set; }
    public uint ThreatMeter { get; set; }
    public ReactorTemplate ActiveReactor { get; set; }
    public DialogSequence ActiveSequence { get; set; }
    public bool CanReact { get; set; }
    public ExchangeSession Exchange { get; set; }
    public NameDisplayStyle NameStyle { get; set; }
    public bool IsCastingSpell { get; set; }
    public bool ProfileOpen { get; set; }
    public Summon SummonObjects { get; set; }
    public bool UsingTwoHanded { get; set; }
    public int LastMapId { get; set; }
    public GameServerTimer AttackDmgTrack { get; }
    public GameServerTimer ThreatTimer { get; set; }
    public List<ClientGameSettings> GameSettings { get; set; }
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

        trader.Client.SendStats(StatusFlags.StructC);
        Client.SendStats(StatusFlags.StructC);

        var packet = new NetworkPacketWriter();
        packet.Write((byte)0x42);
        packet.Write((byte)0x00);

        packet.Write((byte)0x04);
        packet.Write((byte)0x00);
        packet.WriteStringA("Trade was aborted.");
        Client.Send(packet);

        packet = new NetworkPacketWriter();
        packet.Write((byte)0x42);
        packet.Write((byte)0x00);

        packet.Write((byte)0x04);
        packet.Write((byte)0x01);
        packet.WriteStringA("Trade was aborted.");
        trader.Client.Send(packet);
    }

    public bool CanSeeGhosts()
    {
        return IsDead();
    }

    public void UsedSkill(Skill skill)
    {
        if (skill == null) return;
        if (!skill.Ready && skill.Template.SkillType != SkillScope.Assail)
            Client.SendMessage(0x03, $"{skill.Name} not ready yet.");
        skill.CurrentCooldown = skill.Template.Cooldown;
        Client.Send(new ServerFormat3F(1, skill.Slot, skill.CurrentCooldown));
    }

    public Aisling Cast(Spell spell, Sprite target, byte actionSpeed = 30)
    {
        var action = new ServerFormat1A
        {
            Serial = Serial,
            Number = Path switch
            {
                Class.Cleric => 0x80,
                Class.Arcanus => 0x88,
                _ => 0x06
            },
            Speed = actionSpeed
        };

        switch (target)
        {
            case null:
                return this;
            case Aisling aislingTarget:
                aislingTarget.Client
                    .SendMessage(0x02, $"{Username} Attacks you with {spell.Template.Name}.");
                break;
        }

        Client.SendMessage(0x02, $"You've cast {spell.Template.Name}.");
        var animation = new ServerFormat29(spell.Template.Animation, target.Pos);
        Show(Scope.NearbyAislings, action);
        Show(Scope.NearbyAislings, animation);

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
        if (spell.InUse)
        {
            Client.SendMessage(0x03, "Currently casting a similar spell");
            return;
        }

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

                    foreach (var script in spell.Scripts.Values)
                        script.OnUse(this, target);

                    spell.InUse = false;
                }
                else
                {
                    if (spell.Template.TargetType != SpellTemplate.SpellUseType.NoTarget) return;

                    spell.InUse = true;

                    foreach (var script in spell.Scripts.Values)
                        script.OnUse(this, this);

                    spell.InUse = false;
                }

                spell.CurrentCooldown = spell.Template.Cooldown > 0 ? spell.Template.Cooldown : 0;
                Client.Send(new ServerFormat3F(0, spell.Slot, spell.CurrentCooldown));
            }
            else
            {
                Client.SendMessage(0x03, "Issue casting spell; Code: Crocodile");
            }
        }

        Client.Aisling.IsCastingSpell = false;
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
            if (item.GiveTo(this))
            {
            }

        foreach (var item in itemsA)
            if (item.GiveTo(trader))
            {
            }

        GoldPoints += goldB;
        trader.GoldPoints += goldA;

        if (trader.GoldPoints > (uint)ServerSetup.Instance.Config.MaxCarryGold)
            trader.GoldPoints = (uint)ServerSetup.Instance.Config.MaxCarryGold;
        if (GoldPoints > (uint)ServerSetup.Instance.Config.MaxCarryGold)
            GoldPoints = (uint)ServerSetup.Instance.Config.MaxCarryGold;

        exchangeA.Items.Clear();
        exchangeB.Items.Clear();

        trader.Client?.SendStats(StatusFlags.WeightMoney);
        Client?.SendStats(StatusFlags.WeightMoney);
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

        if (sendClientUpdate) Client?.SendStats(StatusFlags.StructC);

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
        Client.SendMessage(0x02, $"{ServerSetup.Instance.Config.NoManaMessage}");

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

        Client.SendStats(StatusFlags.Health);
    }

    public void Remove(bool update = false, bool delete = true)
    {
        if (update)
            Show(Scope.NearbyAislingsExludingSelf, new ServerFormat0E(Serial));

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

        var action = new ServerFormat1A
        {
            Serial = Serial,
            Number = 0x01,
            Speed = 30
        };

        foreach (var obj in infront)
        {
            if (obj.Serial == Serial)
                continue;

            if (!obj.LoggedIn)
                continue;

            if (obj.HasDebuff("Skulled"))
                obj.RemoveDebuff("Skulled", true);

            obj.Client.Revive();
            obj.Animate(5);
        }

        ApplyDamage(this, 0, null);
        Show(Scope.NearbyAislings, new ServerFormat19(8));
        Show(Scope.NearbyAislings, action);
    }

    public void ReviveFromAfar(Aisling aisling)
    {
        var action = new ServerFormat1A
        {
            Serial = Serial,
            Number = 0x01,
            Speed = 30
        };

        if (aisling.HasDebuff("Skulled"))
            aisling.RemoveDebuff("Skulled", true);

        aisling.Client.Revive();
        aisling.Animate(5);

        ApplyDamage(this, 0, null);
        Show(Scope.NearbyAislings, new ServerFormat19(8));
        Show(Scope.NearbyAislings, action);
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
        Client.SendStats(StatusFlags.All);
        return this;
    }

    public void UpdateStats()
    {
        Client?.SendStats(StatusFlags.All);
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