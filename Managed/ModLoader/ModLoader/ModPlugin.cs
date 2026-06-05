using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace ModLoader;
public abstract class ModPlugin
{
    internal static List<ModPlugin> LoadFrom(string Path)
    {
        var plugins = new List<ModPlugin>();
        try
        {
            var assembly = Assembly.LoadFrom(Path);
            RegisterTypeInIl2Cpp.RegisterAssembly(assembly);
            foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
            {
                if (typeof(ModPlugin).IsAssignableFrom(type) && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null)
                {
                    if (Activator.CreateInstance(type) is ModPlugin instance)
                        plugins.Add(instance);
                }
            }
        }
        catch (Exception e)
        {
            Core.Logger.Error("Failed to load mod assembly: " + e);
        }
        return plugins;
    }
    public string Name { get; }
    public abstract void OnLoad();
    public string Path { get; internal set; }
}
[AttributeUsage(AttributeTargets.Class)]
public class RegisterTypeInIl2Cpp(params Type[] interfaces) : Attribute
{
    readonly Type[] Interfaces = interfaces;
    public static void RegisterAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            RegisterTypeInIl2Cpp? attr = type.GetCustomAttribute<RegisterTypeInIl2Cpp>();
            if (attr != null)
            {
                ClassInjector.RegisterTypeInIl2Cpp(type, new RegisterTypeOptions()
                {
                    Interfaces = attr.Interfaces,
                    LogSuccess = true
                });
            }
        }
    }
}