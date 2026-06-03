using System.IO.Compression;
using System.Runtime.InteropServices;

namespace DotNet;
public class Core
{
    [DllImport("DotNetPlugin", EntryPoint = "IsHosting")]
    private static extern int IsHostingInternal();

    [DllImport("DotNetPlugin")]
    private static extern int Host(string dotNetPath);

    [DllImport("DotNetPlugin")]
    private static extern int LoadMethod(string assemblyPath, string typeName, string methodName,
        out IntPtr outFunction);

    public static bool IsHosting => IsHostingInternal() == 1;

    public static void HostDotNet()
    {
        if (IsHosting)
        {
            throw new NotSupportedException("DotNet is already running");
        }

        int result = Host(dotnetRoot);
        if (result != 0)
        {
            throw new InvalidOperationException("Failed to Host DotNet!");
        }
    }

    public static T GetMethod<T>(string AssemblyPath, string TypeName, string MethodName) where T : Delegate
    {
        if (!IsHosting)
        {
            throw new NotSupportedException("DotNet is not running!");
        }

        if (LoadMethod(AssemblyPath, TypeName, MethodName, out var fnPtr) != 0)
        {
            throw new MissingMemberException($"{AssemblyPath}::{TypeName}::{MethodName} was not found!");
        }

        return Marshal.GetDelegateForFunctionPointer<T>(fnPtr);
    }

    public static string dotnetRoot => Path.Combine(Utilities.InternalFilesDir, "dotnet");

    public static void PrepareDotNet(string ZipPath)
    {
        Utilities.ExtractFromStreamingAssets(ZipPath, dotnetRoot);
    }
}