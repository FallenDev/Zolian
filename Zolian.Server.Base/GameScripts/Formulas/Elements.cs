using Darkages.Enums;
using Darkages.ScriptingBase;
using Darkages.Sprites;

namespace Darkages.GameScripts.Formulas;

[Script("Elements 4.0")]
public class Elements : ElementFormulaScript
{
    public Elements(Sprite obj) { }

    public override double Calculate(Sprite obj, ElementManager.Element offenseElement)
    {
        var defenseElement = obj.DefenseElement;

        if (defenseElement == ElementManager.Element.None && offenseElement == ElementManager.Element.None)
        {
            return 1;
        }

        return obj.Amplified switch
        {
            0 => defenseElement switch
            {
                ElementManager.Element.None when offenseElement != ElementManager.Element.None => 2.00,
                ElementManager.Element.Fire => offenseElement switch
                {
                    ElementManager.Element.None => 0.58,
                    ElementManager.Element.Fire => 0.58,
                    ElementManager.Element.Wind => 0.66,
                    ElementManager.Element.Earth => 0.85,
                    ElementManager.Element.Water => 1.75,
                    ElementManager.Element.Holy => 0.75,
                    ElementManager.Element.Void => 1.25,
                    ElementManager.Element.Rage => 1.25,
                    ElementManager.Element.Sorrow => 1.25,
                    ElementManager.Element.Terror => 1.25,
                    _ => 0.58
                },
                ElementManager.Element.Wind => offenseElement switch
                {
                    ElementManager.Element.None => 0.58,
                    ElementManager.Element.Fire => 1.75,
                    ElementManager.Element.Wind => 0.58,
                    ElementManager.Element.Earth => 0.66,
                    ElementManager.Element.Water => 0.85,
                    ElementManager.Element.Holy => 1.25,
                    ElementManager.Element.Void => 0.75,
                    ElementManager.Element.Rage => 1.25,
                    ElementManager.Element.Sorrow => 1.25,
                    ElementManager.Element.Terror => 1.25,
                    _ => 0.58
                },
                ElementManager.Element.Earth => offenseElement switch
                {
                    ElementManager.Element.None => 0.58,
                    ElementManager.Element.Fire => 0.85,
                    ElementManager.Element.Wind => 1.75,
                    ElementManager.Element.Earth => 0.58,
                    ElementManager.Element.Water => 0.66,
                    ElementManager.Element.Holy => 0.75,
                    ElementManager.Element.Void => 1.25,
                    ElementManager.Element.Rage => 1.25,
                    ElementManager.Element.Sorrow => 1.25,
                    ElementManager.Element.Terror => 1.25,
                    _ => 0.58
                },
                ElementManager.Element.Water => offenseElement switch
                {
                    ElementManager.Element.None => 0.58,
                    ElementManager.Element.Fire => 0.66,
                    ElementManager.Element.Wind => 0.85,
                    ElementManager.Element.Earth => 1.75,
                    ElementManager.Element.Water => 0.58,
                    ElementManager.Element.Holy => 1.25,
                    ElementManager.Element.Void => 0.75,
                    ElementManager.Element.Rage => 1.25,
                    ElementManager.Element.Sorrow => 1.25,
                    ElementManager.Element.Terror => 1.25,
                    _ => 0.58
                },
                ElementManager.Element.Holy => offenseElement switch
                {
                    ElementManager.Element.None => 0.58,
                    ElementManager.Element.Fire => 1.25,
                    ElementManager.Element.Wind => 0.75,
                    ElementManager.Element.Earth => 1.25,
                    ElementManager.Element.Water => 0.75,
                    ElementManager.Element.Holy => 0.50,
                    ElementManager.Element.Void => 1.75,
                    ElementManager.Element.Rage => 1.00,
                    ElementManager.Element.Sorrow => 0.75,
                    ElementManager.Element.Terror => 1.00,
                    _ => 0.58
                },
                ElementManager.Element.Void => offenseElement switch
                {
                    ElementManager.Element.None => 0.58,
                    ElementManager.Element.Fire => 0.75,
                    ElementManager.Element.Wind => 1.25,
                    ElementManager.Element.Earth => 0.75,
                    ElementManager.Element.Water => 1.25,
                    ElementManager.Element.Holy => 1.75,
                    ElementManager.Element.Void => 0.50,
                    ElementManager.Element.Rage => 1.00,
                    ElementManager.Element.Sorrow => 1.00,
                    ElementManager.Element.Terror => 0.75,
                    _ => 0.58
                },
                ElementManager.Element.Rage => offenseElement switch
                {
                    ElementManager.Element.None => 0.24,
                    ElementManager.Element.Fire => 0.60,
                    ElementManager.Element.Wind => 0.50,
                    ElementManager.Element.Earth => 0.60,
                    ElementManager.Element.Water => 0.50,
                    ElementManager.Element.Holy => 1.20,
                    ElementManager.Element.Void => 0.60,
                    ElementManager.Element.Rage => 0.25,
                    ElementManager.Element.Sorrow => 1.50,
                    ElementManager.Element.Terror => 0.75,
                    _ => 0.24
                },
                ElementManager.Element.Sorrow => offenseElement switch
                {
                    ElementManager.Element.None => 0.24,
                    ElementManager.Element.Fire => 0.50,
                    ElementManager.Element.Wind => 0.60,
                    ElementManager.Element.Earth => 0.50,
                    ElementManager.Element.Water => 0.60,
                    ElementManager.Element.Holy => 0.60,
                    ElementManager.Element.Void => 1.20,
                    ElementManager.Element.Rage => 0.75,
                    ElementManager.Element.Sorrow => 0.25,
                    ElementManager.Element.Terror => 1.50,
                    _ => 0.24
                },
                ElementManager.Element.Terror => offenseElement switch
                {
                    ElementManager.Element.None => 0.24,
                    ElementManager.Element.Fire => 0.40,
                    ElementManager.Element.Wind => 0.40,
                    ElementManager.Element.Earth => 0.40,
                    ElementManager.Element.Water => 0.40,
                    ElementManager.Element.Holy => 0.80,
                    ElementManager.Element.Void => 0.80,
                    ElementManager.Element.Rage => 0.90,
                    ElementManager.Element.Sorrow => 0.90,
                    ElementManager.Element.Terror => 0.25,
                    _ => 0.24
                },
                _ => 2.00
            },
            1.5 => FasNadur(obj, offenseElement, defenseElement),
            2 => MorFasNadur(obj, offenseElement, defenseElement),
            2.5 => ArdFasNadur(obj, offenseElement, defenseElement),
            _ => 0
        };
    }

