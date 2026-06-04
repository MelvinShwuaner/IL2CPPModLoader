using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class EntryPoint
{
    public static string MainPath{ get; private set; }
    public static string DataPath { get; private set; }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init(IntPtr DataPath, IntPtr logger)
    {
       EntryPoint.DataPath = Path.GetDirectoryName(Marshal.PtrToStringAnsi(DataPath))!;
       MainPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
       Logger = Marshal.GetDelegateForFunctionPointer<Logger>(logger);
       Log("Dotnet: Initializing");
    }
    static Logger Logger;
    public static void Log(string message, MsgType Type =  MsgType.Message)
    {
        IntPtr ptr = Marshal.StringToHGlobalAnsi(message);
        Logger(ptr, (int)Type);
        Marshal.FreeHGlobal(ptr);
    }
}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void Logger(IntPtr msg, int Type);
public enum MsgType
{
    Message = 0,
    Warning = 1,
    Error = 2
}