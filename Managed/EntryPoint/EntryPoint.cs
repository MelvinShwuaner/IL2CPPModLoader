using System.Runtime.InteropServices;

public static class EntryPoint
{
    [UnmanagedCallersOnly]
    public static void Init()
    {
       DotNet.Log("Hello World!");
    }
    
}