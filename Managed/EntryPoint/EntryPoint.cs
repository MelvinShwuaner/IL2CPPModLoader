using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class EntryPoint
{
    public static string MainPath;
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init()
    {
       DotNet.Log("Hello World!");
       MainPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    }
}