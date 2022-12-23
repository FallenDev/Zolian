namespace Darkages.Enums
{
    public class SpellOperator
    {
        public enum SpellOperatorPolicy
        {
            Set = 0,
            Increase = 1,
            Decrease = 2
        }

        public enum SpellOperatorScope
        {
            Ioc = 0,
            Cradh = 1,
            Nadur = 2,
            All = 3
        }

        public enum AoEShape
        {
            None = 0,
            Normal = 1,
            StraightLine = 2,
            GroupOnly = 3,
            GroupAndSelf = 4,
        }

        public SpellOperator(SpellOperatorPolicy option, SpellOperatorScope scope, int value, int min, int max = 9)
        {
            Option = option;
            Scope = scope;
            Value = value;
            MinValue = min;
            MaxValue = max;
        }

        public int MaxValue { get; set; }
        public int MinValue { get; set; }
        public SpellOperatorPolicy Option { get; set; }
        public SpellOperatorScope Scope { get; set; }
        public int Value { get; set; }
    }
}