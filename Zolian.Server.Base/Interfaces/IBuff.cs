using Darkages.Infrastructure;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IBuff
{
    ushort Animation { get; set; }
    bool Cancelled { get; set; }
    byte Icon { get; set; }
    int Length { get; set; }
    string Name { get; set; }
    int TimeLeft { get; set; }
    GameServerTimer Timer { get; set; }
    Buff BuffSpell { get; set; }

    void OnApplied(Sprite affected, Buff buff);
    void OnDurationUpdate(Sprite affected, Buff buff);
    void OnEnded(Sprite affected, Buff buff);
    Buff ObtainBuffName(Sprite affected, Buff buff);
    void Update(Sprite affected, TimeSpan elapsedTime);
    void InsertBuff(Aisling aisling, Buff buff);
    void UpdateBuff(Aisling aisling);
    void DeleteBuff(Aisling aisling, Buff buff);
    Task<bool> CheckOnBuffAsync(IGameClient client, string name);
}