using AssetRipper.Primitives;
using Cpp2IL.Core;

namespace ModLoader;
public class Core
{
    public static UnityVersion UnityVersion { get; private set; }
    public static void Prepare()
    {
        byte[] game = AssetManager.ReadAssetBytes("bin/Data/globalgamemanagers");
        UnityVersion = Cpp2IlApi.GetVersionFromGlobalGameManagers(game);
    }

    public static class Logger
    {
        public static void Msg(string message)
        {
            EntryPoint.Logger(message);
        }
        public static void Warning(string message)
        {
            EntryPoint.Logger("Warning: " + message);
        }
        public static void Error(string message)
        {
            EntryPoint.Logger("Error: " + message);
        }
    }
}

