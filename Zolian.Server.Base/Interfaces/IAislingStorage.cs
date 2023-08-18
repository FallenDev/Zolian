using Darkages.Sprites;
using Microsoft.Data.SqlClient;

namespace Darkages.Interfaces;

public interface IAislingStorage
{
    Task<Aisling> LoadAisling(string name, long serial);
    /// <summary>
    /// Save method for password attempts & password change
    /// </summary>
    Task<bool> PasswordSave(Aisling obj);
    /// <summary>
    /// Save method for properties that change often
    /// </summary>
    Task QuickSave(Aisling obj);
    /// <summary>
    /// Save method used to store properties that rarely change
    /// </summary>
    Task<bool> Save(Aisling obj);
    void SaveSkills(Aisling obj, SqlConnection connection);
    void SaveSpells(Aisling obj, SqlConnection connection);
    void SaveBuffs(Aisling aisling, SqlConnection connection);
    void SaveDebuffs(Aisling aisling, SqlConnection connection);
    Task<bool> SaveItemsForPlayer(Aisling obj, SqlConnection connection);
    Task<bool> CheckIfPlayerExists(string name);
    Task<bool> CheckIfPlayerExists(string name, long serial);
    Task<Aisling> CheckPassword(string name);
    Task<bool> CheckIfItemExists(long itemSerial);
    Task Create(Aisling obj);
}