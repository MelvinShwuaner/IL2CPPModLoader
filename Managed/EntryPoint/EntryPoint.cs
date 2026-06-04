using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using ModLoader;

public static class EntryPoint
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init(IntPtr DataPath, IntPtr logger)
    {
       Core.Start(
           Marshal.GetDelegateForFunctionPointer<LoggerNative>(logger), 
           Log,
           EntryPoint.DataPath, 
           Path.GetDirectoryName(Marshal.PtrToStringAnsi(DataPath))!
       );
    }
    private static readonly string DataPath = Path.GetDirectoryName(typeof(EntryPoint).Assembly.Location)!;
    static EntryPoint()
    {
        if (File.Exists(LogPath))
        {
            File.Delete(DataPath + "/previous.log");
            File.Copy(LogPath, DataPath + "/previous.log");
            File.Delete(LogPath);
        }
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            string path = Path.Combine(DataPath, name.Name + ".dll");
            if (File.Exists(path))
                return context.LoadFromAssemblyPath(path);
            Log($"DLL NOT FOUND: {path}");
            return null;
        };
    }
    private static string LogPath = DataPath + "/latest.log";
    static void Log(string msg)
    {
        File.AppendAllText(LogPath, msg + "\n");
    }
}
