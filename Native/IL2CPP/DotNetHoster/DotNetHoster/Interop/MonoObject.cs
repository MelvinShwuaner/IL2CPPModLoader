namespace DotNet.Interop;

public class MonoObject : MonoHandle
{
    public MonoObject(IntPtr ptr) : base(ptr) { }

    public MonoType GetType()
    {
        return new MonoType(System.GetType(this));
    }
}