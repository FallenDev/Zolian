using Darkages.Enums;
using Darkages.Scripting;
using Darkages.Sprites;

namespace Darkages.GameScripts.Formulas;

[Script("Base Damage")]
public class Damage : DamageFormulaScript
{
    private Sprite _obj;
    private Sprite _target;

    public Damage(Sprite obj, Sprite target, MonsterEnums type)
    {
        _obj = obj;
        _target = target;
    }

    public override int Calculate(Sprite obj, Sprite target, MonsterEnums type)
    {
        if (_target is null) return 0;

        double dmg;
        var diff = _target switch
        {
            Aisling aisling => (int)(_obj.Level + 1 - aisling.ExpLevel),
            Monster monster => (int)(_obj.Level + 1 - monster.Template.Level),
            _ => 0
        };

        if (diff <= 0)
            dmg = _obj.Level * (type == MonsterEnums.Physical ? 1 : 2) * ServerSetup.Instance.Config.BaseDamageMod;
        else
            dmg = _obj.Level * (type == MonsterEnums.Physical ? 1 : 2) * (ServerSetup.Instance.Config.BaseDamageMod * diff);

        if (dmg <= 0)
            dmg = 1;

        return (int)dmg;
    }
}