using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IDebuff
{
    bool Cancelled { get; set; }
    byte Icon { get; set; }
    int Length { get; set; }
    string Name { get; set; }
    int TimeLeft { get; set; }
    Debuff DebuffSpell { get; set; }

    void OnApplied(Sprite affected, Debuff debuff);
    void OnDurationUpdate(Sprite affected, Debuff debuff);
    void OnEnded(Sprite affected, Debuff debuff);
    void OnItemChange(Aisling affected, Debuff debuff);
    Debuff ObtainDebuffName(Sprite affected, Debuff debuff);
    void Update(Sprite affected, TimeSpan elapsedTime);
    void InsertDebuff(Aisling aisling, Debuff debuff);
    void DeleteDebuff(Aisling aisling, Debuff debuff);
    Task<bool> CheckOnDebuffAsync(IWorldClient client, string name);
}