﻿using Chaos.Networking.Entities.Server;

using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

public interface IGlobalSkillMethods
{
    int DistanceTo(Position spritePos, Position inputPos);
    void ApplyPhysicalDebuff(WorldClient client, Debuff debuff, Sprite target, Skill skill);
    void ApplyPhysicalBuff(Sprite target, Buff buff);
    Sprite[] GetInCone(Sprite sprite);
    void Step(Sprite sprite, int savedXStep, int savedYStep);
    void Train(WorldClient client, Skill skill);
    bool OnUse(Aisling aisling, Skill skill);
    void OnSuccess(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit, BodyAnimationArgs action);
    void OnSuccessWithoutAction(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit);
    void OnSuccessWithoutActionAnimation(Sprite enemy, Sprite attacker, Skill skill, long dmg, bool crit);
    int Thrown(WorldClient client, Skill skill, bool crit);
    void FailedAttempt(Sprite sprite, Skill skill, BodyAnimationArgs action);
    (bool, long) OnCrit(long dmg);
}