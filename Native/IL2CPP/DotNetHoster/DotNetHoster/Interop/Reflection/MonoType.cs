namespace DotNet.Interop;

public class MonoType : MonoHandle
{
    public MonoType(string name)
        : base(System.GetType(name)) { }

    public MonoType(IntPtr ptr) : base(ptr) { }

    public MonoMethod GetMethod(string name)
        => new MonoMethod(System.GetMethod(Ptr, name));
    
    public IEnumerable<MonoMethod> GetMethods()
    {
        List<MonoMethod> types = new List<MonoMethod>();
        System.EnumerateMethods(Ptr, m => types.Add(m));
        return types;
    }
    public MonoObject CreateInstance()
        => new MonoObject(System.CreateInstance(Ptr));
}