using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class EntryPoint
{
    public static string MainPath{ get; private set; }
    public static string DataPath { get; private set; }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init(IntPtr DataPath, IntPtr Log)
    {
       DotNet.Log("Initializing DotNet");
       EntryPoint.DataPath = Path.GetDirectoryName(Marshal.PtrToStringAnsi(DataPath))!;
       MainPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
       Logger = Marshal.GetDelegateForFunctionPointer<Logger>(Log);
    }
    public static Logger Logger;
}
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void Logger(string msg);