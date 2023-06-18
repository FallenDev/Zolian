using Darkages.Interfaces;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

public interface IGlobalSkillMethods
{
    int DistanceTo(Position spritePos, Position inputPos);
    void ApplyPhysicalDebuff(IGameClient client, Debuff debuff, Sprite target, Skill skill);
    void ApplyPhysicalBuff(Sprite target, Buff buff);
    Sprite[] GetInCone(Sprite sprite);
    void Step(Sprite sprite, int savedXStep, int savedYStep);
    void Train(IGameClient client, Skill skill);
    bool OnUse(Aisling aisling, Skill skill);
    void OnSuccess(Sprite enemy, Sprite attacker, Skill skill, int dmg, bool crit, ServerFormat1A action);
    void OnSuccessWithoutAction(Sprite enemy, Sprite attacker, Skill skill, int dmg, bool crit);
    int Thrown(IGameClient client, Skill skill, bool crit);
    void FailedAttempt(Sprite sprite, Skill skill, ServerFormat1A action);
    (bool, int) OnCrit(int dmg);
}