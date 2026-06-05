using System.Reflection;
using System.Runtime.InteropServices;
using AssetRipper.Primitives;
using Il2CppInterop.Common;
using Il2CppInterop.HarmonySupport;
using Il2CppInterop.Runtime.Startup;
using LibCpp2IL;
using Microsoft.Extensions.Logging;

namespace ModLoader;
public class Core
{
    public static UnityVersion UnityVersion { get; private set; }
    internal static Dictionary<string, ModPlugin> Plugins = new  Dictionary<string, ModPlugin>();
    internal static void PreStart(LoggerNative logger, Action<string> LoggerManaged, string MainPath)
    {
        Logger.native = logger;
        Logger.managed = LoggerManaged;
        Enviornment.MainPath =  MainPath;
        Logger.Msg("DotNet: Initializing");
        
        UnityVersion = GetVersion();
        Logger.Msg("UnityVersion: " + UnityVersion);
        
        Il2CppInteropRuntime.Create(new RuntimeConfiguration
            {
                UnityVersion = new Version(UnityVersion.Major, UnityVersion.Minor, UnityVersion.Build),
                DetourProvider = new NativeDetourProvider()
            })
            .AddLogger(Logger.Instance)
            .AddHarmonySupport()
            .Start();
    }
    internal static void Start()
    {
        if (!Directory.Exists(Enviornment.PluginsPath))
        {
            Directory.CreateDirectory(Enviornment.PluginsPath);
            return;
        }
        foreach (var File in Directory.GetFiles(Enviornment.PluginsPath).Where(s => s.EndsWith(".dll")))
        {
            LoadPlugin(File);
        }
    }
    public static void LoadPlugin(string Path)
    {
        var plugins = ModPlugin.LoadFrom(Path);
        if (plugins.Count == 0)
        {
            Logger.Msg("Plugins not found: " + Path);
        }
        foreach (var Plugin in plugins)
        {
            if (!Plugins.TryAdd(Plugin.Name, Plugin))
            {
                Logger.Msg($"plugin name conflict: {Plugin.Name} between {Path} and {Plugins[Plugin.Name].Path}");
            }
            else
            {
                Plugin.Path = Path;
                try
                {
                    Plugin.OnLoad();
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to load plugin {Plugin.Name}: {e.Message}");
                }
            }
        }
    }
    static UnityVersion GetVersion()
    {
        byte[]? game = AssetManager.ReadAssetBytes("bin/Data/globalgamemanagers");
        if (game != null)
        {
            return LibCpp2IlMain.GetVersionFromGlobalGameManagers(game);
        }
        game = AssetManager.ReadAssetBytes("bin/Data/data.unity3d")!;
        return LibCpp2IlMain.GetVersionFromDataUnity3D(new MemoryStream(game));
    }
    public class Logger : ILogger
    {
        internal static LoggerNative native;
        internal static Action<string> managed;
        public static void Log(string message, MsgType Type =  MsgType.Message)
        {
            IntPtr ptr = Marshal.StringToHGlobalAnsi(message);
            native(ptr, (int)Type);
            managed(Type + ": " + message);
            Marshal.FreeHGlobal(ptr);
        }
        public static void Msg(string message)
        {
            Log(message);
        }
        public static void Warning(string message)
        {
            Log(message, MsgType.Warning);
        }
        public static void Error(string message)
        {
            Log(message, MsgType.Error);
        }
        public static readonly Logger Instance = new Logger();
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Information:
                case LogLevel.Debug: Msg(formatter(state, exception)); break;
                case LogLevel.Critical:
                case LogLevel.Error: Error(formatter(state, exception)); break;
                case LogLevel.Trace:
                case LogLevel.Warning: Warning(formatter(state, exception)); break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel !=  LogLevel.None;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }
    }
}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void LoggerNative(IntPtr msg, int Type);
public enum MsgType
{
    Message = 0,
    Warning = 1,
    Error = 2
}

