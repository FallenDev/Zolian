using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Object;

public abstract class ObjectService
{
    private static readonly ConcurrentDictionary<(int MapId, Type SpriteType), Tuple<int, Type>> _keyCache = new();

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
    private static Tuple<int, Type> GetKey(int mapId, Type spriteType) =>
        _keyCache.GetOrAdd((mapId, spriteType), static k => Tuple.Create(k.MapId, k.SpriteType));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetCollection<T>(Area map, out SpriteCollection<T> collection) where T : Sprite
    {
        // Fast path: already resolved for this mapId
        if (CollectionCache<T>.ByMapId.TryGetValue(map.ID, out collection!))
            return true;

        // Slow path: resolve from Area.SpriteCollections once, then cache
        var key = GetKey(map.ID, typeof(T));

        if (!map.SpriteCollections.TryGetValue(key, out var objCollection) || objCollection is not SpriteCollection<T> typed)
        {
            collection = null!;
            return false;
        }

        CollectionCache<T>.ByMapId.TryAdd(map.ID, typed);
        collection = typed;
        return true;
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

    public static ConcurrentDictionary<long, T> QueryAllWithPredicate<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        // NOTE: This is allocation-heavy. Prefer FillWithPredicate(map, predicate, results)
        // when you want a zero-alloc hot path.
        if (map is null) return new ConcurrentDictionary<long, T>();

        return TryGetCollection<T>(map, out var spriteCollection)
            ? spriteCollection.QueryAllWithPredicate(predicate)
            : new ConcurrentDictionary<long, T>();
    }

    public static void FillWithPredicate<T>(Area map, Predicate<T> predicate, List<T> results) where T : Sprite
    {
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.FillWithPredicate(predicate, results);
    }

    public static void FillSpriteBucket<T>(Area map, Predicate<Sprite> predicate, List<Sprite> bucket) where T : Sprite
    {
        if (map is null) return;

        if (!TryGetCollection<T>(map, out var spriteCollection)) return;
        spriteCollection.FillSpriteBucket(predicate, bucket);
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

    public ConcurrentDictionary<long, T> QueryAllWithPredicate(Predicate<T> predicate)
    {
        var result = new ConcurrentDictionary<long, T>();

        foreach (var (key, sprite) in Sprites)
        {
            if (sprite != null && predicate(sprite))
                result.TryAdd(key, sprite);
        }

        return result;
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

    public void FillSpriteBucket(Predicate<Sprite> predicate, List<Sprite> bucket)
    {
        foreach (var kv in Sprites)
        {
            var s = kv.Value;
            if (s != null && predicate(s))
                bucket.Add(s);
        }
    }
}