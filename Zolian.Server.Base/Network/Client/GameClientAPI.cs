using System.Globalization;
using System.Numerics;
using Dapper;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.GameScripts.Formulas;
using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using ServiceStack;

namespace Darkages.Network.Client;

public partial class GameClient : IGameClient
{
    public GameClient GhostFormToAisling()
    {
        Aisling.Flags = AislingFlags.Normal;
        Aisling.RegenTimerDisabled = false;
        UpdateDisplay();
        Task.Delay(500).ContinueWith(ct => { ClientRefreshed(); });
        return this;
    }

    public GameClient AislingToGhostForm()
    {
        Aisling.Flags = AislingFlags.Ghost;
        Aisling.CurrentHp = 0;
        Aisling.CurrentMp = 0;
        Aisling.RegenTimerDisabled = true;
        UpdateDisplay();
        Task.Delay(500).ContinueWith(ct => { ClientRefreshed(); });
        return this;
    }

    public GameClient LearnSkill(Mundane source, SkillTemplate subject, string message)
    {
        var canLearn = false;

        if (subject.Prerequisites != null) canLearn = PayPrerequisites(subject.Prerequisites);
        if (subject.LearningRequirements != null && subject.LearningRequirements.Any()) canLearn = subject.LearningRequirements.TrueForAll(PayPrerequisites);
        if (!canLearn) return this;

        var skill = Skill.GiveTo(this, subject.Name);
        if (skill) LoadSkillBook();
        SendOptionsDialog(source, message);

        Aisling.Show(Scope.NearbyAislings,
            new ServerFormat29((uint)Aisling.Serial, (uint)source.Serial,
                subject.TargetAnimation,
                subject.TargetAnimation, 100));

        return this;
    }

    public GameClient LearnSpell(Mundane source, SpellTemplate subject, string message)
    {
        var canLearn = false;

        if (subject.Prerequisites != null) canLearn = PayPrerequisites(subject.Prerequisites);
        if (subject.LearningRequirements != null && subject.LearningRequirements.Any()) canLearn = subject.LearningRequirements.TrueForAll(PayPrerequisites);
        if (!canLearn) return this;

        var spell = Spell.GiveTo(this, subject.Name);
        if (spell) LoadSpellBook();
        SendOptionsDialog(source, message);

        Aisling.Show(Scope.NearbyAislings,
            new ServerFormat29((uint)Aisling.Serial, (uint)source.Serial,
                subject.TargetAnimation,
                subject.TargetAnimation, 100));

        return this;
    }

    public bool CastSpell(string spellName, Sprite caster, Sprite target)
    {
        if (ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey(spellName))
        {
            var scripts = ScriptManager.Load<SpellScript>(spellName,
                Spell.Create(1, ServerSetup.Instance.GlobalSpellTemplateCache[spellName]));
            {
                foreach (var script in scripts.Values) script.OnUse(caster, target);

                return true;
            }
        }

        return false;
    }

    public async Task EffectAsync(ushort n, int d = 1000, int r = 1)
    {
        if (r <= 0)
            r = 1;

        for (var i = 0; i < r; i++)
        {
            Aisling.SendAnimation(n, Aisling, Aisling);

            foreach (var obj in Aisling.MonstersNearby()) obj.SendAnimation(n, obj.Position);
            await Task.Delay(d).ConfigureAwait(true);
        }
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
        Aisling.SkillBook.Remove(skill.Slot);
        {
            Send(new ServerFormat2D(skill.Slot));
        }
    }

