using System.Collections.Concurrent;
using System.Reflection;

namespace Darkages.ScriptingBase;

public static class ScriptManager
{
    private static readonly Dictionary<string, Type> Scripts = [];

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

    /// <summary>
    /// Loads a script of the specified type and returns a concurrent dictionary containing the script instance, keyed
    /// by the provided value.
    /// </summary>
    /// <typeparam name="TScript">The type of script to load. Must be a reference type.</typeparam>
    /// <param name="values">The key used to identify and retrieve the script. Cannot be null.</param>
    /// <param name="args">An array of arguments to pass to the script's constructor.</param>
    /// <returns>A ConcurrentDictionary containing the loaded script instance keyed by the specified value, or null if the value
    /// is null or the script cannot be found.</returns>
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

    /// <summary>
    /// Creates an instance of the specified script type using the given script name and arguments, or returns null if
    /// creation fails.
    /// </summary>
    /// <typeparam name="TScript">The type of script to create. Must be a reference type.</typeparam>
    /// <param name="scriptName">The name of the script to instantiate. Cannot be null or empty.</param>
    /// <param name="args">An array of arguments to pass to the script's constructor or initialization method.</param>
    /// <returns>An instance of type TScript if the script is successfully created; otherwise, null.</returns>
    public static TScript? CreateOrNull<TScript>(string scriptName, params object[] args) where TScript : class
    {
        return TryCreate<TScript>(scriptName, out var instance, args) ? instance : null;
    }

    /// <summary>
    /// Attempts to create an instance of the specified script type by name, using the provided constructor arguments.
    /// </summary>
    /// <remarks>This method does not throw an exception if the script type cannot be found or instantiated.
    /// Instead, it returns false and sets instance to null. The script type must be registered and accessible by the
    /// provided name.</remarks>
    /// <typeparam name="TScript">The type of script to create. Must be a reference type.</typeparam>
    /// <param name="scriptName">The name of the script type to instantiate. Cannot be null.</param>
    /// <param name="instance">When this method returns, contains the created instance of type TScript if successful; otherwise, null.</param>
    /// <param name="args">An array of arguments to pass to the constructor of the script type.</param>
    /// <returns>true if the script instance was successfully created and assigned to instance; otherwise, false.</returns>
    public static bool TryCreate<TScript>(string scriptName, out TScript? instance, params object[] args) where TScript : class
    {
        instance = null;

        if (string.IsNullOrWhiteSpace(scriptName))
            return false;

        if (!Scripts.TryGetValue(scriptName, out var scriptType) || scriptType == null)
            return false;

        if (!typeof(TScript).IsAssignableFrom(scriptType))
            return false;

        try
        {
            object? obj;

            try
            {
                obj = Activator.CreateInstance(scriptType, args);
            }
            catch (MissingMethodException)
            {
                // Fallback to parameterless
                obj = Activator.CreateInstance(scriptType);
            }

            instance = obj as TScript;
            return instance != null;
        }
        catch
        {
            instance = null;
            return false;
        }
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ScriptAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}