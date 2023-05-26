namespace Darkages.Database;

public abstract record StorageManager
{
    public static readonly AislingStorage AislingBucket = new();
}