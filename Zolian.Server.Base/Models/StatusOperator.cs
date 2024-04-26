using Darkages.Enums;

namespace Darkages.Models;

public class StatusOperator(Operator option, int value)
{
    public (Operator, int) Value { get; set; } = (option, value);
}