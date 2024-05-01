using Darkages.Sprites;
using Darkages.Types;

using System.Collections;

namespace Darkages.Object;

public abstract class ObjectService
{
    public static void AddGameObject<T>(T obj) where T : Sprite
    {
        if (obj.Pos.X >= byte.MaxValue || obj.Pos.Y >= byte.MaxValue ||
            !ServerSetup.Instance.SpriteCollections.TryGetValue(obj.CurrentMapId, out var mapCollections) ||
            !mapCollections.TryGetValue(typeof(T), out var objCollection)) return;
        
        ((SpriteCollection<T>)objCollection).Add(obj);
    }

    public static T Query<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        var collections = map is null ? ServerSetup.Instance.SpriteCollections.Values : new[] { ServerSetup.Instance.SpriteCollections[map.ID] };

        foreach (var mapCollections in collections)
        {
            if (!mapCollections.TryGetValue(typeof(T), out var spriteCollection)) continue;
            var queriedObject = ((SpriteCollection<T>)spriteCollection).Query(predicate);
            if (queriedObject is not null) return queriedObject;
        }

        return null;
    }

    public static IEnumerable<T> QueryAll<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null)
        {
            var collections = ServerSetup.Instance.SpriteCollections.Values
                .Select(mapDict => mapDict.TryGetValue(typeof(T), out var collection) ? (SpriteCollection<T>)collection : null)
                .Where(collection => collection is not null && collection.Any());
            var stack = new List<T>();

            foreach (var obj in collections)
                stack.AddRange(obj.QueryAll(predicate));

            return stack;
        }

        if (!ServerSetup.Instance.SpriteCollections.TryGetValue(map.ID, out var mapCollections)) return null;
        return mapCollections.TryGetValue(typeof(T), out var sprites) ? ((SpriteCollection<T>)sprites).QueryAll(predicate) : null;
    }

    public static void RemoveGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null || !ServerSetup.Instance.SpriteCollections.TryGetValue(obj.CurrentMapId, out var mapCollections)) return;
        if (mapCollections.TryGetValue(typeof(T), out var objCollection))
            ((SpriteCollection<T>)objCollection).Delete(obj);
    }
}

public class SpriteCollection<T> : IEnumerable<T> where T : Sprite
{
    private readonly List<T> _values = [];

    public void Add(T obj)
    {
        if (obj is null) return;

        lock (_values)
        {
            var existingIndex = _values.FindIndex(i => i.Serial == obj.Serial);
            if (existingIndex >= 0)
            {
                _values[existingIndex] = obj;
                return;
            }

            _values.Add(obj);
        }
    }

    public void Delete(T obj)
    {
        if (obj is null) return;
        _values.RemoveAll(item => item == obj);
    }

    public T Query(Predicate<T> predicate) => predicate is null ? default : (from item in _values where predicate(item) select item.Abyss ? default : item).FirstOrDefault();

    public IEnumerable<T> QueryAll(Predicate<T> predicate)
    {
        if (predicate is null) yield break;

        foreach (var item in _values.Where(item => predicate(item)))
        {
            yield return item.Abyss ? default : item;
        }
    }

    public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}