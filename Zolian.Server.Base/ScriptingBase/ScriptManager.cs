using System.Collections.Concurrent;
using System.Reflection;

namespace Darkages.ScriptingBase;

public static class ScriptManager
{
    private static readonly Dictionary<string, Type> Scripts = new();

    static ScriptManager()
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttributes(typeof(ScriptAttribute), false).Cast<ScriptAttribute>().FirstOrDefault();

            if (attribute == null) continue;

            Scripts.Add(attribute.Name, type);
        }
    }

    public static ConcurrentDictionary<string, TScript> Load<TScript>(string values, params object[] args)
        where TScript : class
    {
        if (values == null) return null;

        var data = new ConcurrentDictionary<string, TScript>();

        Scripts.TryGetValue(values, out var script);
        if (script == null) return null;

        var instance = Activator.CreateInstance(script, args);
        data[values] = instance as TScript;

        return data;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ScriptAttribute : Attribute
{
    public ScriptAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}