using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.Injection;

public static unsafe partial class Dobby
{
    [LibraryImport("dobby", EntryPoint = "DobbyHook")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Hook(nint target, nint detour, ref nint original);

    [LibraryImport("dobby", EntryPoint = "DobbyDestroy")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int Destroy(nint target);

    public static nint HookAttach(nint target, nint detour)
    {
        nint original = 0;
        if (Hook(target, detour, ref original) != 0)
        {
            throw new AccessViolationException($"Could not prepare patch to target {target:X}");
        }
        return original;
    }

    public static void HookDetach(nint target)
    {
        var result = Destroy(target);
        if (result is not 0 and not -1)
        {
            throw new AccessViolationException($"Could not destroy patch for target {target:X}");
        }
    }
}
/// <summary>
/// straight outa bepinex :fire: 
/// </summary>
public class NativeDetourProvider : IDetourProvider
{
    public class NativeDetour : IDetour
    {
        public IntPtr Target { get; }
        public IntPtr Detour { get; }
        public IntPtr OriginalTrampoline { get; private set; }

        public NativeDetour(IntPtr target, Delegate detour)
        {
            Target = target;
            Detour = Marshal.GetFunctionPointerForDelegate(detour);
            Apply();
        }

        public void Apply()
        {
            if (OriginalTrampoline != IntPtr.Zero)
            {
                return;
            }
            OriginalTrampoline = Dobby.HookAttach(Target, Detour);
        }
    
        public void Dispose()
        {
            if (OriginalTrampoline == IntPtr.Zero)
            {
                return;
            }
            Dobby.HookDetach(Target);
        }

        public T GenerateTrampoline<T>() where T : Delegate
        {
            return Marshal.GetDelegateForFunctionPointer<T>(OriginalTrampoline);
        }
    }
    public IDetour Create<TDelegate>(IntPtr original, TDelegate target) where TDelegate : Delegate
    {
        var detour = new NativeDetour(original, target);
        return new CacheDetourWrapper(detour, target);
    }
    internal class CacheDetourWrapper : IDetour
    {
        public IntPtr Target => wrapped.Target;
        public IntPtr Detour => wrapped.Detour;
        public IntPtr OriginalTrampoline => wrapped.OriginalTrampoline;

        private readonly IDetour wrapped;

        private readonly List<object> cache = [];

        public CacheDetourWrapper(IDetour wrapped, Delegate target)
        {
            this.wrapped = wrapped;
            cache.Add(target);
        }

        public void Apply()
        {
            wrapped.Apply();
        }

        public void Dispose()
        {
            wrapped.Dispose();
            cache.Clear();
        }

        public T GenerateTrampoline<T>() where T : Delegate
        {
            var trampoline = wrapped.GenerateTrampoline<T>();
            cache.Add(trampoline);
            return trampoline;
        }
    }
}