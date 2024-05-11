using Darkages.Sprites;
using Darkages.Types;

using System.Collections;
using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace Darkages.Object;

public abstract class ObjectService
{
    public static void AddGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null) return;
        if (obj.Pos.X >= byte.MaxValue || obj.Pos.Y >= byte.MaxValue) return;
        var mapId = obj.CurrentMapId;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!obj.Map.SpriteCollections.TryGetValue(key, out var objCollection)) return;
        
        var spriteCollection = (SpriteCollection<T>)objCollection;
        spriteCollection.Add(obj);
    }

    public static void RemoveGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null) return;
        var mapId = obj.CurrentMapId;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!obj.Map.SpriteCollections.TryGetValue(key, out var objCollection)) return;

        var spriteCollection = (SpriteCollection<T>)objCollection;
        spriteCollection.Delete(obj);
    }

    public static T Query<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null) return default;
        var spriteType = typeof(T);
        var key = Tuple.Create(map.ID, spriteType);

        return !map.SpriteCollections.TryGetValue(key, out var objCollection) ? default : ((SpriteCollection<T>)objCollection).Query(predicate);
    }

    public static IEnumerable<T> QueryAll<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null) return default;
        var mapId = map.ID;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!map.SpriteCollections.TryGetValue(key, out var objCollection) ||
            objCollection is not SpriteCollection<T> spriteCollection) return default;

        return spriteCollection.QueryAll(predicate);
    }

    public static IEnumerable<T> QueryAll<T>(Predicate<T> predicate) where T : Sprite
    {
        if (predicate is not Sprite sprite) return default;
        var map = sprite.Map;
        if (map is null) return default;
        var mapId = map.ID;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!map.SpriteCollections.TryGetValue(key, out var objCollection) ||
            objCollection is not SpriteCollection<T> spriteCollection) return default;

        return spriteCollection.QueryAll(predicate);
    }
}

public class SpriteCollection<T> : IEnumerable<T> where T : Sprite
{
    private readonly ConcurrentDictionary<uint, T> _values = [];

    public void Add(T obj)
    {
        if (obj is null) return;
        _values.AddOrUpdate(obj.Serial, obj, (_, _) => obj);
    }

    public void Delete(T obj)
    {
        if (obj is null) return;
        _values.TryRemove(obj.Serial, out _);
    }

    [CanBeNull] public T Query(Predicate<T> predicate) => _values.Values.FirstOrDefault(item => predicate(item) && !item.Abyss);

    public IEnumerable<T> QueryAll(Predicate<T> predicate) => _values.Values.Where(item => predicate(item) && !item.Abyss);

    public IEnumerator<T> GetEnumerator() => _values.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}