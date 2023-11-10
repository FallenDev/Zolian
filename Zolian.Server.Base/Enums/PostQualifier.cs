namespace Darkages.Enums;

[Flags]
public enum PostQualifier
{
    BreakInvisible = 1,
    IgnoreDefense = 1 << 1,
    None = 1 << 2,
    Both = BreakInvisible | IgnoreDefense
}

public static class PostQualifierExtensions
{
    public static bool QualifierFlagIsSet(this PostQualifier self, PostQualifier flag) => (self & flag) == flag;
}