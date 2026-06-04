public class ModLoader
{
    public static void Prepare()
    {
        
    }
}

public class Il2CppAssemblyGenerator
{
    static string MainPath => EntryPoint.MainPath + "/Il2CppAssemblies";
    public static void Prepare()
    {
        if (Path.Exists(MainPath))
        {
            return;
        }
        
    }
}