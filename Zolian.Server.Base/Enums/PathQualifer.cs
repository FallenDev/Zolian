namespace Darkages.Enums
{
    [Flags]
    public enum PathQualifer
    {
        Wander = 1,
        Fixed = 2,
        Patrol = 3
    }

    public static class PathExtensions
    {
        public static bool PathFlagIsSet(this PathQualifer self, PathQualifer flag) => (self & flag) == flag;
    }
}