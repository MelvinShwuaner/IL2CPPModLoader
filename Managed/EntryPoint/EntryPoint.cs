using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class EntryPoint
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init()
    {
       DotNet.Log("Hello World!");
    }
}