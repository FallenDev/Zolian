namespace Darkages.Types;

public abstract class Dialog
{
    public class OptionsDataItem(ushort step, string text)
    {
        public ushort Step { get; } = step;
        public string Text { get; } = text;
    }
}