namespace DotNet.Interop;

public class MonoMethod : MonoHandle
{
    public MonoMethod(IntPtr ptr) : base(ptr) { }

    public MonoObject? Invoke(MonoObject instance, MonoObject args = null)
    {
        var result = System.InvokeMethod(
            Ptr,
            instance?.Ptr ?? IntPtr.Zero,
            args?.Ptr ?? IntPtr.Zero
        );
        return result == IntPtr.Zero ? null : new MonoObject(result);
    }
}