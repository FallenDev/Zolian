using Darkages.Sprites;
using Microsoft.Data.SqlClient;

namespace Darkages.Interfaces;

public interface IAislingStorage
{
    Task<Aisling> LoadAisling(string name, uint serial);
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
    bool SaveSkills(Aisling obj, SqlConnection connection);
    bool SaveSpells(Aisling obj, SqlConnection connection);
    Task<bool> CheckIfPlayerExists(string name);
    Task<bool> CheckIfPlayerExists(string name, uint serial);
    Task<Aisling> CheckPassword(string name);
    Task Create(Aisling obj);
}