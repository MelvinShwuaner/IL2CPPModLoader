
using System.Runtime.InteropServices;

public static class DotNet
{
    [DllImport("DotNetPlugin")]
    public static extern void Log(string message);
}