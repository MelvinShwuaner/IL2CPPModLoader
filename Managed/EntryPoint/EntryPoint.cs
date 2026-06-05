using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using ModLoader;

public static class EntryPoint
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void Init(IntPtr _, IntPtr logger)
    {
       Core.PreStart(
           Marshal.GetDelegateForFunctionPointer<LoggerNative>(logger), 
           Log,
           MainPath
       );
       //rn i will do it here
       Core.Start();
    }
    private static readonly string MainPath = Path.GetDirectoryName(typeof(EntryPoint).Assembly.Location)!;
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
            var result = Check(MainPath, name.Name!, context);
            if (result != null)
                return result;
            Log("Failed to load " + name.Name + ".dll");
            return null;
        };
    }
    static Assembly? Check(string Path, string Name, AssemblyLoadContext context)
    {
        foreach (var directory in Directory.GetDirectories(Path))
        {
            var result = Check(directory, Name, context);
            if (result != null)
            {
                return result;
            }
        }
        string path = System.IO.Path.Combine(Path, Name + ".dll");
        if (!File.Exists(path)) return null;
        try
        {
            var assemblyName = AssemblyName.GetAssemblyName(path);
            if (!string.Equals(assemblyName.Name, Name, 
                    StringComparison.OrdinalIgnoreCase))
            {
                Log($"Skipping {path} — assembly name mismatch: {assemblyName.Name}");
                return null;
            }
            Log($"Resolved {Name} at {path}");
            return context.LoadFromAssemblyPath(path);
        }
        catch (Exception e)
        {
            Log($"Failed to load {path}: {e.Message}");
            return null;
        }
    }
    private static readonly string LogPath = MainPath + "/latest.log";
    private static readonly string PreviousLog = MainPath + "/previous.log";
    static void Log(string msg)
    {
        File.AppendAllText(LogPath, msg + "\n");
    }
}
