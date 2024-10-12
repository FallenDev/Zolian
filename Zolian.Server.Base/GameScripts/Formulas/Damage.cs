using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Formulas;

/// <summary>
/// Base damage for Monsters
/// </summary>
[Script("Base Damage")]
public class Damage : DamageFormulaScript
{
    public Damage(Sprite obj, Sprite target, MonsterEnums type) { }

    public override double Calculate(Sprite obj, Sprite target, MonsterEnums type)
    {
        if (target is null) return 0;

        double dmg;
        var diff = (double)(obj.Level - target.Level);

        if (diff <= 0)
            dmg = obj.Level * (type == MonsterEnums.Physical ? 1 : 2) * ServerSetup.Instance.Config.BaseDamageMod;
        else
            dmg = obj.Level * (type == MonsterEnums.Physical ? 1 : 2) * (ServerSetup.Instance.Config.BaseDamageMod * diff);

        if (dmg <= 0)
            dmg = 1;

        return dmg;
    }
}