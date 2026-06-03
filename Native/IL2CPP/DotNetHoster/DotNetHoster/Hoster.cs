using System.Runtime.InteropServices;

namespace DotNet
{
    public class Core
    {
        public static bool IsHosting => IsHostingInternal() == 1;

        [DllImport("DotNetPlugin", EntryPoint="IsHosting")]
        static extern int IsHostingInternal();
        [DllImport("DotNetPlugin")]
        static extern int Host(
            string runtimeConfigPath,
            string dotNetPath,
            string EntryPointPath);
        
        public static void HostDotNet(string EntryPointPath)
        {
            string runtimeConfig = Path.Combine(dotnetRoot, "runtimeconfig.json");
            int result = Host(
                runtimeConfig,
                dotnetRoot,
                EntryPointPath);
            if (result != 0)
            {
                throw new InvalidOperationException("Failed to Host DotNet!");
            }
        }
        public static string dotnetRoot => Path.Combine(Utilities.InternalFilesDir, "dotnet");
        public static void PrepareDotNet(string ZipPath)
        {
            Utilities.ExtractFromStreamingAssets(ZipPath, dotnetRoot);
        }
    }
}