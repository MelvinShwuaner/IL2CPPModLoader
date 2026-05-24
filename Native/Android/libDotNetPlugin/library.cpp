
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
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t *assembly);
}

extern "C"
{
    int Host(
        const char* RuntimeConfigPath,
        const char* DotNetPath,
        const char* EntryPointType,
        const char* EntryPointFunction)
    {
        // Load hostfxr and get exports
        if (!load_hostfxr(DotNetPath))
        {
            assert(false && "Failed to load hostfxr");
            return EXIT_FAILURE;
        }

        hostfxr_handle cxt = nullptr;

        int rc = init_for_config_fptr(
            RuntimeConfigPath,
            nullptr,
            &cxt);

        if (rc != 0 || cxt == nullptr)
        {
            LOG("hostfxr_initialize_for_runtime_config failed: 0x%x", rc);
            return EXIT_FAILURE;
        }
        get_function_pointer_fn get_function_pointer = nullptr;

        rc = get_delegate_fptr(
            cxt,
            hdt_get_function_pointer,
            (void**)&get_function_pointer);

        if (rc != 0 || get_function_pointer == nullptr)
        {
            std::cerr
                << "hostfxr_get_runtime_delegate failed: "
                << std::hex << std::showbase << rc
                << std::endl;

            return EXIT_FAILURE;
        }
        // Signature of managed method:
        //
        // [UnmanagedCallersOnly]
        // public static void Init()
        //
        typedef void (CORECLR_DELEGATE_CALLTYPE* entry_point_fn)();

        entry_point_fn entrypoint = nullptr;

        rc = get_function_pointer(
            EntryPointType,
            EntryPointFunction,
            UNMANAGEDCALLERSONLY_METHOD,
            nullptr,
            nullptr,
            (void**)&entrypoint);

        if (rc != 0 || entrypoint == nullptr)
        {
            std::cerr
                << "get_function_pointer failed: "
                << std::hex << std::showbase << rc
                << std::endl;

            return EXIT_FAILURE;
        }
        entrypoint();

        return EXIT_SUCCESS;
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
    load_assembly_and_get_function_pointer_fn get_dotnet_load_assembly(const char_t *config_path)
    {
        // Load .NET Core
        void *load_assembly_and_get_function_pointer = nullptr;
        hostfxr_handle cxt = nullptr;
        int rc = init_for_config_fptr(config_path, nullptr, &cxt);
        if (rc != 0 || cxt == nullptr)
        {
            std::cerr << "Init failed: " << std::hex << std::showbase << rc << std::endl;
            close_fptr(cxt);
            return nullptr;
        }

        // Get the load assembly function pointer
        rc = get_delegate_fptr(
            cxt,
            hdt_load_assembly_and_get_function_pointer,
            &load_assembly_and_get_function_pointer);
        if (rc != 0 || load_assembly_and_get_function_pointer == nullptr)
            std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;

        close_fptr(cxt);
        return (load_assembly_and_get_function_pointer_fn)load_assembly_and_get_function_pointer;
    }
    // </SnippetInitialize>
}