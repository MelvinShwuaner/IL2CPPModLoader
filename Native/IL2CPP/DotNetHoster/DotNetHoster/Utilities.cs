using System.Collections;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace DotNet;

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