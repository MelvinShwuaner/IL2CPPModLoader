extern alias Game;
using ModLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using Config = Game::Config;

namespace WorldBoxCompatibilityModule;

public class WrappedPlugin : ModPlugin
{
    private Type type;
    public WrappedPlugin(Type pluginType, string name)
    {
        Name = name;
        type = pluginType;
    }
    public override string Name { get; }
    public override void OnLoad()
    {
       GameObject obj = new GameObject(Name);
       obj.transform.parent = GameObject.Find("ModLoader").transform;
       obj.AddComponent(Il2CppType.From(type));
       Game::ModLoader.modsLoaded.Add(Name);
       Config.MODDED = true;
    }
}