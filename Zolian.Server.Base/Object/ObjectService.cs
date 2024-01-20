using Darkages.Sprites;
using Darkages.Types;

using System.Collections;

namespace Darkages.Object;

public sealed class ObjectService
{
    public ObjectService()
    {
        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
            ServerSetup.Instance.SpriteCollections.TryAdd(map.ID, new Dictionary<Type, object>
            {
                {typeof(Monster), new SpriteCollection<Monster>(Enumerable.Empty<Monster>())},
                {typeof(Aisling), new SpriteCollection<Aisling>(Enumerable.Empty<Aisling>())},
                {typeof(Mundane), new SpriteCollection<Mundane>(Enumerable.Empty<Mundane>())},
                {typeof(Item), new SpriteCollection<Item>(Enumerable.Empty<Item>())},
                {typeof(Money), new SpriteCollection<Money>(Enumerable.Empty<Money>())}
            });
    }

    public void AddGameObject<T>(T obj) where T : Sprite
    {
        if (obj.Pos.X >= byte.MaxValue) return;
        if (obj.Pos.Y >= byte.MaxValue) return;
        if (!ServerSetup.Instance.SpriteCollections.ContainsKey(obj.CurrentMapId)) return;
        if (!ServerSetup.Instance.SpriteCollections[obj.CurrentMapId].ContainsKey(typeof(T))) return;
        var objCollection = (SpriteCollection<T>)ServerSetup.Instance.SpriteCollections[obj.CurrentMapId][typeof(T)];
        objCollection?.Add(obj);
    }

    public T Query<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map == null)
        {
            var values = ServerSetup.Instance.SpriteCollections.Select(i => (SpriteCollection<T>)i.Value[typeof(T)]);
            return values.Where(obj => obj != null).Where(obj => obj.Any()).Select(obj => obj.Query(predicate)).FirstOrDefault();
        }

        if (!ServerSetup.Instance.SpriteCollections.ContainsKey(map!.ID)) return null;
        var sprite = (SpriteCollection<T>)ServerSetup.Instance.SpriteCollections[map.ID][typeof(T)];

        return sprite?.Query(predicate);
    }

    public IEnumerable<T> QueryAll<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map == null)
        {
            var values = ServerSetup.Instance.SpriteCollections.Select(i => (SpriteCollection<T>)i.Value[typeof(T)]);
            var stack = new List<T>();

            foreach (var obj in values.Where(obj => obj != null).Where(obj => obj.Any()))
            {
                stack.AddRange(obj.QueryAll(predicate));
            }

            return stack;
        }

        if (!ServerSetup.Instance.SpriteCollections.ContainsKey(map.ID)) return null;
        var sprites = (SpriteCollection<T>)ServerSetup.Instance.SpriteCollections[map.ID][typeof(T)];

        return sprites?.QueryAll(predicate);
    }

    public void RemoveAllGameObjects<T>(T[] objects) where T : Sprite
    {
        if (objects == null) return;

        for (uint i = 0; i < objects.Length; i++)
            RemoveGameObject(objects[i]);
    }

    public void RemoveGameObject<T>(T obj) where T : Sprite
    {
        if (obj != null && !ServerSetup.Instance.SpriteCollections.ContainsKey(obj.CurrentMapId)) return;
        if (obj == null) return;

        var objCollection = (SpriteCollection<T>)ServerSetup.Instance.SpriteCollections[obj.CurrentMapId][typeof(T)];
        objCollection?.Delete(obj);
    }
}

public class SpriteCollection<T>(IEnumerable<T> values) : IEnumerable<T>
    where T : Sprite
{
    private readonly List<T> _values = [..values];

    public void Add(T obj)
    {
        if (obj == null) return;

        lock (_values)
        {
            if (_values.Any(i => i.Serial == obj.Serial))
            {
                var eObj = _values.FindIndex(idx => idx.Serial == obj.Serial);

                if (eObj < 0) return;
                _values[eObj] = obj;
                return;
            }

            _values.Add(obj);
        }
    }

    public void Delete(T obj)
    {
        if (obj == null) return;

        for (var i = _values.Count - 1; i >= 0; i--)
        {
            var subject = obj as Sprite;
            var predicate = _values[i] as Sprite;

            if (subject == predicate) _values.RemoveAt(i);
        }
    }

    public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();

    public T Query(Predicate<T> predicate)
    {
        if (predicate == null) return default;

        for (var i = _values.Count - 1; i >= 0; i--)
            if (_values.Count > i)
            {
                var subject = predicate(_values[i]);

                if (subject)
                    return _values[i].Abyss ? default : _values[i];
            }

        return default;
    }

    public IEnumerable<T> QueryAll(Predicate<T> predicate)
    {
        if (predicate == null) yield return default;

        for (var i = _values.Count - 1; i >= 0; i--)
            if (i < _values.Count)
            {
                var subject = predicate != null && predicate(_values[i]);
                if (subject) yield return _values[i].Abyss ? default : _values[i];
            }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}