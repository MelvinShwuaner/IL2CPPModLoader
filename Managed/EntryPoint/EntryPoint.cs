using System.Runtime.InteropServices;

public static class EntryPoint
{
    [UnmanagedCallersOnly]
    public static void Init()
    {
        DotNet.Log("hello world!");
        File.WriteAllText("/storage/emulated/0/Android/data/com.UnityTechnologies.com.unity.template.urpblank/files/mynigga", "yo!");
    }
}