namespace ModLoader;

public static class Enviornment
{
    public static string MainPath { get; internal set; }
    public static string PluginsPath => Path.Combine(MainPath, "Plugins");

    public static ModPlugin? GetPlugin(string name)
    {
        return Core.Plugins.GetValueOrDefault(name);
    }
}