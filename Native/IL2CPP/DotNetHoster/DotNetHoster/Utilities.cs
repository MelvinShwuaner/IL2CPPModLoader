using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace DotNet
{

    public class Utilities
    {
        public static void ExtractFromStreamingAssets(string ZipPath, string Destination)
        {
            if (Directory.Exists(Destination))
            {
                return;
            }
            string zipUrl = Path.Combine(Application.streamingAssetsPath, ZipPath);
            #if UNITY_ANDROID

            using var req = UnityWebRequest.Get(zipUrl);
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to read {ZipPath}: {req.error}");
            }

            string tempZip = Path.Combine(Application.temporaryCachePath, "temp.zip");
            File.WriteAllBytes(tempZip, req.downloadHandler.data);

            ZipFile.ExtractToDirectory(tempZip, Destination);

            File.Delete(tempZip);
            #else
            ZipFile.ExtractToDirectory(zipUrl, Destination);
            #endif
        }

        public static string InternalFilesDir
        {
            get
            {
#if UNITY_ANDROID
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        using var filesDir = activity.Call<AndroidJavaObject>("getFilesDir");
        return filesDir.Call<string>("getAbsolutePath");
#else
                return Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Frameworks");
#endif
            }
        }
    }
    public enum LoadAssemblyError : int
    {
        #if UNITY_ANDROID
        // Win32 HRESULTs
        FileNotFound = unchecked((int)0x80070002),
        PathNotFound = unchecked((int)0x80070003),
        InvalidArgument = unchecked((int)0x80070057),

        // COM / CLR generic errors
        E_FAIL = unchecked((int)0x80004005),

        // .NET exceptions surfaced through HRESULT
        ManagedException = unchecked((int)0x80131500),
        AssemblyLoadFailure = unchecked((int)0x80131515),
        RuntimeInitializationFailure = unchecked((int)0x80131700),

        // Type / reflection errors
        TypeLoadException = unchecked((int)0x80131522),
        FileLoadException = unchecked((int)0x80131513),

        // Assembly binding errors
        AssemblyVersionMismatch = unchecked((int)0x80131040),
        AssemblyReferenceLoadFailure = unchecked((int)0x80131018),
        #else
        InvalidArgument = unchecked((int)0x80070057),
        AssemblyNotFound = unchecked((int)0x80131522),
        TypeNotFound = unchecked((int)0x80131510),
        MethodNotFound = unchecked((int)0x80131511),
        SignatureMismatch = unchecked((int)0x80131515),
        UnspecifiedFailure = unchecked((int)0x80004005),
        #endif
    }
    public enum HostError : int
    {
        #if UNITY_ANDROID
        FailedToLoadHostFxr = -100,


        // hostfxr_initialize_for_runtime_config / hostfxr_get_runtime_delegate errors

        InvalidArg = unchecked((int)0x80070057),

        FileNotFound = unchecked((int)0x80070002),
        PathNotFound = unchecked((int)0x80070003),

        Fail = unchecked((int)0x80004005),


        // hostfxr errors

        CoreHostLibLoadFailure = unchecked((int)0x80008083),
        CoreHostLibMissingFailure = unchecked((int)0x80008084),
        CoreHostEntryPointFailure = unchecked((int)0x80008085),

        CoreHostResolveFailure = unchecked((int)0x80008087),
        CoreHostBindFailure = unchecked((int)0x80008088),

        CoreClrResolveFailure = unchecked((int)0x8000808B),


        // runtimeconfig problems

        InvalidConfig = unchecked((int)0x80008093),
        FrameworkMissingFailure = unchecked((int)0x80008096),


        // hosting API errors

        HostApiBufferTooSmall = unchecked((int)0x80008098),
        HostInvalidState = unchecked((int)0x80008099),
        HostPropertyNotFound = unchecked((int)0x8000809A),


        // .NET runtime loading

        HostIncompatibleConfig = unchecked((int)0x800080A0),
        HostApiFailed = unchecked((int)0x800080A1),
        #else
        DlopenFailed = 100,
        MissingExports = 10,
    
        // coreclr_initialize HRESULTs
        InvalidArgument = unchecked((int)0x80070057),
        GenericRuntimeError = unchecked((int)0x80131500),
        UnspecifiedFailure = unchecked((int)0x80004005),
        OutOfMemory = unchecked((int)0x8007000E),
        #endif
    }
}