using System.Runtime.InteropServices;
using UnityEngine;

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
        string dotnetRoot    = Path.Combine(Application.persistentDataPath, "dotnet");
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
}