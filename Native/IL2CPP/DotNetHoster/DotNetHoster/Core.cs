using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DotNet
{
    public class Core
    {
        private const string Name =
#if UNITY_ANDROID
            "DotNetPlugin";
#else
            "__Internal";
            #endif  
        [DllImport(Name)]
        private static extern void SetAssetManager(IntPtr assetManager);

        [DllImport(Name, EntryPoint = "IsHosting")]
        private static extern int IsHostingInternal();

        [DllImport(Name)]
        private static extern int Host(string dotNetPath);

        [DllImport(Name)]
        private static extern int LoadMethod(string assemblyPath, string typeName, string methodName,
            out IntPtr outFunction);

        /// <summary>
        /// is DotNet being hosted?
        /// </summary>
        public static bool IsHosting => IsHostingInternal() == 1;

        /// <summary>
        /// Hosts DotNet. 
        /// </summary>
        /// <exception cref="NotSupportedException">DotNet is already running!</exception>
        /// <exception cref="InvalidOperationException">dotnet was failed to be hosted</exception>
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

        /// <summary>
        /// loads an assembly and returns a function pointer
        /// </summary>
        /// <param name="AssemblyPath">the full path to the DLL</param>
        /// <param name="TypeName">the Type Name. "namespace.class"</param>
        /// <param name="MethodName">the name of the method</param>
        /// <typeparam name="T">the delegate type. must be a UnManagedCallersOnly method</typeparam>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">DotNet is not being hosted</exception>
        /// <exception cref="MissingMemberException">the method, dll, or class was failed to be found</exception>
        public static T GetMethod<T>(string AssemblyPath, string TypeName, string MethodName) where T : Delegate
        {
            if (!IsHosting)
            {
                throw new NotSupportedException("DotNet is not running!");
            }

            string DllName = Path.GetFileNameWithoutExtension(AssemblyPath);
            if (LoadMethod(AssemblyPath, TypeName + ", " + DllName, MethodName, out var fnPtr) != 0)
            {
                throw new MissingMemberException($"{AssemblyPath}::{TypeName}::{MethodName} was not found!");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(fnPtr);
        }

        public static string dotnetRoot => Path.Combine(Utilities.InternalFilesDir, "dotnet");

        public static void PrepareDotNet(string ZipPath)
        {
#if UNITY_ANDROID
            Utilities.ExtractFromStreamingAssets(ZipPath, dotnetRoot);
#endif
        }

        /// <summary>
        /// loads the unity asset manager to be used by the mod loader
        /// </summary>
        public static void InitAssetManager()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        using var assets = activity.Call<AndroidJavaObject>("getAssets");
        SetAssetManager(assets.GetRawObject());
#else
            nint result = Marshal.StringToHGlobalAnsi(Application.dataPath);
            SetAssetManager(result);
            Marshal.FreeHGlobal(result);
#endif
        }
    }
}