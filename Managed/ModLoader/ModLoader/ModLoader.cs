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
            EntryPoint.Log(message);
        }
        public static void Warning(string message)
        {
            EntryPoint.Log(message, MsgType.Warning);
        }
        public static void Error(string message)
        {
            EntryPoint.Log(message, MsgType.Error);
        }
    }
}

