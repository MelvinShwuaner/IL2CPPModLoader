using System;
using System.Runtime.InteropServices;
using AOT;

namespace DotNet
{
    using static DotNet.Core;

    public enum MsgType
    {
        Message = 0,
        Warning = 1,
        Error = 2
    }
    public delegate void Logger(string msg, MsgType Type);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void log(IntPtr msg, int Type);
    /// <summary>
    /// this class utilizes the Entry Point dll
    /// </summary>
    /// <remarks>it is possible for users to have their own custom entry point as long as they have the static methods PreStart and Start</remarks>
    public class System
    {
        private static Logger Logger;

        [MonoPInvokeCallback(typeof(log))]
        static void Log(IntPtr msg, int Type)
        {
            string message = Marshal.PtrToStringAnsi(msg)!;
            Logger(message, (MsgType)Type);
        }

        public static string EntryPointPath { get; private set; }

        public static void Start(string EntryPointPath, Logger Logger)
        {
            System.Logger = Logger;
            var log = Marshal.GetFunctionPointerForDelegate((log)Log);
            GetMethod<VoidFromIntPtr>(EntryPointPath, "EntryPoint", "PreStart")(log);
            GetMethod<Void>(EntryPointPath, "EntryPoint", "Start")();

            System.EntryPointPath = EntryPointPath;
        }
    }
}