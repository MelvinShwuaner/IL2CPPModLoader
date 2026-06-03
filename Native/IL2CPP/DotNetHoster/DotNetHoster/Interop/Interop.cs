using System.Runtime.InteropServices;

namespace DotNet.Interop;
using static DotNet.Core;
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void Init();
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr IntPtrFromIntPtr(IntPtr a);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr IntPtrFromTwoIntPtr(IntPtr a, IntPtr b);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr IntPtrFromThreeIntPtr(IntPtr a, IntPtr b, IntPtr c);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void VoidFromIntPtr(IntPtr a);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void VoidFromTwoIntPtr(IntPtr a, IntPtr b);
/// <summary>
/// this class utilizes the Entry Point dll
/// </summary>
public class System
{
    public static string EntryPointPath { get; private set; }
    public static void Start(string EntryPointPath)
    {
        System.EntryPointPath =  EntryPointPath;
        GetMethod<Init>(EntryPointPath, "EntryPoint, EntryPoint", "Init")();
        _getAssembly    = GetMethod<IntPtrFromIntPtr>   (EntryPointPath, "Reflection, EntryPoint", "GetAssembly");
        _loadAssembly   = GetMethod<IntPtrFromIntPtr>   (EntryPointPath, "Reflection, EntryPoint", "LoadAssembly");
        _getAssemblyType= GetMethod<IntPtrFromTwoIntPtr>(EntryPointPath, "Reflection, EntryPoint", "GetAssemblyType");
        _getType        = GetMethod<IntPtrFromIntPtr>   (EntryPointPath, "Reflection, EntryPoint", "GetType");
        _getTypeObject  = GetMethod<IntPtrFromIntPtr>   (EntryPointPath, "Reflection, EntryPoint", "GetTypeFromObject");
        _getMethod      = GetMethod<IntPtrFromTwoIntPtr>(EntryPointPath, "Reflection, EntryPoint", "GetMethod");
        _createInstance = GetMethod<IntPtrFromIntPtr>   (EntryPointPath, "Reflection, EntryPoint", "CreateInstance");
        _invokeMethod   = GetMethod<IntPtrFromThreeIntPtr>(EntryPointPath, "Reflection, EntryPoint", "InvokeMethod");
        _releaseHandle  = GetMethod<VoidFromIntPtr>     (EntryPointPath, "Reflection, EntryPoint", "ReleaseHandle");
        _enumerateTypes = GetMethod<VoidFromTwoIntPtr>(EntryPointPath, "Reflection, EntryPoint", "EnumerateTypes");
        _enumerateMethods = GetMethod<VoidFromTwoIntPtr>(EntryPointPath, "Reflection, EntryPoint", "EnumerateMethods");
        
        
        
        
    }
    static IntPtrFromIntPtr      _getAssembly;
    static IntPtrFromIntPtr      _loadAssembly;
    static IntPtrFromTwoIntPtr   _getAssemblyType;
    static IntPtrFromIntPtr      _getType;
    static IntPtrFromIntPtr      _getTypeObject;
    static IntPtrFromTwoIntPtr   _getMethod;
    static IntPtrFromIntPtr      _createInstance;
    static IntPtrFromThreeIntPtr _invokeMethod;
    static VoidFromIntPtr        _releaseHandle;
    static VoidFromTwoIntPtr _enumerateTypes;
    static VoidFromTwoIntPtr _enumerateMethods;
    public static IntPtr GetAssembly(string name)
    {
        var ptr = Marshal.StringToHGlobalAnsi(name);
        var result = _getAssembly(ptr);
        Marshal.FreeHGlobal(ptr);
        return result;
    }

    public static IntPtr LoadAssembly(string path)
    {
        var ptr = Marshal.StringToHGlobalAnsi(path);
        var result = _loadAssembly(ptr);
        Marshal.FreeHGlobal(ptr);
        return result;
    }

    public static IntPtr GetAssemblyType(IntPtr assembly, string name)
    {
        var ptr = Marshal.StringToHGlobalAnsi(name);
        var result = _getAssemblyType(assembly, ptr);
        Marshal.FreeHGlobal(ptr);
        return result;
    }
    public static void EnumerateTypes(IntPtr assembly, Action<MonoType> callback)
    {
        // Pin the callback so GC doesn't move it
        var cb = (IntPtr typeHandle) => callback(new MonoType(typeHandle));
        var fnPtr = Marshal.GetFunctionPointerForDelegate(cb);
        _enumerateTypes(assembly, fnPtr);
    }
    public static void EnumerateMethods(IntPtr type, Action<MonoMethod> callback)
    {
        // Pin the callback so GC doesn't move it
        var cb = (IntPtr handle) => callback(new MonoMethod(handle));
        var fnPtr = Marshal.GetFunctionPointerForDelegate(cb);
        _enumerateMethods(type, fnPtr);
    }
    
    public static IntPtr GetType(string name)
    {
        var ptr = Marshal.StringToHGlobalAnsi(name);
        var result = _getType(ptr);
        Marshal.FreeHGlobal(ptr);
        return result;
    }
    public static IntPtr GetType(MonoObject obj)
    {
        var result = _getTypeObject(obj.Ptr);
        return result;
    }
    public static IntPtr GetMethod(IntPtr type, string name)
    {
        var ptr = Marshal.StringToHGlobalAnsi(name);
        var result = _getMethod(type, ptr);
        Marshal.FreeHGlobal(ptr);
        return result;
    }

    public static IntPtr CreateInstance(IntPtr type)   => _createInstance(type);
    public static IntPtr InvokeMethod(IntPtr method, IntPtr instance, IntPtr args) => _invokeMethod(method, instance, args);
    public static void   ReleaseHandle(IntPtr handle)  => _releaseHandle(handle);
}
public class MonoHandle : IDisposable
{
    public IntPtr Ptr { get; private set; }
    private bool _disposed;

    public MonoHandle(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            throw new InvalidOperationException("Invalid handle");
        Ptr = ptr;
    }

    public void Dispose()
    {
        if (_disposed) return;
        System.ReleaseHandle(Ptr);
        Ptr = IntPtr.Zero;
        _disposed = true;
    }
}