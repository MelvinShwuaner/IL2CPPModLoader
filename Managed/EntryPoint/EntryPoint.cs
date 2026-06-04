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
            if(File.Exists(PreviousLog))
                File.Delete(PreviousLog);
            File.Copy(LogPath, PreviousLog);
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
    private static readonly string LogPath = DataPath + "/latest.log";
    private static readonly string PreviousLog = DataPath + "/previous.log";
    static void Log(string msg)
    {
        File.AppendAllText(LogPath, msg + "\n");
    }
}
