using System.Reflection;
using HarmonyLib;

namespace ModLoader;

public abstract class BaseMod
{
    public abstract string Name { get; }
    public abstract void OnLoad();
    public string Path { get; internal set; }
}
public abstract class Module : BaseMod
{
    internal static List<Module> LoadFrom(string Path)
    {
        var modules = new List<Module>();
        try
        {
            var assembly = Assembly.LoadFrom(Path);
            foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
            {
                if (typeof(Module).IsAssignableFrom(type) && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    if (Activator.CreateInstance(type) is Module instance)
                        modules.Add(instance);
                }
            }
        }
        catch (Exception e)
        {
            Core.Logger.Error("Failed to load module assembly: " + e);
        }
        return modules;
    }
    public abstract IEnumerable<ModPlugin> LoadPlugin(Assembly assembly);
}