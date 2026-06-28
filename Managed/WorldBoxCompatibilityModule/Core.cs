using System.Reflection;
using HarmonyLib;
using ModLoader;
using Module = ModLoader.Module;

namespace WorldBoxCompatibilityModule;
//example of a module
public class ModLoader : Module
{
    public override string Name { get => "WorldBoxCompatibilityModule"; }
    public override void OnLoad()
    {
      
    }

    public override IEnumerable<ModPlugin> LoadPlugin(Assembly assembly)
    {
        string filename = System.IO.Path.GetFileNameWithoutExtension(assembly.Location);
        foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
        {
            if (type.Name == $"{filename}.WorldBoxMod")
            {
                yield return new WrappedPlugin(type, filename);
            }
        }
    }
}