using System.Data;
using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Dapper;
using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Sprites;
using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Database;

public record AislingStorage : Sql, IAislingStorage
{
    public const string ConnectionString = "Data Source=.;Initial Catalog=ZolianPlayers;Integrated Security=True;Encrypt=False";
    private const string EncryptedConnectionString = "Data Source=.;Initial Catalog=ZolianPlayers;Integrated Security=True;Column Encryption Setting=enabled;TrustServerCertificate=True";
    private SemaphoreSlim SaveLock { get; } = new(1, 1);
    private SemaphoreSlim LoadLock { get; } = new(1, 1);
    private SemaphoreSlim CreateLock { get; } = new(1, 1);

    public async Task<Aisling> LoadAisling(string name, long serial)
    {
        await LoadLock.WaitAsync().ConfigureAwait(false);

        var aisling = new Aisling();

        try
        {
            var continueLoad = await CheckIfPlayerExists(name, serial);
            if (!continueLoad) return null;

            var sConn = ConnectToDatabase(ConnectionString);
            var values = new { Name = name };
            aisling = await sConn.QueryFirstAsync<Aisling>("[SelectPlayer]", values, commandType: CommandType.StoredProcedure);
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
        finally
        {
            LoadLock.Release();
        }

        return aisling;
    }

    public async Task<bool> PasswordSave(Aisling obj)
    {
        if (obj == null) return false;
        if (obj.Loading) return false;
        var continueLoad = await CheckIfPlayerExists(obj.Username, obj.Serial);
        if (!continueLoad) return false;

        await SaveLock.WaitAsync().ConfigureAwait(false);

        try
        {
            var connection = ConnectToDatabase(EncryptedConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("PasswordSave", connection);
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = obj.Username;
            cmd.Parameters.Add("@Pass", SqlDbType.VarChar).Value = obj.Password;
            cmd.Parameters.Add("@Attempts", SqlDbType.Int).Value = obj.PasswordAttempts;
            cmd.Parameters.Add("@Hacked", SqlDbType.Bit).Value = obj.Hacked;
            cmd.Parameters.Add("@LastIP", SqlDbType.VarChar).Value = obj.LastIP;
            cmd.Parameters.Add("@LastAttemptIP", SqlDbType.VarChar).Value = obj.LastAttemptIP;
            ExecuteAndCloseConnection(cmd, connection);
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your password did not save. Try again.");
                Crashes.TrackError(e);
                return false;
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
            SaveLock.Release();
        }

        return true;
    }

    public async Task QuickSave(Aisling obj)
    {
        if (obj == null) return;
        if (obj.Loading) return;
        var continueLoad = await CheckIfPlayerExists(obj.Username, obj.Serial);
        if (!continueLoad) return;

        await SaveLock.WaitAsync().ConfigureAwait(false);

        try
        {
            var connection = ConnectToDatabase(ConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("PlayerQuickSave", connection);
            SaveSkills(obj, connection);
            SaveSpells(obj, connection);
            SaveBuffs(obj, connection);
            SaveDebuffs(obj, connection);
            var inventory = await SaveItemsForPlayer(obj, connection);

            #region Parameters

            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = obj.Username;
            cmd.Parameters.Add("@X", SqlDbType.Int).Value = obj.X;
            cmd.Parameters.Add("@Y", SqlDbType.Int).Value = obj.Y;
            cmd.Parameters.Add("@CurrentMap", SqlDbType.Int).Value = obj.CurrentMapId;
            cmd.Parameters.Add("@OffensePrimary", SqlDbType.VarChar).Value = obj.OffenseElement;
            cmd.Parameters.Add("@DefensePrimary", SqlDbType.VarChar).Value = obj.DefenseElement;
            cmd.Parameters.Add("@OffenseSecondary", SqlDbType.VarChar).Value = obj.SecondaryOffensiveElement;
            cmd.Parameters.Add("@DefenseSecondary", SqlDbType.VarChar).Value = obj.SecondaryDefensiveElement;
            cmd.Parameters.Add("@Direction", SqlDbType.Int).Value = obj.Direction;
            cmd.Parameters.Add("@CurrentHp", SqlDbType.Int).Value = obj.CurrentHp;
            cmd.Parameters.Add("@BaseHp", SqlDbType.Int).Value = obj.BaseHp;
            cmd.Parameters.Add("@CurrentMp", SqlDbType.Int).Value = obj.CurrentMp;
            cmd.Parameters.Add("@BaseMp", SqlDbType.Int).Value = obj.BaseMp;
            cmd.Parameters.Add("@AC", SqlDbType.Int).Value = obj._ac;
            cmd.Parameters.Add("@Regen", SqlDbType.Int).Value = obj._Regen;
            cmd.Parameters.Add("@Dmg", SqlDbType.Int).Value = obj._Dmg;
            cmd.Parameters.Add("@Hit", SqlDbType.Int).Value = obj._Hit;
            cmd.Parameters.Add("@Mr", SqlDbType.Int).Value = obj._Mr;
            cmd.Parameters.Add("@Str", SqlDbType.Int).Value = obj._Str;
            cmd.Parameters.Add("@Int", SqlDbType.Int).Value = obj._Int;
            cmd.Parameters.Add("@Wis", SqlDbType.Int).Value = obj._Wis;
            cmd.Parameters.Add("@Con", SqlDbType.Int).Value = obj._Con;
            cmd.Parameters.Add("@Dex", SqlDbType.Int).Value = obj._Dex;
            cmd.Parameters.Add("@Luck", SqlDbType.Int).Value = obj._Luck;
            cmd.Parameters.Add("@ABL", SqlDbType.SmallInt).Value = obj.AbpLevel;
            cmd.Parameters.Add("@ABN", SqlDbType.Int).Value = obj.AbpNext;
            cmd.Parameters.Add("@ABT", SqlDbType.Int).Value = obj.AbpTotal;
            cmd.Parameters.Add("@EXPL", SqlDbType.SmallInt).Value = obj.ExpLevel;
            cmd.Parameters.Add("@EXPN", SqlDbType.Int).Value = obj.ExpNext;
            cmd.Parameters.Add("@EXPT", SqlDbType.Int).Value = obj.ExpTotal;
            cmd.Parameters.Add("@Afflix", SqlDbType.VarChar).Value = obj.Afflictions;
            cmd.Parameters.Add("@HairColor", SqlDbType.TinyInt).Value = obj.HairColor;
            cmd.Parameters.Add("@HairStyle", SqlDbType.TinyInt).Value = obj.HairStyle;
            cmd.Parameters.Add("@OldColor", SqlDbType.TinyInt).Value = obj.OldColor;
            cmd.Parameters.Add("@OldStyle", SqlDbType.TinyInt).Value = obj.OldStyle;
            cmd.Parameters.Add("@Animal", SqlDbType.VarChar).Value = obj.AnimalForm;
            cmd.Parameters.Add("@Monster", SqlDbType.SmallInt).Value = obj.MonsterForm;
            cmd.Parameters.Add("@Active", SqlDbType.VarChar).Value = obj.ActiveStatus;
            cmd.Parameters.Add("@Flags", SqlDbType.VarChar).Value = obj.Flags;
            cmd.Parameters.Add("@CurrentWeight", SqlDbType.Int).Value = obj.CurrentWeight;
            cmd.Parameters.Add("@World", SqlDbType.Int).Value = obj.World;
            cmd.Parameters.Add("@Lantern", SqlDbType.TinyInt).Value = obj.Lantern;
            cmd.Parameters.Add("@Invisible", SqlDbType.Bit).Value = obj.IsInvisible;
            cmd.Parameters.Add("@Resting", SqlDbType.VarChar).Value = obj.Resting;
            cmd.Parameters.Add("@PartyStatus", SqlDbType.VarChar).Value = obj.PartyStatus;
            cmd.Parameters.Add("@GoldPoints", SqlDbType.BigInt).Value = obj.GoldPoints;
            cmd.Parameters.Add("@StatPoints", SqlDbType.Int).Value = obj.StatPoints;
            cmd.Parameters.Add("@GamePoints", SqlDbType.BigInt).Value = obj.GamePoints;
            cmd.Parameters.Add("@BankedGold", SqlDbType.BigInt).Value = obj.BankedGold;
            cmd.Parameters.Add("@ArmorImg", SqlDbType.Int).Value = obj.ArmorImg;
            cmd.Parameters.Add("@HelmetImg", SqlDbType.Int).Value = obj.HelmetImg;
            cmd.Parameters.Add("@ShieldImg", SqlDbType.Int).Value = obj.ShieldImg;
            cmd.Parameters.Add("@WeaponImg", SqlDbType.Int).Value = obj.WeaponImg;
            cmd.Parameters.Add("@BootsImg", SqlDbType.Int).Value = obj.BootsImg;
            cmd.Parameters.Add("@HeadAccessoryImg", SqlDbType.Int).Value = obj.HeadAccessoryImg;
            cmd.Parameters.Add("@Accessory1Img", SqlDbType.Int).Value = obj.Accessory1Img;
            cmd.Parameters.Add("@Accessory2Img", SqlDbType.Int).Value = obj.Accessory2Img;
            cmd.Parameters.Add("@Accessory3Img", SqlDbType.Int).Value = obj.Accessory3Img;
            cmd.Parameters.Add("@Accessory1Color", SqlDbType.Int).Value = obj.Accessory1Color;
            cmd.Parameters.Add("@Accessory2Color", SqlDbType.Int).Value = obj.Accessory2Color;
            cmd.Parameters.Add("@Accessory3Color", SqlDbType.Int).Value = obj.Accessory3Color;
            cmd.Parameters.Add("@BodyColor", SqlDbType.TinyInt).Value = obj.BodyColor;
            cmd.Parameters.Add("@BodySprite", SqlDbType.TinyInt).Value = obj.BodySprite;
            cmd.Parameters.Add("@FaceSprite", SqlDbType.TinyInt).Value = obj.FaceSprite;
            cmd.Parameters.Add("@OverCoatImg", SqlDbType.Int).Value = obj.OverCoatImg;
            cmd.Parameters.Add("@BootColor", SqlDbType.TinyInt).Value = obj.BootColor;
            cmd.Parameters.Add("@OverCoatColor", SqlDbType.TinyInt).Value = obj.OverCoatColor;
            cmd.Parameters.Add("@Pants", SqlDbType.TinyInt).Value = obj.Pants;
            cmd.Parameters.Add("@Aegis", SqlDbType.TinyInt).Value = obj.Aegis;
            cmd.Parameters.Add("@Bleeding", SqlDbType.TinyInt).Value = obj.Bleeding;
            cmd.Parameters.Add("@Spikes", SqlDbType.Int).Value = obj.Spikes;
            cmd.Parameters.Add("@Rending", SqlDbType.TinyInt).Value = obj.Rending;
            cmd.Parameters.Add("@Reaping", SqlDbType.TinyInt).Value = obj.Reaping;
            cmd.Parameters.Add("@Vampirism", SqlDbType.TinyInt).Value = obj.Vampirism;
            cmd.Parameters.Add("@Haste", SqlDbType.TinyInt).Value = obj.Haste;
            cmd.Parameters.Add("@Gust", SqlDbType.TinyInt).Value = obj.Gust;
            cmd.Parameters.Add("@Quake", SqlDbType.TinyInt).Value = obj.Quake;
            cmd.Parameters.Add("@Rain", SqlDbType.TinyInt).Value = obj.Rain;
            cmd.Parameters.Add("@Flame", SqlDbType.TinyInt).Value = obj.Flame;
            cmd.Parameters.Add("@Dusk", SqlDbType.TinyInt).Value = obj.Dusk;
            cmd.Parameters.Add("@Dawn", SqlDbType.TinyInt).Value = obj.Dawn;
            
            #endregion

            ExecuteAndCloseConnection(cmd, connection);

            if (inventory == false)
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue with inventory save. (Code: Dying Light)");
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your character did not save. Contact GM (Code: Quick)");
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
            SaveLock.Release();
        }
    }

    public async Task<bool> Save(Aisling obj)
    {
        if (obj == null) return false;
        if (obj.Loading) return false;
        var continueLoad = await CheckIfPlayerExists(obj.Username, obj.Serial);
        if (!continueLoad) return false;

        await SaveLock.WaitAsync().ConfigureAwait(false);

        try
        {
            var connection = ConnectToDatabase(ConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("PlayerSave", connection);
            var cmd2 = ConnectToDatabaseSqlCommandWithProcedure("PlayerQuestSave", connection);

            #region Parameters

            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = obj.Username;
            cmd.Parameters.Add("@LoggedIn", SqlDbType.Bit).Value = obj.LoggedIn;
            cmd.Parameters.Add("@LastLogged", SqlDbType.DateTime).Value = obj.LastLogged;
            cmd.Parameters.Add("@Stage", SqlDbType.VarChar).Value = obj.Stage;
            cmd.Parameters.Add("@Path", SqlDbType.VarChar).Value = obj.Path;
            cmd.Parameters.Add("@PastClass", SqlDbType.VarChar).Value = obj.PastClass;
            cmd.Parameters.Add("@Race", SqlDbType.VarChar).Value = obj.Race;
            cmd.Parameters.Add("@Gender", SqlDbType.VarChar).Value = SpriteMaker.GenderValue(obj.Gender);
            cmd.Parameters.Add("@NameColor", SqlDbType.TinyInt).Value = obj.NameColor;
            cmd.Parameters.Add("@Profile", SqlDbType.VarChar).Value = obj.ProfileMessage ?? "";
            cmd.Parameters.Add("@Nation", SqlDbType.VarChar).Value = obj.Nation;
            cmd.Parameters.Add("@Clan", SqlDbType.VarChar).Value = obj.Clan ?? "";
            cmd.Parameters.Add("@ClanRank", SqlDbType.VarChar).Value = obj.ClanRank ?? "";
            cmd.Parameters.Add("@ClanTitle", SqlDbType.VarChar).Value = obj.ClanTitle ?? "";
            cmd.Parameters.Add("@FireImm", SqlDbType.Bit).Value = obj.FireImmunity;
            cmd.Parameters.Add("@WaterImm", SqlDbType.Bit).Value = obj.WaterImmunity;
            cmd.Parameters.Add("@WindImm", SqlDbType.Bit).Value = obj.WindImmunity;
            cmd.Parameters.Add("@EarthImm", SqlDbType.Bit).Value = obj.EarthImmunity;
            cmd.Parameters.Add("@LightImm", SqlDbType.Bit).Value = obj.LightImmunity;
            cmd.Parameters.Add("@DarkImm", SqlDbType.Bit).Value = obj.DarkImmunity;
            cmd.Parameters.Add("@PoisonImm", SqlDbType.Bit).Value = obj.PoisonImmunity;
            cmd.Parameters.Add("@EnticeImm", SqlDbType.Bit).Value = obj.EnticeImmunity;
            cmd.Parameters.Add("@RaceSkill", SqlDbType.VarChar).Value = obj.RaceSkill ?? "";
            cmd.Parameters.Add("@RaceSpell", SqlDbType.VarChar).Value = obj.RaceSpell ?? "";
            cmd.Parameters.Add("@GM", SqlDbType.Bit).Value = obj.GameMaster;
            cmd.Parameters.Add("@AH", SqlDbType.Bit).Value = obj.ArenaHost;
            cmd.Parameters.Add("@DEV", SqlDbType.Bit).Value = obj.Developer;
            cmd.Parameters.Add("@Ranger", SqlDbType.Bit).Value = obj.Ranger;
            cmd.Parameters.Add("@Knight", SqlDbType.Bit).Value = obj.Knight;

            #endregion

            cmd.ExecuteNonQuery();

            #region Parameters

            cmd2.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)obj.Serial;
            cmd2.Parameters.Add("@TutComplete", SqlDbType.Bit).Value = obj.QuestManager.TutorialCompleted;
            cmd2.Parameters.Add("@BetaReset", SqlDbType.Bit).Value = obj.QuestManager.BetaReset;
            cmd2.Parameters.Add("@StoneSmith", SqlDbType.Int).Value = obj.QuestManager.StoneSmithing;
            cmd2.Parameters.Add("@MilethRep", SqlDbType.Int).Value = obj.QuestManager.MilethReputation;
            cmd2.Parameters.Add("@ArtursGift", SqlDbType.Int).Value = obj.QuestManager.ArtursGift;
            cmd2.Parameters.Add("@CamilleGreeting", SqlDbType.Bit).Value = obj.QuestManager.CamilleGreetingComplete;
            cmd2.Parameters.Add("@ConnPotions", SqlDbType.Bit).Value = obj.QuestManager.ConnPotions;
            cmd2.Parameters.Add("@CryptTerror", SqlDbType.Bit).Value = obj.QuestManager.CryptTerror;
            cmd2.Parameters.Add("@CryptTerrorSlayed", SqlDbType.Bit).Value = obj.QuestManager.CryptTerrorSlayed;
            cmd2.Parameters.Add("@Dar", SqlDbType.Int).Value = obj.QuestManager.Dar;
            cmd2.Parameters.Add("@DarItem", SqlDbType.VarChar).Value = obj.QuestManager.DarItem ?? "";
            cmd2.Parameters.Add("@EternalLove", SqlDbType.Bit).Value = obj.QuestManager.EternalLove;
            cmd2.Parameters.Add("@Fiona", SqlDbType.Bit).Value = obj.QuestManager.FionaDance;
            cmd2.Parameters.Add("@Keela", SqlDbType.Int).Value = obj.QuestManager.Keela;
            cmd2.Parameters.Add("@KeelaCount", SqlDbType.Int).Value = obj.QuestManager.KeelaCount;
            cmd2.Parameters.Add("@KeelaKill", SqlDbType.VarChar).Value = obj.QuestManager.KeelaKill ?? "";
            cmd2.Parameters.Add("@KeelaQuesting", SqlDbType.Bit).Value = obj.QuestManager.KeelaQuesting;
            cmd2.Parameters.Add("@KillerBee", SqlDbType.Bit).Value = obj.QuestManager.KillerBee;
            cmd2.Parameters.Add("@Neal", SqlDbType.Int).Value = obj.QuestManager.Neal;
            cmd2.Parameters.Add("@NealCount", SqlDbType.Int).Value = obj.QuestManager.NealCount;
            cmd2.Parameters.Add("@NealKill", SqlDbType.VarChar).Value = obj.QuestManager.NealKill ?? "";
            cmd2.Parameters.Add("@AbelShopAccess", SqlDbType.Bit).Value = obj.QuestManager.AbelShopAccess;
            cmd2.Parameters.Add("@PeteKill", SqlDbType.Int).Value = obj.QuestManager.PeteKill;
            cmd2.Parameters.Add("@PeteComplete", SqlDbType.Bit).Value = obj.QuestManager.PeteComplete;
            cmd2.Parameters.Add("@SwampAccess", SqlDbType.Bit).Value = obj.QuestManager.SwampAccess;
            cmd2.Parameters.Add("@SwampCount", SqlDbType.Int).Value = obj.QuestManager.SwampCount;
            cmd2.Parameters.Add("@TagorDungeonAccess", SqlDbType.Bit).Value = obj.QuestManager.TagorDungeonAccess;

            #endregion

            ExecuteAndCloseConnection(cmd2, connection);
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your character did not save. Contact GM (Code: Long)");
                Crashes.TrackError(e);
                return false;
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
            SaveLock.Release();
        }

        return true;
    }

    public void SaveSkills(Aisling obj, SqlConnection connection)
    {
        if (obj?.SkillBook == null) return;

        try
        {
            foreach (var skill in obj.SkillBook.Skills.Values.Where(i => i is { SkillName: not null }))
            {
                var cmd = ConnectToDatabaseSqlCommandWithProcedure("PlayerSaveSkills", connection);
                cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)obj.Serial;
                cmd.Parameters.Add("@Level", SqlDbType.Int).Value = skill.Level;
                cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = skill.Slot;
                cmd.Parameters.Add("@Skill", SqlDbType.VarChar).Value = skill.SkillName;
                cmd.Parameters.Add("@Uses", SqlDbType.Int).Value = skill.Uses;
                cmd.Parameters.Add("@Cooldown", SqlDbType.Int).Value = skill.CurrentCooldown;
                cmd.ExecuteNonQuery();
            }
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your skills did not save. Contact GM (Code: Slash)");
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

    public void SaveSpells(Aisling obj, SqlConnection connection)
    {
        if (obj?.SpellBook == null) return;

        try
        {
            foreach (var skill in obj.SpellBook.Spells.Values.Where(i => i is { SpellName: not null }))
            {
                var cmd = ConnectToDatabaseSqlCommandWithProcedure("PlayerSaveSpells", connection);
                cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)obj.Serial;
                cmd.Parameters.Add("@Level", SqlDbType.Int).Value = skill.Level;
                cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = skill.Slot;
                cmd.Parameters.Add("@Spell", SqlDbType.VarChar).Value = skill.SpellName;
                cmd.Parameters.Add("@Casts", SqlDbType.Int).Value = skill.Casts;
                cmd.Parameters.Add("@Cooldown", SqlDbType.Int).Value = skill.CurrentCooldown;
                cmd.ExecuteNonQuery();
            }
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your spells did not save. Contact GM (Code: Blast)");
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

    public void SaveBuffs(Aisling aisling, SqlConnection connection)
    {
        if (aisling.Buffs.IsEmpty) return;

        try
        {
            foreach (var buff in aisling.Buffs.Values.Where(i => i is { Name: not null }))
            {
                var cmd = ConnectToDatabaseSqlCommandWithProcedure("BuffSave", connection);
                cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)aisling.Serial;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = buff.Name;
                cmd.Parameters.Add("@TimeLeft", SqlDbType.Int).Value = buff.TimeLeft;
                cmd.ExecuteNonQuery();
            }
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

    public void SaveDebuffs(Aisling aisling, SqlConnection connection)
    {
        if (aisling.Debuffs.IsEmpty) return;

        try
        {
            foreach (var deBuff in aisling.Debuffs.Values.Where(i => i is { Name: not null }))
            {
                var cmd = ConnectToDatabaseSqlCommandWithProcedure("DeBuffSave", connection);
                cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)aisling.Serial;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = deBuff.Name;
                cmd.Parameters.Add("@TimeLeft", SqlDbType.Int).Value = deBuff.TimeLeft;
                cmd.ExecuteNonQuery();
            }
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

    public async Task<bool> SaveItemsForPlayer(Aisling obj, SqlConnection connection)
    {
        if (obj?.Inventory == null) return false;
        var itemList = obj.Inventory.Items.Values.Where(i => i is not null).ToList();
        itemList.AddRange(from item in obj.EquipmentManager.Equipment.Values.Where(i => i is not null) where item.Item != null select item.Item);
        itemList.AddRange(obj.BankManager.Items.Values.Where(i => i is not null));
        
        try
        {
            foreach (var item in itemList.Where(i => i is not null))
            {
                var updateIfExists = await CheckIfItemExists(item.ItemId);
                var cmd = ConnectToDatabaseSqlCommandWithProcedure(updateIfExists ? "ItemUpdate" : "ItemInsert", connection);
                var pane = ItemEnumConverters.PaneToString(item.ItemPane);
                var color = ItemColors.ItemColorsToInt(item.Template.Color);
                var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
                var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
                var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
                var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);

                cmd.Parameters.Add("@ItemId", SqlDbType.BigInt).Value = (long)item.ItemId;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = item.Template.Name;
                cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)obj.Serial;
                cmd.Parameters.Add("@ItemPane", SqlDbType.VarChar).Value = pane;
                cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = item.Slot;
                cmd.Parameters.Add("@InventorySlot", SqlDbType.Int).Value = item.InventorySlot;
                cmd.Parameters.Add("@Color", SqlDbType.Int).Value = color;
                cmd.Parameters.Add("@Cursed", SqlDbType.Bit).Value = item.Cursed;
                cmd.Parameters.Add("@Durability", SqlDbType.Int).Value = item.Durability;
                cmd.Parameters.Add("@Identified", SqlDbType.Bit).Value = item.Identified;
                cmd.Parameters.Add("@ItemVariance", SqlDbType.VarChar).Value = itemVariance;
                cmd.Parameters.Add("@WeapVariance", SqlDbType.VarChar).Value = weapVariance;
                cmd.Parameters.Add("@ItemQuality", SqlDbType.VarChar).Value = quality;
                cmd.Parameters.Add("@OriginalQuality", SqlDbType.VarChar).Value = orgQuality;
                cmd.Parameters.Add("@Stacks", SqlDbType.Int).Value = item.Stacks;
                cmd.Parameters.Add("@Enchantable", SqlDbType.Bit).Value = item.Enchantable;
                cmd.Parameters.Add("@Tarnished", SqlDbType.Bit).Value = item.Tarnished;
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Item did not save correctly. Contact GM (Code: Lost Dwarf)");
                Crashes.TrackError(e);
            }

            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
            return false;
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
            return false;
        }

        return true;
    }

    public async Task<bool> CheckIfPlayerExists(string name)
    {
        try
        {
            var sConn = ConnectToDatabase(ConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("CheckIfPlayerExists", sConn);
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = name;
            var reader = await cmd.ExecuteReaderAsync();
            var userFound = false;

            while (reader.Read())
            {
                var userName = reader["Username"].ToString();
                if (!string.Equals(userName, name, StringComparison.CurrentCultureIgnoreCase)) continue;
                if (string.Equals(name, userName, StringComparison.CurrentCultureIgnoreCase))
                {
                    userFound = true;
                }
            }

            reader.Close();
            sConn.Close();
            return userFound;
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

        return false;
    }

    public async Task<bool> CheckIfPlayerExists(string name, long serial)
    {
        try
        {
            var sConn = ConnectToDatabase(ConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("CheckIfPlayerHashExists", sConn);
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = name;
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = serial;
            var reader = await cmd.ExecuteReaderAsync();
            var userFound = false;

            while (reader.Read())
            {
                var userName = reader["Username"].ToString();
                if (!string.Equals(userName, name, StringComparison.CurrentCultureIgnoreCase)) continue;
                if (string.Equals(name, userName, StringComparison.CurrentCultureIgnoreCase))
                {
                    userFound = true;
                }
            }

            reader.Close();
            sConn.Close();
            return userFound;
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

        return false;
    }

    public async Task<Aisling> CheckPassword(string name)
    {
        var aisling = new Aisling();

        try
        {
            var continueLoad = await CheckIfPlayerExists(name);
            if (!continueLoad) return null;

            var sConn = ConnectToDatabase(EncryptedConnectionString);
            var values = new { Name = name };
            aisling = await sConn.QueryFirstAsync<Aisling>("[PlayerSecurity]", values, commandType: CommandType.StoredProcedure);
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

        return aisling;
    }

    public async Task<bool> CheckIfItemExists(long itemSerial)
    {
        try
        {
            var sConn = ConnectToDatabase(ConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("CheckIfItemExists", sConn);
            cmd.Parameters.Add("@ItemId", SqlDbType.BigInt).Value = itemSerial;
            var reader = await cmd.ExecuteReaderAsync();
            var itemFound = false;

            while (reader.Read())
            {
                var dbId = (long)reader["ItemId"];
                if (itemSerial != dbId) continue;
                itemFound = true;
            }

            reader.Close();
            sConn.Close();
            return itemFound;
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

        return false;
    }

    public async Task Create(Aisling obj)
    {
        await CreateLock.WaitAsync().ConfigureAwait(false);

        var serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;
        var item = EphemeralRandomIdGenerator<uint>.Shared.NextId;

        try
        {
            // Player
            var connection = ConnectToDatabase(EncryptedConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("PlayerCreation", connection);

            #region Parameters

            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)serial;
            cmd.Parameters.Add("@Created", SqlDbType.DateTime).Value = obj.Created;
            cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = obj.Username;
            cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = obj.Password;
            cmd.Parameters.Add("@LastLogged", SqlDbType.DateTime).Value = obj.LastLogged;
            cmd.Parameters.Add("@CurrentHp", SqlDbType.Int).Value = obj.CurrentHp;
            cmd.Parameters.Add("@BaseHp", SqlDbType.Int).Value = obj.BaseHp;
            cmd.Parameters.Add("@CurrentMp", SqlDbType.Int).Value = obj.CurrentMp;
            cmd.Parameters.Add("@BaseMp", SqlDbType.Int).Value = obj.BaseMp;
            cmd.Parameters.Add("@Gender", SqlDbType.VarChar).Value = SpriteMaker.GenderValue(obj.Gender);
            cmd.Parameters.Add("@HairColor", SqlDbType.TinyInt).Value = obj.HairColor;
            cmd.Parameters.Add("@HairStyle", SqlDbType.TinyInt).Value = obj.HairStyle;

            #endregion

            ExecuteAndCloseConnection(cmd, connection);

            var sConn = ConnectToDatabase(ConnectionString);
            var adapter = new SqlDataAdapter();

            #region Adapter Inserts

            // Discovered
            var playerDiscoveredMaps =
                "INSERT INTO ZolianPlayers.dbo.PlayersDiscoveredMaps (Serial, MapId) VALUES " +
                $"('{(long)serial}','{obj.CurrentMapId}')";

            var cmd2 = new SqlCommand(playerDiscoveredMaps, sConn);
            cmd2.CommandTimeout = 5;

            adapter.InsertCommand = cmd2;
            adapter.InsertCommand.ExecuteNonQuery();

            // PlayersSkills
            var playerSkillBook =
                "INSERT INTO ZolianPlayers.dbo.PlayersSkillBook (Serial, Level, Slot, SkillName, Uses, CurrentCooldown) VALUES " +
                $"('{(long)serial}','{0}','{73}','Assail','{0}','{0}')";

            var cmd3 = new SqlCommand(playerSkillBook, sConn);
            cmd3.CommandTimeout = 5;

            adapter.InsertCommand = cmd3;
            adapter.InsertCommand.ExecuteNonQuery();

            // PlayerInventory
            var playerInventory =
                "INSERT INTO ZolianPlayers.dbo.PlayersItems (ItemId, Name, Serial, ItemPane, Slot, InventorySlot, Color, Cursed, Durability, Identified, ItemVariance, WeapVariance, ItemQuality, OriginalQuality, Stacks, Enchantable, Tarnished) VALUES " +
                $"('{(long)item}','Zolian Guide','{(long)serial}','Inventory','{0}','{24}','{0}','False','{0}','True','None','None','Common','Common','{1}','False', 'False')";

            var cmd4 = new SqlCommand(playerInventory, sConn);
            cmd4.CommandTimeout = 5;

            adapter.InsertCommand = cmd4;
            adapter.InsertCommand.ExecuteNonQuery();

            #endregion

            var cmd5 = ConnectToDatabaseSqlCommandWithProcedure("InsertQuests", sConn);

            #region Parameters

            cmd5.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)serial;
            cmd5.Parameters.Add("@TutComplete", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@BetaReset", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@StoneSmith", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@MilethRep", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@ArtursGift", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@CamilleGreeting", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ConnPotions", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@CryptTerror", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@CryptTerrorSlayed", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Dar", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@DarItem", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@EternalLove", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Fiona", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Keela", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@KeelaCount", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@KeelaKill", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@KeelaQuesting", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@KillerBee", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Neal", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@NealCount", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@NealKill", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@AbelShopAccess", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@PeteKill", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@PeteComplete", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@SwampAccess", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@SwampCount", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@TagorDungeonAccess", SqlDbType.Bit).Value = false;

            #endregion

            ExecuteAndCloseConnection(cmd5, sConn);
        }
        catch (SqlException e) when (e.Number == 2627)
        {
            if (e.Message.Contains("PK__Players"))
            {
                obj.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue creating player. Error: Phoenix");
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
}