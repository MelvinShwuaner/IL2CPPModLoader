using System.Collections;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace DotNetHoster;

public class Hoster
{
    [DllImport("DotNetPlugin")]
    static extern int Host(
        string runtimeConfigPath,
        string dotNetPath,
        string entryPointType,
        string entryPointFunction);

    public static void HostDotNet(string entryPointDLL, string entryPointType, string entryPointFunction)
    {
        string runtimeConfig = Path.Combine(dotnetRoot, "runtimeconfig.json");
        int result = Host(
            runtimeConfig,
            dotnetRoot,
            $"{entryPointType}, {entryPointDLL}",
            entryPointFunction);
        if (result != 0)
        {
            throw new InvalidProgramException("Failed to Host DotNet!");
        }
    }
    public static string dotnetRoot => Path.Combine(Application.persistentDataPath, "dotnet");
    public static IEnumerator ExtractDotNet(string ZipPath)
    {
        if (Directory.Exists(dotnetRoot))
            yield break;
        
        string zipUrl = Path.Combine(Application.streamingAssetsPath, ZipPath);
    
        using var req = UnityWebRequest.Get(zipUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to read {ZipPath}: {req.error}");
            yield break;
        }

        // Write zip to temp location
        string tempZip = Path.Combine(Application.temporaryCachePath, "dotnet.zip");
        File.WriteAllBytes(tempZip, req.downloadHandler.data);

        // Extract
        ZipFile.ExtractToDirectory(tempZip, dotnetRoot);

        // Clean up temp zip
        File.Delete(tempZip);
    
        Debug.Log("dotnet extracted");
    }
}