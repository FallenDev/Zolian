using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.GameScripts.Formulas;

/// <summary>
/// Base damage for Monsters
/// </summary>
[Script("Base Damage")]
public class Damage : DamageFormulaScript
{
    public Damage(Sprite attacker, Sprite defender, MonsterEnums type) { }

    public override double Calculate(Sprite attacker, Sprite defender, MonsterEnums type)
    {
        if (defender is null) return 0;
        double dmg = 0;
        
        switch (attacker)
        {
            case Monster when defender is Aisling damageReceiver:
                {
                    dmg = attacker.Level * (type == MonsterEnums.Physical ? 1 : 2) * ServerSetup.Instance.Config.BaseDamageMod;
                    var diff = (damageReceiver.ExpLevel + damageReceiver.AbpLevel) - attacker.Level;
                    dmg *= diff;
                }
                break;
            case Monster when defender is Monster:
                {
                    dmg = attacker.Level * (type == MonsterEnums.Physical ? 1 : 2) * ServerSetup.Instance.Config.BaseDamageMod;
                    var diff = defender.Level - attacker.Level;
                    dmg *= diff;
                }
                break;
            case Aisling damageDealer when defender is Aisling damageReceiver:
                {
                    dmg = (damageDealer.ExpLevel + damageDealer.AbpLevel) * (type == MonsterEnums.Physical ? 1 : 2) * ServerSetup.Instance.Config.BaseDamageMod;
                    var diff = (damageReceiver.ExpLevel + damageReceiver.AbpLevel) - (damageDealer.ExpLevel + damageDealer.AbpLevel);
                    dmg *= diff;
                }
                break;
            case Aisling damageDealer when defender is Monster:
                {
                    dmg = (damageDealer.ExpLevel + damageDealer.AbpLevel) * (type == MonsterEnums.Physical ? 1 : 2) * ServerSetup.Instance.Config.BaseDamageMod;
                    var diff = defender.Level - (damageDealer.ExpLevel + damageDealer.AbpLevel);
                    dmg *= diff;
                }
                break;
        }

        if (dmg <= 0) dmg = 1;

        return dmg;
    }
}