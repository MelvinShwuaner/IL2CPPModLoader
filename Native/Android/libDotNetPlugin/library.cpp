
#include <stdio.h>
#include <assert.h>
#include <chrono>
#include <iostream>
#include "nethost.h"
#include "coreclr_delegates.h"
#include "hostfxr.h"
#include <dlfcn.h>
#include <android/log.h>
#define STR(s) s
#define CH(c) c
#define DIR_SEPARATOR '/'
#define MAX_PATH PATH_MAX
#define LOG(...) __android_log_print(ANDROID_LOG_ERROR, "DotNetPlugin", __VA_ARGS__)
#define string_compare strcmp

namespace
{
    // Globals to hold hostfxr exports
    hostfxr_initialize_for_dotnet_command_line_fn init_for_cmd_line_fptr;
    hostfxr_initialize_for_runtime_config_fn init_for_config_fptr;
    hostfxr_get_runtime_delegate_fn get_delegate_fptr;
    hostfxr_run_app_fn run_app_fptr;
    hostfxr_close_fn close_fptr;

    // Forward declarations
    bool load_hostfxr(const char_t *app);
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly();
    typedef void (CORECLR_DELEGATE_CALLTYPE* entry_point_fn)();
}
static bool Hosted = false;
static std::string RuntimeConfigPath;
static hostfxr_handle Context = nullptr;
static std::string EntryPointPath = nullptr;
extern "C"
{
    void Log(const char* message) {
        LOG("%s", message);
    }
    int Host(
        const char* DotNetPath,
        const char* EntryPointPath)
    {
        if (Hosted) {
            return EXIT_FAILURE;
        }
        // Load hostfxr and get exports
        if (!load_hostfxr(DotNetPath))
        {
            LOG("Failed to load hostfxr");
            return EXIT_FAILURE;
        }
        RuntimeConfigPath = std::string(DotNetPath) + "/runtimeconfig.json";
        int rc = init_for_config_fptr(
            RuntimeConfigPath.c_str(),
            nullptr,
            &Context);

        if (rc != 0 || Context == nullptr)
        {
            LOG("hostfxr_initialize_for_runtime_config failed: 0x%x", rc);
            return EXIT_FAILURE;
        }
        load_assembly_and_get_function_pointer_fn load_assembly_fn =
           get_dotnet_load_assembly();

        if (load_assembly_fn == nullptr)
        {
            LOG("Failed to get load_assembly delegate");
            return EXIT_FAILURE;
        }
        ::EntryPointPath = std::string(EntryPointPath);

        entry_point_fn entrypoint = nullptr;

        rc = load_assembly_fn(
            EntryPointPath,
            "",
            "Init",
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            reinterpret_cast<void **>(&entrypoint)
        );
        if (rc != 0 || entrypoint == nullptr) {
            return EXIT_FAILURE;
        }
        entrypoint();

        Hosted = true;

        return EXIT_SUCCESS;
    }
    int IsHosting() {
        return Hosted;
    }
}


/********************************************************************************************
 * Function used to load and activate .NET Core
 ********************************************************************************************/

namespace
{
    // Forward declarations
    void *load_library(const char_t *);
    void *get_export(void *, const char *);

    void *load_library(const char_t *path)
    {
        void *h = dlopen(path, RTLD_LAZY | RTLD_LOCAL);
        assert(h != nullptr);
        return h;
    }
    void *get_export(void *h, const char *name)
    {
        void *f = dlsym(h, name);
        assert(f != nullptr);
        return f;
    }

    // <SnippetLoadHostFxr>
    // Using the nethost library, discover the location of hostfxr and get exports
    bool load_hostfxr(const char_t* dotnet_root)
    {
        get_hostfxr_parameters params { sizeof(get_hostfxr_parameters), nullptr, dotnet_root };
        // Pre-allocate a large buffer for the path to hostfxr
        char_t buffer[MAX_PATH];
        size_t buffer_size = sizeof(buffer) / sizeof(char_t);
        int rc = get_hostfxr_path(buffer, &buffer_size, &params);
        if (rc != 0)
            return false;

        // Load hostfxr and get desired exports
        // NOTE: The .NET Runtime does not support unloading any of its native libraries. Running
        // dlclose/FreeLibrary on any .NET libraries produces undefined behavior.
        void *lib = load_library(buffer);
        init_for_cmd_line_fptr = (hostfxr_initialize_for_dotnet_command_line_fn)get_export(lib, "hostfxr_initialize_for_dotnet_command_line");
        init_for_config_fptr = (hostfxr_initialize_for_runtime_config_fn)get_export(lib, "hostfxr_initialize_for_runtime_config");
        get_delegate_fptr = (hostfxr_get_runtime_delegate_fn)get_export(lib, "hostfxr_get_runtime_delegate");
        run_app_fptr = (hostfxr_run_app_fn)get_export(lib, "hostfxr_run_app");
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