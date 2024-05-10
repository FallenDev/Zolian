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
        if (!obj.Map.SpriteCollections.TryGetValue(obj.CurrentMapId, out var mapCollections) ||
            !mapCollections.TryGetValue(typeof(T), out var objCollection)) return;

        var spriteCollection = (SpriteCollection<T>)objCollection;
        spriteCollection.Add(obj);
    }

    public static void RemoveGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null) return;
        if (!obj.Map.SpriteCollections.TryGetValue(obj.CurrentMapId, out var mapCollections) ||
            !mapCollections.TryGetValue(typeof(T), out var objCollection)) return;

        var spriteCollection = (SpriteCollection<T>)objCollection;
        spriteCollection.Delete(obj);
    }

    public static T Query<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null) return default;
        var collections = map.SpriteCollections.Values;

        foreach (var mapCollections in collections)
        {
            if (!mapCollections.TryGetValue(typeof(T), out var spriteCollection)) continue;
            var queriedObject = ((SpriteCollection<T>)spriteCollection).Query(predicate);
            if (queriedObject is not null) return queriedObject;
        }

        return default;
    }

    public static IEnumerable<T> QueryAll<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null) return default;
        if (!map.SpriteCollections.TryGetValue(map.ID, out var mapCollections)) return null;
        return mapCollections.TryGetValue(typeof(T), out var sprites) ? ((SpriteCollection<T>)sprites).QueryAll(predicate) : null;
    }
}

public class SpriteCollection<T> : IEnumerable<T> where T : Sprite
{
    private readonly ConcurrentDictionary<uint, T> _values = [];

    public void Add(T obj)
    {
        if (obj is null) return;
        _values.AddOrUpdate(obj.Serial, obj, (key, existingVal) => obj);
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