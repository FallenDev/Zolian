using Darkages.Sprites;
using Darkages.Types;

using Newtonsoft.Json;

namespace Darkages.Object;

public interface IObjectManager
{
    void AddObject<T>(T obj, Predicate<T> p = null) where T : Sprite;
    void DelObject<T>(T obj) where T : Sprite;
    void DelObjects<T>(T[] obj) where T : Sprite;
    T GetObject<T>(Area map, Predicate<T> p) where T : Sprite;
    Sprite GetObject(Area map, Predicate<Sprite> p, ObjectManager.Get selections);
    T GetObjectByName<T>(string name, Area map = null) where T : Sprite, new();
    IEnumerable<T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite;
    IEnumerable<Sprite> GetObjects(Area map, Predicate<Sprite> p, ObjectManager.Get selections);
    T PersonalMailJsonConvert<T>(object source);
    Aisling GetAislingForMailDeliveryMessage(string name);
}

public class ObjectManager : IObjectManager
{
    [Flags]
    public enum Get
    {
        Aislings = 1,
        MonsterDamage = Aislings,
        Monsters = 2,
        AislingDamage = Monsters | Aislings,
        Mundanes = 4,
        UpdateNonPlayerSprites = Monsters | Mundanes,
        Items = 8,
        Money = 16,
        AllButAislings = Monsters | Mundanes | Items | Money,
        All = Aislings | Items | Money | Monsters | Mundanes
    }

    public void AddObject<T>(T obj, Predicate<T> p = null) where T : Sprite
    {
        if (p != null && p(obj))
            ServerSetup.Instance.Game.ObjectFactory.AddGameObject(obj);
        else if (p == null)
            ServerSetup.Instance.Game.ObjectFactory.AddGameObject(obj);
    }

    public void DelObject<T>(T obj) where T : Sprite => ServerSetup.Instance.Game?.ObjectFactory.RemoveGameObject(obj);

    public void DelObjects<T>(T[] obj) where T : Sprite => ServerSetup.Instance.Game?.ObjectFactory.RemoveAllGameObjects(obj);

    public T GetObject<T>(Area map, Predicate<T> p) where T : Sprite => ServerSetup.Instance.Game?.ObjectFactory.Query(map, p);

    public Sprite GetObject(Area map, Predicate<Sprite> p, Get selections) => GetObjects(map, p, selections).FirstOrDefault();

    public T GetObjectByName<T>(string name, Area map = null) where T : Sprite, new()
    {
        var objType = new T();

        return objType switch
        {
            Aisling => GetObject<Aisling>(null,
                i => i != null && string.Equals(i.Username.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).Cast<T>(),
            Monster => GetObject<Monster>(map,
                i => i != null && string.Equals(i.Template.Name.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).Cast<T>(),
            Mundane => GetObject<Mundane>(map,
                i => i != null && string.Equals(i.Template.Name.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).Cast<T>(),
            Item => GetObject<Item>(map,
                i => i != null && string.Equals(i.Template.Name.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).Cast<T>(),
            _ => null
        };
    }

    public IEnumerable<T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite => ServerSetup.Instance.Game?.ObjectFactory.QueryAll(map, p);

    public IEnumerable<Sprite> GetObjects(Area map, Predicate<Sprite> p, Get selections)
    {
        List<Sprite> bucket = new();

        switch (selections)
        {
            case Get.Aislings:
                bucket.AddRange(GetObjects<Aisling>(map, p));
                break;
            case Get.Monsters:
                bucket.AddRange(GetObjects<Monster>(map, p));
                break;
            case Get.AislingDamage:
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Aisling>(map, p));
                break;
            case Get.Mundanes:
                bucket.AddRange(GetObjects<Mundane>(map, p));
                break;
            case Get.UpdateNonPlayerSprites:
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                break;
            case Get.Items:
                bucket.AddRange(GetObjects<Item>(map, p));
                break;
            case Get.Money:
                bucket.AddRange(GetObjects<Money>(map, p));
                break;
            case Get.AllButAislings:
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                bucket.AddRange(GetObjects<Item>(map, p));
                bucket.AddRange(GetObjects<Money>(map, p));
                break;
            case Get.All:
                bucket.AddRange(GetObjects<Aisling>(map, p));
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                bucket.AddRange(GetObjects<Money>(map, p));
                bucket.AddRange(GetObjects<Item>(map, p));
                break;
            default:
                bucket.AddRange(GetObjects<Aisling>(map, p));
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                bucket.AddRange(GetObjects<Money>(map, p));
                bucket.AddRange(GetObjects<Item>(map, p));
                break;
        }

        return bucket;
    }

    public T PersonalMailJsonConvert<T>(object source)
    {
        var serialized = JsonConvert.SerializeObject(source, Formatting.Indented, Settings);
        return JsonConvert.DeserializeObject<T>(serialized, Settings);
    }

    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public Aisling GetAislingForMailDeliveryMessage(string name)
    {
        try
        {
            var sprite = GetObjects(null, i => i is Aisling aisling && string.Equals(aisling.Username, name, StringComparison.CurrentCultureIgnoreCase), Get.Aislings).FirstOrDefault();

            if (sprite is Aisling player) return player;
        }
        catch
        {
            // Ignored
        }

        return null;
    }
}