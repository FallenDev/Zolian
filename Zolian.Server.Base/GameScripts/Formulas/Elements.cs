using Darkages.Enums;
using Darkages.Scripting;
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
                    _ => 1
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
                    _ => 1
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
                    _ => 1
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
                    _ => 1
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
                    _ => 1
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
                    _ => 1
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
                    _ => 1
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
                    _ => 1
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
                    _ => 1
                },
                _ => 1
            },
            1.5 => FasNadur(obj, offenseElement, defenseElement),
            2 => MorFasNadur(obj, offenseElement, defenseElement),
            3 => ArdFasNadur(obj, offenseElement, defenseElement),
            _ => 1
        };
    }

    public override double FasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement)
    {
        return defenseElement switch
        {
            ElementManager.Element.None when offenseElement != ElementManager.Element.None => 3.00,
            ElementManager.Element.Fire => offenseElement switch
            {
                ElementManager.Element.None => 0.39,
                ElementManager.Element.Fire => 0.39,
                ElementManager.Element.Wind => 0.44,
                ElementManager.Element.Earth => 0.57,
                ElementManager.Element.Water => 2.63,
                ElementManager.Element.Holy => 0.50,
                ElementManager.Element.Void => 1.88,
                ElementManager.Element.Rage => 1.35,
                ElementManager.Element.Sorrow => 1.35,
                ElementManager.Element.Terror => 1.35,
                _ => 1
            },
            ElementManager.Element.Wind => offenseElement switch
            {
                ElementManager.Element.None => 0.39,
                ElementManager.Element.Fire => 2.63,
                ElementManager.Element.Wind => 0.39,
                ElementManager.Element.Earth => 0.44,
                ElementManager.Element.Water => 0.57,
                ElementManager.Element.Holy => 1.88,
                ElementManager.Element.Void => 0.50,
                ElementManager.Element.Rage => 1.35,
                ElementManager.Element.Sorrow => 1.35,
                ElementManager.Element.Terror => 1.35,
                _ => 1
            },
            ElementManager.Element.Earth => offenseElement switch
            {
                ElementManager.Element.None => 0.39,
                ElementManager.Element.Fire => 0.57,
                ElementManager.Element.Wind => 2.63,
                ElementManager.Element.Earth => 0.39,
                ElementManager.Element.Water => 0.44,
                ElementManager.Element.Holy => 0.50,
                ElementManager.Element.Void => 1.88,
                ElementManager.Element.Rage => 1.35,
                ElementManager.Element.Sorrow => 1.35,
                ElementManager.Element.Terror => 1.35,
                _ => 1
            },
            ElementManager.Element.Water => offenseElement switch
            {
                ElementManager.Element.None => 0.39,
                ElementManager.Element.Fire => 0.44,
                ElementManager.Element.Wind => 0.57,
                ElementManager.Element.Earth => 2.63,
                ElementManager.Element.Water => 0.39,
                ElementManager.Element.Holy => 1.88,
                ElementManager.Element.Void => 0.50,
                ElementManager.Element.Rage => 1.35,
                ElementManager.Element.Sorrow => 1.35,
                ElementManager.Element.Terror => 1.35,
                _ => 1
            },
            ElementManager.Element.Holy => offenseElement switch
            {
                ElementManager.Element.None => 0.39,
                ElementManager.Element.Fire => 1.88,
                ElementManager.Element.Wind => 0.50,
                ElementManager.Element.Earth => 1.88,
                ElementManager.Element.Water => 0.50,
                ElementManager.Element.Holy => 0.33,
                ElementManager.Element.Void => 2.63,
                ElementManager.Element.Rage => 1.10,
                ElementManager.Element.Sorrow => 0.70,
                ElementManager.Element.Terror => 1.10,
                _ => 1
            },
            ElementManager.Element.Void => offenseElement switch
            {
                ElementManager.Element.None => 0.39,
                ElementManager.Element.Fire => 0.50,
                ElementManager.Element.Wind => 1.88,
                ElementManager.Element.Earth => 0.50,
                ElementManager.Element.Water => 1.88,
                ElementManager.Element.Holy => 2.63,
                ElementManager.Element.Void => 0.33,
                ElementManager.Element.Rage => 1.10,
                ElementManager.Element.Sorrow => 1.10,
                ElementManager.Element.Terror => 0.70,
                _ => 1
            },
            ElementManager.Element.Rage => offenseElement switch
            {
                ElementManager.Element.None => 0.15,
                ElementManager.Element.Fire => 0.60,
                ElementManager.Element.Wind => 0.50,
                ElementManager.Element.Earth => 0.60,
                ElementManager.Element.Water => 0.50,
                ElementManager.Element.Holy => 1.20,
                ElementManager.Element.Void => 0.60,
                ElementManager.Element.Rage => 0.25,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 0.75,
                _ => 1
            },
            ElementManager.Element.Sorrow => offenseElement switch
            {
                ElementManager.Element.None => 0.15,
                ElementManager.Element.Fire => 0.50,
                ElementManager.Element.Wind => 0.60,
                ElementManager.Element.Earth => 0.50,
                ElementManager.Element.Water => 0.60,
                ElementManager.Element.Holy => 0.60,
                ElementManager.Element.Void => 1.20,
                ElementManager.Element.Rage => 0.75,
                ElementManager.Element.Sorrow => 0.25,
                ElementManager.Element.Terror => 1.50,
                _ => 1
            },
            ElementManager.Element.Terror => offenseElement switch
            {
                ElementManager.Element.None => 0.15,
                ElementManager.Element.Fire => 0.40,
                ElementManager.Element.Wind => 0.40,
                ElementManager.Element.Earth => 0.40,
                ElementManager.Element.Water => 0.40,
                ElementManager.Element.Holy => 0.80,
                ElementManager.Element.Void => 0.80,
                ElementManager.Element.Rage => 0.90,
                ElementManager.Element.Sorrow => 0.90,
                ElementManager.Element.Terror => 0.25,
                _ => 1
            },
            _ => 1
        };
    }

    public override double MorFasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement)
    {
        return defenseElement switch
        {
            ElementManager.Element.None when offenseElement != ElementManager.Element.None => 4.00,
            ElementManager.Element.Fire => offenseElement switch
            {
                ElementManager.Element.None => 0.29,
                ElementManager.Element.Fire => 0.29,
                ElementManager.Element.Wind => 0.33,
                ElementManager.Element.Earth => 0.43,
                ElementManager.Element.Water => 3.50,
                ElementManager.Element.Holy => 0.38,
                ElementManager.Element.Void => 2.50,
                ElementManager.Element.Rage => 1.45,
                ElementManager.Element.Sorrow => 1.45,
                ElementManager.Element.Terror => 1.45,
                _ => 1
            },
            ElementManager.Element.Wind => offenseElement switch
            {
                ElementManager.Element.None => 0.29,
                ElementManager.Element.Fire => 3.50,
                ElementManager.Element.Wind => 0.29,
                ElementManager.Element.Earth => 0.33,
                ElementManager.Element.Water => 0.43,
                ElementManager.Element.Holy => 2.50,
                ElementManager.Element.Void => 0.38,
                ElementManager.Element.Rage => 1.45,
                ElementManager.Element.Sorrow => 1.45,
                ElementManager.Element.Terror => 1.45,
                _ => 1
            },
            ElementManager.Element.Earth => offenseElement switch
            {
                ElementManager.Element.None => 0.29,
                ElementManager.Element.Fire => 0.43,
                ElementManager.Element.Wind => 3.50,
                ElementManager.Element.Earth => 0.29,
                ElementManager.Element.Water => 0.33,
                ElementManager.Element.Holy => 0.38,
                ElementManager.Element.Void => 2.50,
                ElementManager.Element.Rage => 1.45,
                ElementManager.Element.Sorrow => 1.45,
                ElementManager.Element.Terror => 1.45,
                _ => 1
            },
            ElementManager.Element.Water => offenseElement switch
            {
                ElementManager.Element.None => 0.29,
                ElementManager.Element.Fire => 0.33,
                ElementManager.Element.Wind => 0.43,
                ElementManager.Element.Earth => 3.50,
                ElementManager.Element.Water => 0.29,
                ElementManager.Element.Holy => 2.50,
                ElementManager.Element.Void => 0.38,
                ElementManager.Element.Rage => 1.45,
                ElementManager.Element.Sorrow => 1.45,
                ElementManager.Element.Terror => 1.45,
                _ => 1
            },
            ElementManager.Element.Holy => offenseElement switch
            {
                ElementManager.Element.None => 0.29,
                ElementManager.Element.Fire => 2.50,
                ElementManager.Element.Wind => 0.38,
                ElementManager.Element.Earth => 2.50,
                ElementManager.Element.Water => 0.38,
                ElementManager.Element.Holy => 0.25,
                ElementManager.Element.Void => 3.50,
                ElementManager.Element.Rage => 1.15,
                ElementManager.Element.Sorrow => 0.65,
                ElementManager.Element.Terror => 1.15,
                _ => 1
            },
            ElementManager.Element.Void => offenseElement switch
            {
                ElementManager.Element.None => 0.29,
                ElementManager.Element.Fire => 0.38,
                ElementManager.Element.Wind => 2.50,
                ElementManager.Element.Earth => 0.38,
                ElementManager.Element.Water => 2.50,
                ElementManager.Element.Holy => 3.50,
                ElementManager.Element.Void => 0.25,
                ElementManager.Element.Rage => 1.15,
                ElementManager.Element.Sorrow => 1.15,
                ElementManager.Element.Terror => 0.65,
                _ => 1
            },
            ElementManager.Element.Rage => offenseElement switch
            {
                ElementManager.Element.None => 0.10,
                ElementManager.Element.Fire => 0.60,
                ElementManager.Element.Wind => 0.50,
                ElementManager.Element.Earth => 0.60,
                ElementManager.Element.Water => 0.50,
                ElementManager.Element.Holy => 1.20,
                ElementManager.Element.Void => 0.60,
                ElementManager.Element.Rage => 0.25,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 0.75,
                _ => 1
            },
            ElementManager.Element.Sorrow => offenseElement switch
            {
                ElementManager.Element.None => 0.10,
                ElementManager.Element.Fire => 0.50,
                ElementManager.Element.Wind => 0.60,
                ElementManager.Element.Earth => 0.50,
                ElementManager.Element.Water => 0.60,
                ElementManager.Element.Holy => 0.60,
                ElementManager.Element.Void => 1.20,
                ElementManager.Element.Rage => 0.75,
                ElementManager.Element.Sorrow => 0.25,
                ElementManager.Element.Terror => 1.50,
                _ => 1
            },
            ElementManager.Element.Terror => offenseElement switch
            {
                ElementManager.Element.None => 0.10,
                ElementManager.Element.Fire => 0.40,
                ElementManager.Element.Wind => 0.40,
                ElementManager.Element.Earth => 0.40,
                ElementManager.Element.Water => 0.40,
                ElementManager.Element.Holy => 0.80,
                ElementManager.Element.Void => 0.80,
                ElementManager.Element.Rage => 0.90,
                ElementManager.Element.Sorrow => 0.90,
                ElementManager.Element.Terror => 0.25,
                _ => 1
            },
            _ => 1
        };
    }

    public override double ArdFasNadur(Sprite obj, ElementManager.Element offenseElement, ElementManager.Element defenseElement)
    {
        return defenseElement switch
        {
            ElementManager.Element.None when offenseElement != ElementManager.Element.None => 6.00,
            ElementManager.Element.Fire => offenseElement switch
            {
                ElementManager.Element.None => 0.19,
                ElementManager.Element.Fire => 0.19,
                ElementManager.Element.Wind => 0.22,
                ElementManager.Element.Earth => 0.28,
                ElementManager.Element.Water => 5.25,
                ElementManager.Element.Holy => 0.25,
                ElementManager.Element.Void => 3.75,
                ElementManager.Element.Rage => 1.50,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 1.50,
                _ => 1
            },
            ElementManager.Element.Wind => offenseElement switch
            {
                ElementManager.Element.None => 0.19,
                ElementManager.Element.Fire => 5.25,
                ElementManager.Element.Wind => 0.19,
                ElementManager.Element.Earth => 0.22,
                ElementManager.Element.Water => 0.28,
                ElementManager.Element.Holy => 3.75,
                ElementManager.Element.Void => 0.25,
                ElementManager.Element.Rage => 1.50,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 1.50,
                _ => 1
            },
            ElementManager.Element.Earth => offenseElement switch
            {
                ElementManager.Element.None => 0.19,
                ElementManager.Element.Fire => 0.28,
                ElementManager.Element.Wind => 5.25,
                ElementManager.Element.Earth => 0.19,
                ElementManager.Element.Water => 0.22,
                ElementManager.Element.Holy => 0.25,
                ElementManager.Element.Void => 3.75,
                ElementManager.Element.Rage => 1.50,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 1.50,
                _ => 1
            },
            ElementManager.Element.Water => offenseElement switch
            {
                ElementManager.Element.None => 0.19,
                ElementManager.Element.Fire => 0.22,
                ElementManager.Element.Wind => 0.28,
                ElementManager.Element.Earth => 5.25,
                ElementManager.Element.Water => 0.19,
                ElementManager.Element.Holy => 3.75,
                ElementManager.Element.Void => 0.25,
                ElementManager.Element.Rage => 1.50,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 1.50,
                _ => 1
            },
            ElementManager.Element.Holy => offenseElement switch
            {
                ElementManager.Element.None => 0.19,
                ElementManager.Element.Fire => 3.75,
                ElementManager.Element.Wind => 0.25,
                ElementManager.Element.Earth => 3.75,
                ElementManager.Element.Water => 0.25,
                ElementManager.Element.Holy => 0.17,
                ElementManager.Element.Void => 5.25,
                ElementManager.Element.Rage => 1.20,
                ElementManager.Element.Sorrow => 0.60,
                ElementManager.Element.Terror => 1.20,
                _ => 1
            },
            ElementManager.Element.Void => offenseElement switch
            {
                ElementManager.Element.None => 0.19,
                ElementManager.Element.Fire => 0.25,
                ElementManager.Element.Wind => 3.75,
                ElementManager.Element.Earth => 0.25,
                ElementManager.Element.Water => 3.75,
                ElementManager.Element.Holy => 5.25,
                ElementManager.Element.Void => 0.17,
                ElementManager.Element.Rage => 1.20,
                ElementManager.Element.Sorrow => 1.20,
                ElementManager.Element.Terror => 0.60,
                _ => 1
            },
            ElementManager.Element.Rage => offenseElement switch
            {
                ElementManager.Element.None => 0.05,
                ElementManager.Element.Fire => 0.60,
                ElementManager.Element.Wind => 0.50,
                ElementManager.Element.Earth => 0.60,
                ElementManager.Element.Water => 0.50,
                ElementManager.Element.Holy => 1.20,
                ElementManager.Element.Void => 0.60,
                ElementManager.Element.Rage => 0.25,
                ElementManager.Element.Sorrow => 1.50,
                ElementManager.Element.Terror => 0.75,
                _ => 1
            },
            ElementManager.Element.Sorrow => offenseElement switch
            {
                ElementManager.Element.None => 0.05,
                ElementManager.Element.Fire => 0.50,
                ElementManager.Element.Wind => 0.60,
                ElementManager.Element.Earth => 0.50,
                ElementManager.Element.Water => 0.60,
                ElementManager.Element.Holy => 0.60,
                ElementManager.Element.Void => 1.20,
                ElementManager.Element.Rage => 0.75,
                ElementManager.Element.Sorrow => 0.25,
                ElementManager.Element.Terror => 1.50,
                _ => 1
            },
            ElementManager.Element.Terror => offenseElement switch
            {
                ElementManager.Element.None => 0.05,
                ElementManager.Element.Fire => 0.40,
                ElementManager.Element.Wind => 0.40,
                ElementManager.Element.Earth => 0.40,
                ElementManager.Element.Water => 0.40,
                ElementManager.Element.Holy => 0.80,
                ElementManager.Element.Void => 0.80,
                ElementManager.Element.Rage => 0.90,
                ElementManager.Element.Sorrow => 0.90,
                ElementManager.Element.Terror => 0.25,
                _ => 1
            },
            _ => 1
        };
    }
}