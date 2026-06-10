#if defined(__APPLE__)
#include <dlfcn.h>
#include <objc/objc.h>
#include <objc/runtime.h>
#include <objc/message.h>
#include <sys/syslimits.h>
#define MAX_PATH PATH_MAX
#define LOG(...) printf(__VA_ARGS__)
#define hostfxr "/libhostfxr.dylib"
#elif defined(__ANDROID__)
#include <android/log.h>
#define LOG(...) __android_log_print(ANDROID_LOG_ERROR, "DotNetPlugin", __VA_ARGS__)
#define MAX_PATH PATH_MAX
#define hostfxr "/libhostfxr.so"
#endif
#include <assert.h>
#include <chrono>
#include <iostream>
#include "coreclr_delegates.h"
#include "hostfxr.h"
#include <dlfcn.h>
#define STR(s) s
#define CH(c) c
#define string_compare strcmp

namespace
{
    // Globals to hold hostfxr exports
    hostfxr_initialize_for_runtime_config_fn init_for_config_fptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr;
    hostfxr_close_fn close_fptr;

    // Forward declarations
    bool load_hostfxr(const char_t *app);
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly();
}
static bool Hosted = false;
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
            return EXIT_FAILURE;
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
            return EXIT_FAILURE;
        }
        load_assembly = get_dotnet_load_assembly();

        if (load_assembly == nullptr)
        {
            LOG("Failed to get load_assembly delegate");
            return EXIT_FAILURE;
        }
        Hosted = true;
        return EXIT_SUCCESS;
    }
    int LoadMethod(
        const char* AssemblyPath,
        const char* TypeName,
        const char* MethodName,
        void** OutFunction)
    {
        int rc = load_assembly(
            AssemblyPath,
            TypeName,
            MethodName,
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            OutFunction
        );

        if (rc != 0 || *OutFunction == nullptr)
        {
            LOG("Failed to get function: 0x%x", rc);
            return EXIT_FAILURE;
        }
        return EXIT_SUCCESS;
    }
    int IsHosting() {
        return Hosted;
    }
}


/********************************************************************************************
 * Function used to load and activate .NET Core.
 ********************************************************************************************/
namespace
{
    // Forward declarations
    void *load_library(const char_t *);
    void *get_export(void *, const char *);

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
        std::string str = std::string(dotnet_root) + hostfxr;

        // Load hostfxr and get desired exports
        // NOTE: The .NET Runtime does not support unloading any of its native libraries. Running
        // dlclose/FreeLibrary on any .NET libraries produces undefined behavior.
        void *lib = load_library(str.c_str());
        init_for_config_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
        get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
        close_fptr = (hostfxr_close_fn)get_export(lib, "hostfxr_close");

        return (init_for_config_fptr && get_delegate_fptr && close_fptr);
    }
    // </SnippetLoadHostFxr>

    // <SnippetInitialize>
    // Load and initialize .NET Core and get desired function pointer for scenario
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly()
    {
        // Load .NET Core
        void *load_assembly_and_get_function_pointer = nullptr;
        // Get the load assembly function pointer
        int rc = get_delegate_fptr(
            Context,
            hdt_load_assembly_and_get_function_pointer,
            &load_assembly_and_get_function_pointer);
        if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
            std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;

        return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
    }
    // </SnippetInitialize>
}