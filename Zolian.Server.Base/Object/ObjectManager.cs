using System.Collections.Concurrent;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Object;

public class ObjectManager
{
    [Flags]
    public enum Get
    {
        Aislings = 1,
        Monsters = 2,
        Damageable = Monsters | Aislings,
        Mundanes = 4,
        UpdateNonPlayerSprites = Monsters | Mundanes,
        Items = 8,
        Money = 16,
        AllButAislings = Monsters | Mundanes | Items | Money,
        All = Aislings | Items | Money | Monsters | Mundanes
    }

    public static void AddObject<T>(T obj, Predicate<T> p = null) where T : Sprite
    {
        if (p == null || p(obj))
            ObjectService.AddGameObject(obj);
    }

    public static void DelObject<T>(T obj) where T : Sprite => ObjectService.RemoveGameObject(obj);
    public static T GetObject<T>(Area map, Predicate<T> p) where T : Sprite => ObjectService.Query(map, p);
    public static Sprite GetObject(Area map, Predicate<Sprite> p, Get selections) => GetObjects(map, p, selections).FirstOrDefault();
    public static ConcurrentDictionary<long, T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite => map == null ? GetObjects(p) : ObjectService.QueryAll(map, p);
    private static ConcurrentDictionary<long, T> GetObjects<T>(Predicate<T> p) where T : Sprite => ObjectService.QueryAll(p);

    public static List<Sprite> GetObjects(Area map, Predicate<Sprite> p, Get selections)
    {
        List<Sprite> bucket = [];

        try
        {
            switch (selections)
            {
                case Get.Aislings:
                    bucket.AddRange(GetObjects<Aisling>(map, p).Values);
                    break;
                case Get.Monsters:
                    bucket.AddRange(GetObjects<Monster>(map, p).Values);
                    break;
                case Get.Damageable:
                    bucket.AddRange(GetObjects<Monster>(map, p).Values);
                    bucket.AddRange(GetObjects<Aisling>(map, p).Values);
                    break;
                case Get.Mundanes:
                    bucket.AddRange(GetObjects<Mundane>(map, p).Values);
                    break;
                case Get.UpdateNonPlayerSprites:
                    bucket.AddRange(GetObjects<Monster>(map, p).Values);
                    bucket.AddRange(GetObjects<Mundane>(map, p).Values);
                    break;
                case Get.Items:
                    bucket.AddRange(GetObjects<Item>(map, p).Values);
                    break;
                case Get.Money:
                    bucket.AddRange(GetObjects<Money>(map, p).Values);
                    break;
                case Get.AllButAislings:
                    bucket.AddRange(GetObjects<Monster>(map, p).Values);
                    bucket.AddRange(GetObjects<Mundane>(map, p).Values);
                    bucket.AddRange(GetObjects<Item>(map, p).Values);
                    bucket.AddRange(GetObjects<Money>(map, p).Values);
                    break;
                case Get.All:
                    bucket.AddRange(GetObjects<Aisling>(map, p).Values);
                    bucket.AddRange(GetObjects<Monster>(map, p).Values);
                    bucket.AddRange(GetObjects<Mundane>(map, p).Values);
                    bucket.AddRange(GetObjects<Money>(map, p).Values);
                    bucket.AddRange(GetObjects<Item>(map, p).Values);
                    break;
                default:
                    bucket.AddRange(GetObjects<Aisling>(map, p).Values);
                    bucket.AddRange(GetObjects<Monster>(map, p).Values);
                    bucket.AddRange(GetObjects<Mundane>(map, p).Values);
                    bucket.AddRange(GetObjects<Money>(map, p).Values);
                    bucket.AddRange(GetObjects<Item>(map, p).Values);
                    break;
            }
        }
        catch
        {
            // ignored -- Can sometimes throw an error when testing new maps using /rm
        }

        return bucket;
    }
}