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
        Movable = Monsters | Aislings | Mundanes,
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
    public static ConcurrentDictionary<long, T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite => map == null ? GetObjects(p) : ObjectService.QueryAllWithPredicate(map, p);
    private static ConcurrentDictionary<long, T> GetObjects<T>(Predicate<T> p) where T : Sprite => ObjectService.QueryAllWithPredicate(p);

    public static List<Sprite> GetObjects(Area map, Predicate<Sprite> p, Get selections)
    {
        List<Sprite> bucket = [];

        try
        {
            switch (selections)
            {
                case Get.Aislings:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Aisling>(map, p).Values);
                    break;
                case Get.Monsters:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Monster>(map, p).Values);
                    break;
                case Get.Damageable:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Monster>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Aisling>(map, p).Values);
                    break;
                case Get.Mundanes:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Mundane>(map, p).Values);
                    break;
                case Get.UpdateNonPlayerSprites:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Monster>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Mundane>(map, p).Values);
                    break;
                case Get.Movable:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Aisling>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Monster>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Mundane>(map, p).Values);
                    break;
                case Get.Items:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Item>(map, p).Values);
                    break;
                case Get.Money:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Money>(map, p).Values);
                    break;
                case Get.AllButAislings:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Monster>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Mundane>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Item>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Money>(map, p).Values);
                    break;
                case Get.All:
                default:
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Aisling>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Monster>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Mundane>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Money>(map, p).Values);
                    bucket.AddRange(ObjectService.QueryAllWithPredicate<Item>(map, p).Values);
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