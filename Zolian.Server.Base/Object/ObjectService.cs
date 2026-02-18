using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Object;

public abstract class ObjectService
{
    /// <summary>
    /// Hot-path cache so we don't pay:
    ///  - Tuple key resolution
    ///  - object boxing + type checks
    ///  - ConcurrentDictionary lookups on Area.SpriteCollections for every add/remove/query/fill.
    ///
    /// Area.SpriteCollections remains the source-of-truth. This cache is just a fast pointer
    /// to the strongly-typed SpriteCollection<T> for a given mapId.
    /// </summary>
    private static class CollectionCache<T> where T : Sprite
    {
        internal static readonly ConcurrentDictionary<int, SpriteCollection<T>> ByMapId = new();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetCollection<T>(Area map, out SpriteCollection<T> collection) where T : Sprite
    {
        // Fast path: already resolved for this mapId
        if (CollectionCache<T>.ByMapId.TryGetValue(map.ID, out collection!))
            return true;

        // Slow path: resolve from Area.SpriteCollections once, then cache
        if (!map.SpriteCollections.TryGetValue((map.ID, typeof(T)), out var objCollection) || objCollection is not SpriteCollection<T> typed)
        {
            collection = null;
            return false;
        }

        CollectionCache<T>.ByMapId.TryAdd(map.ID, typed);
        collection = typed;
        return true;
    }

    /// <summary>
    /// Returns a count of sprites of type <typeparamref name="T"/> in <paramref name="map"/>
    /// that match <paramref name="predicate"/>.
    ///
    /// Notes:
    /// - Zero allocations (no lists/dicts created)
    /// - Enumerates the backing ConcurrentDictionary once
    /// - Safe under concurrent adds/removes (snapshot-ish semantics of enumeration)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountWithPredicate<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null || predicate is null) return 0;

        if (!TryGetCollection<T>(map, out var spriteCollection))
            return 0;

        return spriteCollection.CountWithPredicate(predicate);
    }

    public static void AddGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null) return;
        if (obj.Pos.X >= byte.MaxValue || obj.Pos.Y >= byte.MaxValue) return;

        var map = obj.Map;
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.AddOrUpdate(obj);
    }

    public static void RemoveGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null) return;

        var map = obj.Map;
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.Delete(obj);
    }

    public static T Query<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null) return default;

        return TryGetCollection<T>(map, out var spriteCollection)
            ? spriteCollection.Query(predicate)
            : default;
    }

    public static void FillWithPredicate<T>(Area map, Predicate<T> predicate, List<T> results) where T : Sprite
    {
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.FillWithPredicate(predicate, results);
    }

    public static void ForEachWithPredicate<T>(Area map, Predicate<T> predicate, Action<T> action) where T : Sprite
    {
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.ForEachWithPredicate(predicate, action);
    }

    public static void FillSpriteBucket<T>(Area map, Predicate<Sprite> predicate, List<Sprite> bucket) where T : Sprite
    {
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.FillSpriteBucket(predicate, bucket);
    }

    public static Sprite QueryFirstSprite<T>(Area map, Predicate<Sprite> predicate) where T : Sprite
    {
        if (map is null) return default;

        return TryGetCollection<T>(map, out var spriteCollection)
            ? spriteCollection.QueryFirstSprite(predicate)
            : default;
    }

    public static void ForEachSpriteBucket<T>(Area map, Predicate<Sprite> predicate, Action<Sprite> action) where T : Sprite
    {
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.ForEachSpriteBucket(predicate, action);
    }
}

public sealed class SpriteCollection<T> where T : Sprite
{
    public ConcurrentDictionary<long, T> Sprites { get; } = new();

    public void AddOrUpdate(T obj)
    {
        if (obj is null) return;

        var id = obj is Item item ? item.ItemVisibilityId : obj.Serial;
        Sprites[id] = obj;
    }

    public void Delete(T obj)
    {
        if (obj is null) return;

        var id = obj is Item item ? item.ItemVisibilityId : obj.Serial;
        Sprites.TryRemove(id, out _);
    }

    public T Query(Predicate<T> predicate)
    {
        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                return s;
        }

        return default;
    }

    /// <summary>
    /// Counts sprites matching <paramref name="predicate"/> with no allocations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountWithPredicate(Predicate<T> predicate)
    {
        if (predicate is null) return 0;

        var count = 0;

        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                count++;
        }

        return count;
    }

    public void FillWithPredicate(Predicate<T> predicate, List<T> results)
    {
        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                results.Add(s);
        }
    }

    public void ForEachWithPredicate(Predicate<T> predicate, Action<T> action)
    {
        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                action(s);
        }
    }

    public void FillSpriteBucket(Predicate<Sprite> predicate, List<Sprite> bucket)
    {
        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                bucket.Add(s);
        }
    }

    public Sprite QueryFirstSprite(Predicate<Sprite> predicate)
    {
        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                return s;
        }

        return default;
    }

    public void ForEachSpriteBucket(Predicate<Sprite> predicate, Action<Sprite> action)
    {
        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                action(s);
        }
    }
}