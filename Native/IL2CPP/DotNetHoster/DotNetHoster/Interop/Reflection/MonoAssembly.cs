namespace DotNet.Interop;

public class MonoAssembly : MonoHandle
{
    public MonoAssembly(string name) 
        : base(System.GetAssembly(name)) { }

    public static MonoAssembly LoadFrom(string path)
        => new MonoAssembly(System.LoadAssembly(path));

    public MonoType GetType(string name)
        => new MonoType(System.GetAssemblyType(Ptr, name));

    private MonoAssembly(IntPtr ptr) : base(ptr) { }

    public IEnumerable<MonoType> GetTypes()
    {
        List<MonoType> types = new List<MonoType>();
        System.EnumerateTypes(Ptr, type => types.Add(type));
        return types;
    }
}