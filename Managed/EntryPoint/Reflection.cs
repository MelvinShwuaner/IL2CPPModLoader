using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

public static class Reflection
{
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr GetAssembly(IntPtr namePtr)
    {
        string name = Marshal.PtrToStringAnsi(namePtr);
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == name);
        if (assembly == null) return IntPtr.Zero;
        return GCHandle.ToIntPtr(GCHandle.Alloc(assembly));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr LoadAssembly(IntPtr pathPtr)
    {
        string path = Marshal.PtrToStringAnsi(pathPtr);
        var assembly = Assembly.LoadFrom(path);
        if (assembly == null) return IntPtr.Zero;
        return GCHandle.ToIntPtr(GCHandle.Alloc(assembly));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr GetAssemblyType(IntPtr assemblyHandle, IntPtr namePtr)
    {
        var assembly = (Assembly)GCHandle.FromIntPtr(assemblyHandle).Target;
        string name = Marshal.PtrToStringAnsi(namePtr);
        var type = assembly.GetType(name);
        if (type == null) return IntPtr.Zero;
        return GCHandle.ToIntPtr(GCHandle.Alloc(type));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr GetType(IntPtr namePtr)
    {
        string name = Marshal.PtrToStringAnsi(namePtr);
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name))
            .FirstOrDefault(t => t != null);
        if (type == null) return IntPtr.Zero;
        return GCHandle.ToIntPtr(GCHandle.Alloc(type));
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr GetTypeFromObject(IntPtr objectPtr)
    {
        var obj = GCHandle.FromIntPtr(objectPtr).Target;
        var type = obj.GetType();
        return GCHandle.ToIntPtr(GCHandle.Alloc(type));
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr GetMethod(IntPtr typeHandle, IntPtr namePtr)
    {
        var type = (Type)GCHandle.FromIntPtr(typeHandle).Target;
        string name = Marshal.PtrToStringAnsi(namePtr);
        var method = type.GetMethod(name,
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static);
        if (method == null) return IntPtr.Zero;
        return GCHandle.ToIntPtr(GCHandle.Alloc(method));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr CreateInstance(IntPtr typeHandle)
    {
        var type = (Type)GCHandle.FromIntPtr(typeHandle).Target;
        var instance = Activator.CreateInstance(type);
        return GCHandle.ToIntPtr(GCHandle.Alloc(instance));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static IntPtr InvokeMethod(IntPtr methodHandle, IntPtr instanceHandle, IntPtr argsHandle)
    {
        var method   = (MethodInfo)GCHandle.FromIntPtr(methodHandle).Target;
        var instance = instanceHandle == IntPtr.Zero ? null : GCHandle.FromIntPtr(instanceHandle).Target;
        var args     = argsHandle    == IntPtr.Zero ? null : (object[])GCHandle.FromIntPtr(argsHandle).Target;
        var result   = method.Invoke(instance, args);
        if (result == null) return IntPtr.Zero;
        return GCHandle.ToIntPtr(GCHandle.Alloc(result));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void ReleaseHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return;
        GCHandle.FromIntPtr(handle).Free();
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe void EnumerateTypes(IntPtr assemblyHandle, IntPtr callback)
    {
        var assembly = (Assembly)GCHandle.FromIntPtr(assemblyHandle).Target;
        var fn = (delegate* unmanaged<IntPtr, void>)callback;

        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types ?? Array.Empty<Type>(); }

        foreach (var type in types)
        {
            if (type == null) continue;
            var handle = GCHandle.ToIntPtr(GCHandle.Alloc(type));
            fn(handle);
        }
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe void EnumerateMethods(IntPtr typeHandle, IntPtr callback)
    {
        var assembly = (Type)GCHandle.FromIntPtr(typeHandle).Target;
        var fn = (delegate* unmanaged<IntPtr, void>)callback;

        MethodInfo[] methods;
        methods = assembly.GetMethods(); 
        foreach (var type in methods)
        {
            if (type == null) continue;
            var handle = GCHandle.ToIntPtr(GCHandle.Alloc(type));
            fn(handle);
        }
    }
}