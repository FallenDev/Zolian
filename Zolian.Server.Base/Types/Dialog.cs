namespace Darkages.Types;

public abstract class Dialog
{
    public class OptionsDataItem(short step, string text)
    {
        public short Step { get; } = step;
        public string Text { get; } = text;
    }
}