using Darkages.Sprites;
using Darkages.Templates;

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
    /// Save method for spells, skills, items, buffs, debuffs
    /// </summary>
    Task AuxiliarySave(Aisling obj);
    /// <summary>
    /// Save method for an individual player
    /// </summary>
    Task<bool> Save(Aisling obj);
    /// <summary>
    /// Server Save method used to save all players on the server at once
    /// </summary>
    bool ServerSave(List<Aisling> playerList);
    void SaveBuffs(Aisling aisling, SqlConnection connection);
    void SaveDebuffs(Aisling aisling, SqlConnection connection);
    Task<bool> CheckIfPlayerExists(string name);
    Task<bool> CheckIfPlayerExists(string name, long serial);
    Task<Aisling> CheckPassword(string name);
    BoardTemplate ObtainMailboxId(long serial);
    List<PostTemplate> ObtainPosts(ushort boardId);
    void SendPost(PostTemplate postInfo, ushort boardId);
    Task Create(Aisling obj);
}