using Darkages.Types;

namespace Darkages.Interfaces;

public interface IAreaStorage
{
    void CacheFromDatabase();
    bool LoadMap(Area mapObj, string mapFile);
}