    public override double FasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement)
    {
        return defenseElement switch
        {
            ElementManager.Element.None when offenseElement != ElementManager.Element.None => 3.00,
            ElementManager.Element.Fire => offenseElement switch
            {
                ElementManager.Element.None => 0.87,
                ElementManager.Element.Fire => 0.87,
                ElementManager.Element.Wind => 0.99,
                ElementManager.Element.Earth => 1.28,
                ElementManager.Element.Water => 2.63,
                ElementManager.Element.Holy => 1.13,
                ElementManager.Element.Void => 1.88,
                ElementManager.Element.Rage => 1.88,
                ElementManager.Element.Sorrow => 1.88,
                ElementManager.Element.Terror => 1.88,
                _ => 0.87
            },
            ElementManager.Element.Wind => offenseElement switch
            {
                ElementManager.Element.None => 0.87,
                ElementManager.Element.Fire => 2.63,
                ElementManager.Element.Wind => 0.87,
                ElementManager.Element.Earth => 0.99,
                ElementManager.Element.Water => 1.28,
                ElementManager.Element.Holy => 1.88,
                ElementManager.Element.Void => 1.13,
                ElementManager.Element.Rage => 1.88,
                ElementManager.Element.Sorrow => 1.88,
                ElementManager.Element.Terror => 1.88,
                _ => 0.87
            },
            ElementManager.Element.Earth => offenseElement switch
            {
                ElementManager.Element.None => 0.87,
                ElementManager.Element.Fire => 1.28,
                ElementManager.Element.Wind => 2.63,
                ElementManager.Element.Earth => 0.87,
                ElementManager.Element.Water => 0.99,
                ElementManager.Element.Holy => 1.13,
                ElementManager.Element.Void => 1.88,
                ElementManager.Element.Rage => 1.88,
                ElementManager.Element.Sorrow => 1.88,
                ElementManager.Element.Terror => 1.88,
                _ => 0.87
            },
            ElementManager.Element.Water => offenseElement switch
            {
                ElementManager.Element.None => 0.87,
                ElementManager.Element.Fire => 0.99,
                ElementManager.Element.Wind => 1.28,
                ElementManager.Element.Earth => 2.63,
                ElementManager.Element.Water => 0.87,
                ElementManager.Element.Holy => 1.88,
                ElementManager.Element.Void => 1.13,
                ElementManager.Element.Rage => 1.88,
                ElementManager.Element.Sorrow => 1.88,
                ElementManager.Element.Terror => 1.88,
                _ => 0.87
            },
            ElementManager.Element.Holy => offenseElement switch
            {
                ElementManager.Element.None => 0.87,
                ElementManager.Element.Fire => 1.88,
                ElementManager.Element.Wind => 1.13,
                ElementManager.Element.Earth => 1.88,
                ElementManager.Element.Water => 1.13,
                ElementManager.Element.Holy => 0.75,
                ElementManager.Element.Void => 2.63,
                ElementManager.Element.Rage => 1.50,
                ElementManager.Element.Sorrow => 1.13,
                ElementManager.Element.Terror => 1.50,
                _ => 0.87
            },
            ElementManager.Element.Void => offenseElement switch
            {
                ElementManager.Element.None => 0.87,
                ElementManager.Element.Fire => 1.13,
                ElementManager.Element.Wind => 1.88,
                ElementManager.Element.Earth => 1.13,
                ElementManager.Element.Water => 1.88,
                ElementManager.Element.Holy => 2.63,
                ElementManager.Element.Void => 0.75,
                ElementManager.Element.Rage => 1.50,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 1.13,
                _ => 0.87
            },
            ElementManager.Element.Rage => offenseElement switch
            {
                ElementManager.Element.None => 0.36,
                ElementManager.Element.Fire => 0.90,
                ElementManager.Element.Wind => 0.75,
                ElementManager.Element.Earth => 0.90,
                ElementManager.Element.Water => 0.75,
                ElementManager.Element.Holy => 1.80,
                ElementManager.Element.Void => 0.90,
                ElementManager.Element.Rage => 0.38,
                ElementManager.Element.Sorrow => 2.25,
                ElementManager.Element.Terror => 1.13,
                _ => 0.36
            },
            ElementManager.Element.Sorrow => offenseElement switch
            {
                ElementManager.Element.None => 0.36,
                ElementManager.Element.Fire => 0.75,
                ElementManager.Element.Wind => 0.90,
                ElementManager.Element.Earth => 0.75,
                ElementManager.Element.Water => 0.90,
                ElementManager.Element.Holy => 0.90,
                ElementManager.Element.Void => 1.80,
                ElementManager.Element.Rage => 1.13,
                ElementManager.Element.Sorrow => 0.38,
                ElementManager.Element.Terror => 2.25,
                _ => 0.36
            },
            ElementManager.Element.Terror => offenseElement switch
            {
                ElementManager.Element.None => 0.36,
                ElementManager.Element.Fire => 0.60,
                ElementManager.Element.Wind => 0.60,
                ElementManager.Element.Earth => 0.60,
                ElementManager.Element.Water => 0.60,
                ElementManager.Element.Holy => 1.20,
                ElementManager.Element.Void => 1.20,
                ElementManager.Element.Rage => 1.35,
                ElementManager.Element.Sorrow => 1.35,
                ElementManager.Element.Terror => 0.38,
                _ => 0.36
            },
            _ => 3.00
        };
    }

    public override double MorFasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement)
    {
        return defenseElement switch
        {
            ElementManager.Element.None when offenseElement != ElementManager.Element.None => 4.00,
            ElementManager.Element.Fire => offenseElement switch
            {
                ElementManager.Element.None => 1.16,
                ElementManager.Element.Fire => 1.16,
                ElementManager.Element.Wind => 1.32,
                ElementManager.Element.Earth => 1.70,
                ElementManager.Element.Water => 3.50,
                ElementManager.Element.Holy => 1.50,
                ElementManager.Element.Void => 2.50,
                ElementManager.Element.Rage => 2.50,
                ElementManager.Element.Sorrow => 2.50,
                ElementManager.Element.Terror => 2.50,
                _ => 1.16
            },
            ElementManager.Element.Wind => offenseElement switch
            {
                ElementManager.Element.None => 1.16,
                ElementManager.Element.Fire => 3.50,
                ElementManager.Element.Wind => 1.16,
                ElementManager.Element.Earth => 1.32,
                ElementManager.Element.Water => 1.70,
                ElementManager.Element.Holy => 2.50,
                ElementManager.Element.Void => 1.50,
                ElementManager.Element.Rage => 2.50,
                ElementManager.Element.Sorrow => 2.50,
                ElementManager.Element.Terror => 2.50,
                _ => 1.16
            },
            ElementManager.Element.Earth => offenseElement switch
            {
                ElementManager.Element.None => 1.16,
                ElementManager.Element.Fire => 1.70,
                ElementManager.Element.Wind => 3.50,
                ElementManager.Element.Earth => 1.16,
                ElementManager.Element.Water => 1.32,
                ElementManager.Element.Holy => 1.50,
                ElementManager.Element.Void => 2.50,
                ElementManager.Element.Rage => 2.50,
                ElementManager.Element.Sorrow => 2.50,
                ElementManager.Element.Terror => 2.50,
                _ => 1.16
            },
            ElementManager.Element.Water => offenseElement switch
            {
                ElementManager.Element.None => 1.16,
                ElementManager.Element.Fire => 1.32,
                ElementManager.Element.Wind => 1.70,
                ElementManager.Element.Earth => 3.50,
                ElementManager.Element.Water => 1.16,
                ElementManager.Element.Holy => 2.50,
                ElementManager.Element.Void => 1.50,
                ElementManager.Element.Rage => 2.50,
                ElementManager.Element.Sorrow => 2.50,
                ElementManager.Element.Terror => 2.50,
                _ => 1.16
            },
            ElementManager.Element.Holy => offenseElement switch
            {
                ElementManager.Element.None => 1.16,
                ElementManager.Element.Fire => 2.50,
                ElementManager.Element.Wind => 1.50,
                ElementManager.Element.Earth => 2.50,
                ElementManager.Element.Water => 1.50,
                ElementManager.Element.Holy => 1.00,
                ElementManager.Element.Void => 3.50,
                ElementManager.Element.Rage => 2.00,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 2.00,
                _ => 1.16
            },
            ElementManager.Element.Void => offenseElement switch
            {
                ElementManager.Element.None => 1.16,
                ElementManager.Element.Fire => 1.50,
                ElementManager.Element.Wind => 2.50,
                ElementManager.Element.Earth => 1.50,
                ElementManager.Element.Water => 2.50,
                ElementManager.Element.Holy => 3.50,
                ElementManager.Element.Void => 1.00,
                ElementManager.Element.Rage => 2.00,
                ElementManager.Element.Sorrow => 2.00,
                ElementManager.Element.Terror => 1.50,
                _ => 1.16
            },
            ElementManager.Element.Rage => offenseElement switch
            {
                ElementManager.Element.None => 0.48,
                ElementManager.Element.Fire => 1.20,
                ElementManager.Element.Wind => 1.00,
                ElementManager.Element.Earth => 1.20,
                ElementManager.Element.Water => 1.00,
                ElementManager.Element.Holy => 2.40,
                ElementManager.Element.Void => 1.20,
                ElementManager.Element.Rage => 0.50,
                ElementManager.Element.Sorrow => 3.00,
                ElementManager.Element.Terror => 1.50,
                _ => 0.48
            },
            ElementManager.Element.Sorrow => offenseElement switch
            {
                ElementManager.Element.None => 0.48,
                ElementManager.Element.Fire => 1.00,
                ElementManager.Element.Wind => 1.20,
                ElementManager.Element.Earth => 1.00,
                ElementManager.Element.Water => 1.20,
                ElementManager.Element.Holy => 1.20,
                ElementManager.Element.Void => 2.40,
                ElementManager.Element.Rage => 1.50,
                ElementManager.Element.Sorrow => 0.50,
                ElementManager.Element.Terror => 3.00,
                _ => 0.48
            },
            ElementManager.Element.Terror => offenseElement switch
            {
                ElementManager.Element.None => 0.48,
                ElementManager.Element.Fire => 0.80,
                ElementManager.Element.Wind => 0.80,
                ElementManager.Element.Earth => 0.80,
                ElementManager.Element.Water => 0.80,
                ElementManager.Element.Holy => 1.60,
                ElementManager.Element.Void => 1.60,
                ElementManager.Element.Rage => 1.80,
                ElementManager.Element.Sorrow => 1.80,
                ElementManager.Element.Terror => 0.50,
                _ => 0.48
            },
            _ => 4.00
        };
    }

    public override double ArdFasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement)
    {
        return defenseElement switch
        {
            ElementManager.Element.None when offenseElement != ElementManager.Element.None => 5.00,
            ElementManager.Element.Fire => offenseElement switch
            {
                ElementManager.Element.None => 1.45,
                ElementManager.Element.Fire => 1.45,
                ElementManager.Element.Wind => 1.65,
                ElementManager.Element.Earth => 2.13,
                ElementManager.Element.Water => 4.38,
                ElementManager.Element.Holy => 1.88,
                ElementManager.Element.Void => 3.13,
                ElementManager.Element.Rage => 3.13,
                ElementManager.Element.Sorrow => 3.13,
                ElementManager.Element.Terror => 3.13,
                _ => 1.45
            },
            ElementManager.Element.Wind => offenseElement switch
            {
                ElementManager.Element.None => 1.45,
                ElementManager.Element.Fire => 4.38,
                ElementManager.Element.Wind => 1.45,
                ElementManager.Element.Earth => 1.65,
                ElementManager.Element.Water => 2.13,
                ElementManager.Element.Holy => 3.13,
                ElementManager.Element.Void => 1.88,
                ElementManager.Element.Rage => 3.13,
                ElementManager.Element.Sorrow => 3.13,
                ElementManager.Element.Terror => 3.13,
                _ => 1.45
            },
            ElementManager.Element.Earth => offenseElement switch
            {
                ElementManager.Element.None => 1.45,
                ElementManager.Element.Fire => 2.13,
                ElementManager.Element.Wind => 4.38,
                ElementManager.Element.Earth => 1.45,
                ElementManager.Element.Water => 1.65,
                ElementManager.Element.Holy => 1.88,
                ElementManager.Element.Void => 3.13,
                ElementManager.Element.Rage => 3.13,
                ElementManager.Element.Sorrow => 3.13,
                ElementManager.Element.Terror => 3.13,
                _ => 1.45
            },
            ElementManager.Element.Water => offenseElement switch
            {
                ElementManager.Element.None => 1.45,
                ElementManager.Element.Fire => 1.65,
                ElementManager.Element.Wind => 2.13,
                ElementManager.Element.Earth => 4.38,
                ElementManager.Element.Water => 1.45,
                ElementManager.Element.Holy => 3.13,
                ElementManager.Element.Void => 1.88,
                ElementManager.Element.Rage => 3.13,
                ElementManager.Element.Sorrow => 3.13,
                ElementManager.Element.Terror => 3.13,
                _ => 1.45
            },
            ElementManager.Element.Holy => offenseElement switch
            {
                ElementManager.Element.None => 1.45,
                ElementManager.Element.Fire => 3.13,
                ElementManager.Element.Wind => 1.88,
                ElementManager.Element.Earth => 3.13,
                ElementManager.Element.Water => 1.88,
                ElementManager.Element.Holy => 1.25,
                ElementManager.Element.Void => 4.38,
                ElementManager.Element.Rage => 2.50,
                ElementManager.Element.Sorrow => 1.88,
                ElementManager.Element.Terror => 2.50,
                _ => 1.45
            },
            ElementManager.Element.Void => offenseElement switch
            {
                ElementManager.Element.None => 1.45,
                ElementManager.Element.Fire => 1.88,
                ElementManager.Element.Wind => 3.13,
                ElementManager.Element.Earth => 1.88,
                ElementManager.Element.Water => 3.13,
                ElementManager.Element.Holy => 4.38,
                ElementManager.Element.Void => 1.25,
                ElementManager.Element.Rage => 2.50,
                ElementManager.Element.Sorrow => 2.50,
                ElementManager.Element.Terror => 1.88,
                _ => 1.45
            },
            ElementManager.Element.Rage => offenseElement switch
            {
                ElementManager.Element.None => 0.60,
                ElementManager.Element.Fire => 1.50,
                ElementManager.Element.Wind => 1.25,
                ElementManager.Element.Earth => 1.50,
                ElementManager.Element.Water => 1.25,
                ElementManager.Element.Holy => 3.00,
                ElementManager.Element.Void => 1.50,
                ElementManager.Element.Rage => 0.63,
                ElementManager.Element.Sorrow => 3.75,
                ElementManager.Element.Terror => 1.88,
                _ => 0.60
            },
            ElementManager.Element.Sorrow => offenseElement switch
            {
                ElementManager.Element.None => 0.60,
                ElementManager.Element.Fire => 1.25,
                ElementManager.Element.Wind => 1.50,
                ElementManager.Element.Earth => 1.25,
                ElementManager.Element.Water => 1.50,
                ElementManager.Element.Holy => 1.50,
                ElementManager.Element.Void => 3.00,
                ElementManager.Element.Rage => 1.88,
                ElementManager.Element.Sorrow => 0.63,
                ElementManager.Element.Terror => 3.75,
                _ => 0.60
            },
            ElementManager.Element.Terror => offenseElement switch
            {
                ElementManager.Element.None => 0.60,
                ElementManager.Element.Fire => 1.00,
                ElementManager.Element.Wind => 1.00,
                ElementManager.Element.Earth => 1.00,
                ElementManager.Element.Water => 1.00,
                ElementManager.Element.Holy => 2.00,
                ElementManager.Element.Void => 2.00,
                ElementManager.Element.Rage => 2.25,
                ElementManager.Element.Sorrow => 2.25,
                ElementManager.Element.Terror => 0.63,
                _ => 0.60
            },
            _ => 5.00
        };
    }
}