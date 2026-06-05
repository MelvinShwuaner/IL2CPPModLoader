using System.Runtime.InteropServices;
using AOT;

namespace DotNet.Interop;
using static DotNet.Core;
public enum MsgType
{
    Message = 0,
    Warning = 1,
    Error = 2
}
/// <summary>
/// type 0 is msg, 1 is warning, 2 is error
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void Logger(string msg, MsgType Type);
/// <summary>
/// this class utilizes the Entry Point dll
/// </summary>
public class System
{
    private static Logger Logger;
    [MonoPInvokeCallback(typeof(Logger))]
    static void Log(IntPtr msg, int Type)
    {
        string message = Marshal.PtrToStringAnsi(msg)!;
        Logger(message, (MsgType)Type);
    }
    public static string EntryPointPath { get; private set; }
    public static void Start(string EntryPointPath, Logger Logger)
    {
        System.Logger = Logger;
        var log = Marshal.GetFunctionPointerForDelegate(Log);
        GetMethod<VoidFromIntPtr>(EntryPointPath, "EntryPoint", "PreStart")(log);
        GetMethod<Void>(EntryPointPath, "EntryPoint", "Start")();
        
        System.EntryPointPath =  EntryPointPath;
    }
}