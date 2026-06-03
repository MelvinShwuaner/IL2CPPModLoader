using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

public class DotNet
{

    [DllImport("DotNetPlugin", EntryPoint="IsHosting")]
    private static extern int IsHostingInternal();
    [DllImport("DotNetPlugin")]
    private static extern int Host(string dotNetPath);
    [DllImport("DotNetPlugin")]
    private static extern int LoadMethod(string assemblyPath, string typeName, string methodName, out IntPtr outFunction);
    
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
public class Utilities
{
    public static void ExtractFromStreamingAssets(string ZipPath, string Destination)
    {
        if (Directory.Exists(Destination))
        {
            return;
        }
        
        string zipUrl = Path.Combine(Application.streamingAssetsPath, ZipPath);

        using var req = UnityWebRequest.Get(zipUrl);
        var op = req.SendWebRequest();
        while (!op.isDone) { }
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to read {ZipPath}: {req.error}");
        }

        // Write zip to temp location
        string tempZip = Path.Combine(Application.temporaryCachePath, "temp.zip");
        File.WriteAllBytes(tempZip, req.downloadHandler.data);

        // Extract
        ZipFile.ExtractToDirectory(tempZip, Destination);

        // Clean up temp zip
        File.Delete(tempZip);

        Debug.Log("dotnet extracted");
    }
    public static string InternalFilesDir
    {
        get
        {
            /*using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var filesDir = activity.Call<AndroidJavaObject>("getFilesDir");

            return filesDir.Call<string>("getAbsolutePath");*/
            var temp = "assa";
            return "" + temp;
        }
    }
}