using System.Runtime.InteropServices;
using AssetRipper.Primitives;
using Cpp2IL.Core;
using Microsoft.Extensions.Logging;

namespace ModLoader;
public class Core
{
    public static UnityVersion UnityVersion { get; private set; }
    public static string MainPath{ get; private set; }
    public static string DataPath { get; private set; }
    public static void Start(LoggerNative logger, Action<string> LoggerManaged, string MainPath, string DataPath)
    {
        Logger.native = logger;
        Logger.managed = LoggerManaged;
        Core.MainPath =  MainPath;
        Core.DataPath = DataPath;
        Logger.Msg("DotNet: Initializing");
        
        UnityVersion = GetVersion();
        Logger.Msg("UnityVersion: " + UnityVersion);
        
        Il2CppAssemblyGenerator.Prepare();
    }

    static UnityVersion GetVersion()
    {
        byte[] game = AssetManager.ReadAssetBytes("bin/Data/globalgamemanagers");
        if (game != null)
        {
            return Cpp2IlApi.GetVersionFromGlobalGameManagers(game);
        }
        game = AssetManager.ReadAssetBytes("bin/Data/data.unity3d");
        using var stream = new MemoryStream(game);
        return Cpp2IlApi.GetVersionFromDataUnity3D(stream);
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
                case LogLevel.Trace:
                case LogLevel.Debug: Msg(formatter(state, exception)); break;
                case LogLevel.Critical:
                case LogLevel.Error: Error(formatter(state, exception)); break;
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

