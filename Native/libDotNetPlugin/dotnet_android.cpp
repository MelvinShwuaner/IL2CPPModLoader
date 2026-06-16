#include <android/log.h>
#define LOG(...) __android_log_print(ANDROID_LOG_ERROR, "DotNetPlugin", __VA_ARGS__)
#include <chrono>
#include <iostream>
#include <dlfcn.h>
#define STR(s) s
#define CH(c) c
#define string_compare strcmp
#include "dotnet.cpp"
#define HOSTFXR_CALLTYPE
#define CORECLR_DELEGATE_CALLTYPE
typedef char char_t;
#define UNMANAGEDCALLERSONLY_METHOD ((const char_t*)-1)
typedef void* hostfxr_handle;
enum hostfxr_delegate_type
{
    hdt_com_activation,
    hdt_load_in_memory_assembly,
    hdt_winrt_activation,
    hdt_com_register,
    hdt_com_unregister,
    hdt_load_assembly_and_get_function_pointer,
    hdt_get_function_pointer,
    hdt_load_assembly,
    hdt_load_assembly_bytes,
};
typedef int (CORECLR_DELEGATE_CALLTYPE *load_assembly_and_get_function_pointer_fn)(
    const char_t *assembly_path      /* Fully qualified path to assembly */,
    const char_t *type_name          /* Assembly qualified type name */,
    const char_t *method_name        /* Public static method name compatible with delegateType */,
    const char_t *delegate_type_name /* Assembly qualified delegate type name or null
                                        or UNMANAGEDCALLERSONLY_METHOD if the method is marked with
                                        the UnmanagedCallersOnlyAttribute. */,
    void         *reserved           /* Extensibility parameter (currently unused and must be 0) */,
    /*out*/ void **delegate          /* Pointer where to store the function pointer result */);
namespace
{
    typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_initialize_for_runtime_config_fn)(
    const char_t *runtime_config_path,
    const struct hostfxr_initialize_parameters *parameters,
    /*out*/ hostfxr_handle *host_context_handle);

    typedef int32_t(HOSTFXR_CALLTYPE *hostfxr_get_runtime_delegate_fn)(
    const hostfxr_handle host_context_handle,
    enum hostfxr_delegate_type type,
    /*out*/ void **delegate);

    // Globals to hold hostfxr exports
    hostfxr_initialize_for_runtime_config_fn init_for_config_fptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr;

    // Forward declarations
    bool load_hostfxr(const char_t *app);
    int get_dotnet_load_assembly(void** load_assembly_and_get_function_pointer);
}
static const char* RuntimeConfigPath;
static hostfxr_handle Context = nullptr;
static load_assembly_and_get_function_pointer_fn load_assembly;
extern "C"
{
    int Host(
        const char* DotNetPath)
    {
        // Load hostfxr and get exports
        if (!load_hostfxr(DotNetPath))
        {
            LOG("Failed to load hostfxr");
            return -100;
        }
        static std::string RuntimeConfigPathStr;

        RuntimeConfigPathStr = std::string(DotNetPath) + "/runtimeconfig.json";
        RuntimeConfigPath = RuntimeConfigPathStr.c_str();
        int rc = init_for_config_fptr(
            RuntimeConfigPath,
            nullptr,
            &Context);

        if (rc != 0 || Context == nullptr)
        {
            LOG("hostfxr_initialize_for_runtime_config failed: 0x%x", rc);
            return rc;
        }
        rc = get_dotnet_load_assembly(reinterpret_cast<void**>(&load_assembly));

        if (rc != 0)
        {
            LOG("Failed to get load_assembly delegate");
            return rc;
        }
        Hosted = true;
        return EXIT_SUCCESS;
    }
    int LoadMethod(
        const char* AssemblyName,
        const char* TypeName,
        const char* MethodName,
        void** OutFunction)
    {
        return load_assembly(
            AssemblyName,
            TypeName,
            MethodName,
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            OutFunction
        );
    }
}


/********************************************************************************************
 * Function used to load and activate .NET Core.
 ********************************************************************************************/
namespace
{
    void *load_library(const char_t *path)
    {
        void *h = dlopen(path, RTLD_LAZY | RTLD_LOCAL);
        if (h == nullptr)
            LOG("dlopen failed for %s: %s", path, dlerror());
        return h;
    }
    void *get_export(void *h, const char *name)
    {
        void *f = dlsym(h, name);
        if (f == nullptr)
            LOG("dlsym failed for %s: %s", name, dlerror());
        return f;
    }

    // <SnippetLoadHostFxr>
    // Using the nethost library, discover the location of hostfxr and get exports
    bool load_hostfxr(const char_t* dotnet_root)
    {
        std::string str = std::string(dotnet_root) + "/libhostfxr.so";

        // Load hostfxr and get desired exports
        // NOTE: The .NET Runtime does not support unloading any of its native libraries. Running
        // dlclose/FreeLibrary on any .NET libraries produces undefined behavior.
        void *lib = load_library(str.c_str());
        init_for_config_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
        get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");

        return (init_for_config_fptr && get_delegate_fptr);
    }
    // </SnippetLoadHostFxr>

    // <SnippetInitialize>
    // Load and initialize .NET Core and get desired function pointer for scenario
    int get_dotnet_load_assembly(void** load_assembly_and_get_function_pointer)
    {
        // Load .NET Core
        *load_assembly_and_get_function_pointer = nullptr;
        // Get the load assembly function pointer
        int rc = get_delegate_fptr(
            Context,
            hdt_load_assembly_and_get_function_pointer,
            load_assembly_and_get_function_pointer);
        return rc;
    }
    // </SnippetInitialize>
}