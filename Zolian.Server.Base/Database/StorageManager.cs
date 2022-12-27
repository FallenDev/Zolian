using Darkages.Templates;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Darkages.Database;

public static class StorageManager
{
    public static readonly AislingStorage AislingBucket = new();
    public static readonly DatabaseLoad<ItemTemplate> ItemBucket = new();
    public static readonly DatabaseLoad<MonsterTemplate> MonsterBucket = new();
    public static readonly DatabaseLoad<MundaneTemplate> MundaneBucket = new();
    public static readonly DatabaseLoad<NationTemplate> NationBucket = new();
    public static readonly DatabaseLoad<SkillTemplate> SkillBucket = new();
    public static readonly DatabaseLoad<SpellTemplate> SpellBucket = new();
    public static readonly DatabaseLoad<WarpTemplate> WarpBucket = new();
    public static readonly DatabaseLoad<WorldMapTemplate> WorldMapBucket = new();
    private static readonly KnownTypesBinder HadesTypesBinder = new();

    static StorageManager()
    {
        HadesTypesBinder.KnownTypes = new List<Type>
        {
            typeof(WorldMapTemplate),
            typeof(ServerTemplate)
        };
    }

    private class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; }

        public Type BindToType(string assemblyName, string typeName)
        {
            return KnownTypes.Distinct().SingleOrDefault(t => t.Name == typeName);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }

    public static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        SerializationBinder = HadesTypesBinder,
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public static T Deserialize<T>(string data)
    {
        return JsonConvert.DeserializeObject<T>(data, Settings);
    }
}