    public void DeleteSkillFromDb(Skill skill)
    {
        var sConn = new SqlConnection(AislingStorage.ConnectionString);
        if (skill.SkillId == 0) return;

        try
        {
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSkillBook WHERE SkillId = @SkillId";
            sConn.Execute(cmd, new { skill.SkillId });
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

    private void ForgetSpellSend(Spell spell)
    {
        Aisling.SpellBook.Remove(spell.Slot);
        {
            Send(new ServerFormat18(spell.Slot));
        }
    }

    public void DeleteSpellFromDb(Spell spell)
    {
        var sConn = new SqlConnection(AislingStorage.ConnectionString);
        if (spell.SpellId == 0) return;

        try
        {
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSpellBook WHERE SpellId = @SpellId";
            sConn.Execute(cmd, new { spell.SpellId });
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

    #region Give Stats

    public void GiveHp(int v = 1)
    {
        Aisling.BaseHp += v;

        if (Aisling.BaseHp > ServerSetup.Instance.Config.MaxHP)
            Aisling.BaseHp = ServerSetup.Instance.Config.MaxHP;

        SendStats(StatusFlags.StructA);
    }

    public void GiveMp(int v = 1)
    {
        Aisling.BaseMp += v;

        if (Aisling.BaseMp > ServerSetup.Instance.Config.MaxHP)
            Aisling.BaseMp = ServerSetup.Instance.Config.MaxHP;

        SendStats(StatusFlags.StructA);
    }

    public void GiveStr(byte v = 1)
    {
        Aisling._Str += v;
        SendStats(StatusFlags.StructA);
    }

    public void GiveInt(byte v = 1)
    {
        Aisling._Int += v;
        SendStats(StatusFlags.StructA);
    }

    public void GiveWis(byte v = 1)
    {
        Aisling._Wis += v;
        SendStats(StatusFlags.StructA);
    }

    public void GiveCon(byte v = 1)
    {
        Aisling._Con += v;
        SendStats(StatusFlags.StructA);
    }

    public void GiveDex(byte v = 1)
    {
        Aisling._Dex += v;
        SendStats(StatusFlags.StructA);
    }

    #endregion

    public void GiveExp(uint a)
    {
        HandleExp(Aisling, a);
        SendStats(StatusFlags.ExpSpend);
    }

    public void HandleExp(Aisling player, uint exp)
    {
        if (exp <= 0) exp = 1;

        player.Client.SendMessage(0x03, $"Received {exp:n0} experience points!");
        player.ExpTotal += exp;
        player.ExpNext -= (int)exp;

        if (player.ExpNext >= int.MaxValue) player.ExpNext = 0;

        var seed = player.ExpLevel * 0.1 + 0.5;
        {
            if (player.ExpLevel >= ServerSetup.Instance.Config.PlayerLevelCap)
                return;
        }

        while (player.ExpNext <= 0 && player.ExpLevel < 500)
        {
            player.ExpNext = (int)(player.ExpLevel * seed * 5000);

            if (player.ExpLevel == 500)
                break;

            if (player.ExpTotal <= 0)
                player.ExpTotal = uint.MaxValue;

            if (player.ExpTotal >= uint.MaxValue)
                player.ExpTotal = uint.MaxValue;

            if (player.ExpNext <= 0)
                player.ExpNext = 1;

            if (player.ExpNext >= int.MaxValue)
                player.ExpNext = int.MaxValue;

            player.Client.LevelUp(player);
        }
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

        player.Client.SendMessage(0x03, $"{ServerSetup.Instance.Config.LevelUpMessage}, Insight:{player.ExpLevel}");
        player.Show(Scope.NearbyAislings, new ServerFormat29((uint)player.Serial, (uint)player.Serial, 0x004F, 0x004F, 64));
        player.Client.SendStats(StatusFlags.ExpSpend);
    }

    public void GiveScar()
    {
        var item = new Legend.LegendItem
        {
            Category = "Event",
            Time = DateTime.Now,
            Color = LegendColor.Red,
            Icon = (byte)LegendIcon.Warrior,
            Value = "Fragment of spark taken.."
        };

        Aisling.LegendBook.AddLegend(item, this);
    }

    public bool GiveTutorialArmor()
    {
        var item = Aisling.Gender == Gender.Male ? "Shirt" : "Blouse";
        return GiveItem(item);
    }

    public bool IsBehind(Sprite sprite)
    {
        var delta = sprite.Direction - Aisling.Direction;
        return Aisling.Position.IsNextTo(sprite.Position) && delta == 0;
    }

    public void KillPlayer(string u)
    {
        if (u.IsNullOrEmpty()) return;
        var user = ObjectHandlers.GetObject<Aisling>(null, i => i.Username.Equals(u, StringComparison.OrdinalIgnoreCase));

        if (user != null)
            user.CurrentHp = 0;
    }

    public void LearnEverything()
    {
        Aisling.SkillBook = new SkillBook();
        Aisling.SpellBook = new SpellBook();

        foreach (var skill in ServerSetup.Instance.GlobalSkillTemplateCache.Values.Where(skill => skill != null))
            Skill.GiveTo(Aisling, skill.Name);

        foreach (var spell in ServerSetup.Instance.GlobalSpellTemplateCache.Values.Where(spell => spell != null))
            Spell.GiveTo(Aisling, spell.Name);


        LoadSkillBook();
        LoadSpellBook();
    }

    public GameClient LoggedIn(bool state)
    {
        Aisling.LoggedIn = state;

        return this;
    }

    public void OpenBoard(string n)
    {
        if (ServerSetup.Instance.GlobalBoardCache.ContainsKey(n))
        {
            var boardListObj = ServerSetup.Instance.GlobalBoardCache[n];

            if (boardListObj != null && boardListObj.Any())
                Send(new BoardList(boardListObj));
        }
    }

    public void Port(int i, int x = 0, int y = 0)
    {
        TransitionToMap(i, new Position(x, y));
    }

    public void ResetLocation(GameClient client)
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
        if (u.IsNullOrEmpty()) return;
        var user = ObjectHandlers.GetObject<Aisling>(null, i => i.Username.Equals(u, StringComparison.OrdinalIgnoreCase));

        if (user is { LoggedIn: true })
            user.Client.Revive();
    }

    public void Spawn(string monster, int x, int y, int amount = 1)
    {
        var (_, value) = ServerSetup.Instance.GlobalMonsterTemplateCache
            .FirstOrDefault(i => i.Value.Name.Equals(monster, StringComparison.CurrentCulture));

        if (value != null)
        {
            for (var i = 0; i < amount; i++)
            {
                var mon = Monster.Create(value, Aisling.Map);
                if (mon == null) continue;
                mon.Pos = new Vector2(x, y);

                ObjectHandlers.AddObject(mon);
            }

            SystemMessage($"Spawn: Successfully spawned {amount} {monster}(s).");
        }
        else
        {
            SystemMessage("Spawn: No monster found in cache.");
        }
    }

    public void StressTest()
    {
        Task.Run(async () =>
        {
            for (var n = 0; n < 5000; n++)
            for (byte i = 0; i < 100; i++)
                await EffectAsync(i, 500).ConfigureAwait(false);
        });
    }

    public GameClient ApproachGroup(Aisling targetAisling, IReadOnlyList<string> allowedMaps)
    {
        if (targetAisling.GroupParty?.PartyMembers == null) return this;
        foreach (var member in targetAisling.GroupParty.PartyMembers.Where(member => member.Serial != Aisling.Serial).Where(member => allowedMaps.ListContains(member.Map.Name)))
        {
            Aisling.Client.SendAnimation(67, targetAisling, targetAisling);
            Aisling.Client.TransitionToMap(targetAisling.Map, targetAisling.Position);
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

        Send(new ServerFormat2C(skill.Slot, skill.Icon, skill.Name));
        skill.CurrentCooldown = skill.Template.Cooldown;
        Aisling.Client.Send(new ServerFormat3F(1, skill.Slot, skill.CurrentCooldown));

        SendMessage(0x02,
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

        Send(new ServerFormat17(spell));
        spell.CurrentCooldown = spell.Template.Cooldown;
        Aisling.Client.Send(new ServerFormat3F(0, spell.Slot, spell.CurrentCooldown));

        SendMessage(0x02,
            spell.Level >= 100
                ? string.Format(CultureInfo.CurrentUICulture, "{0} has been mastered.", spell.Template.Name)
                : string.Format(CultureInfo.CurrentUICulture, "{0} improved, Lv:{1}", spell.Template.Name, spell.Level));
    }

    public void TakeAwayQuantity(Sprite owner, string item, int range)
    {
        var foundItem = Aisling.Inventory.Has(i => i.Template.Name.Equals(item, StringComparison.OrdinalIgnoreCase));
        if (foundItem == null) return;

        Aisling.Inventory.RemoveRange(Aisling.Client, foundItem, range);
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
            Aisling.Client.Send(new ServerFormat37(item, (byte)key));
        }

        var reapplyMods = new Item();
        reapplyMods.ReapplyItemModifiers(Aisling.Client);

        SendStats(StatusFlags.All);
    }

    public bool Revive()
    {
        Aisling.Flags = AislingFlags.Normal;
        Aisling.RegenTimerDisabled = false;
        Aisling.Client.Send(new ServerFormat3A(89, byte.MinValue));

        Aisling.CurrentHp = (int)(Aisling.MaximumHp * 0.80);
        Aisling.CurrentMp = (int)(Aisling.MaximumMp * 0.80);

        SendStats(StatusFlags.Health);
        return Aisling.CurrentHp > 0;
    }
}