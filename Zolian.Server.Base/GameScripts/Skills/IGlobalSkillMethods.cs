using Darkages.Interfaces;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills
{
    public interface IGlobalSkillMethods
    {
        int DistanceTo(Position spritePos, Position inputPos);
        void ApplyPhysicalDebuff(IGameClient client, Debuff debuff, Sprite target, Skill skill);
        void ApplyPhysicalBuff(Sprite target, Buff buff);
        Sprite[] GetInCone(Sprite sprite);
        void Step(Sprite sprite, int savedXStep, int savedYStep);
        void Train(IGameClient client, Skill skill);
        bool OnUse(Aisling aisling, Skill skill);
        int Thrown(IGameClient client, Skill skill, bool crit);
        void FailedAttempt(GameClient client, Aisling aisling, Skill skill, ServerFormat1A action);
    }
}
