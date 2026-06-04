using System.Runtime.InteropServices;

public class AssetManager
{
    [DllImport("DotNetPlugin")]
    private static extern IntPtr ReadAsset(string path, out int size);

    [DllImport("DotNetPlugin")]
    private static extern void FreeAssetBuffer(IntPtr buffer);

    public static byte[]? ReadAssetBytes(string path)
    {
        IntPtr buffer = ReadAsset(path, out int size);
        if (buffer == IntPtr.Zero) return null;

        byte[] data = new byte[size];
        Marshal.Copy(buffer, data, 0, size);
        FreeAssetBuffer(buffer);
        return data;
    }
}