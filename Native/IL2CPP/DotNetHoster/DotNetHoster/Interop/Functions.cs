using System.Runtime.InteropServices;

namespace DotNet.Interop;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr IntPtrFromIntPtr(IntPtr a);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr IntPtrFromTwoIntPtr(IntPtr a, IntPtr b);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr IntPtrFromThreeIntPtr(IntPtr a, IntPtr b, IntPtr c);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void VoidFromIntPtr(IntPtr a);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void VoidFromTwoIntPtr(IntPtr a, IntPtr b);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void VoidFromThreeIntPtr(IntPtr a, IntPtr b, IntPtr c);