using Darkages.Enums;

namespace Darkages.Models;

public class StatusOperator(Operator option, int value)
{
    public StatusOperator() : this(Operator.Add, 0)
    {
    }

    public Operator Option { get; set; } = option;
    public int Value { get; set; } = value;
}