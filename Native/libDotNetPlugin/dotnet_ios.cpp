#import <Foundation/Foundation.h>
#import <dispatch/dispatch.h>
#import <dlfcn.h>
#include <filesystem>
#include "dotnet.cpp"
namespace fs = std::filesystem;
#define LOG(...) printf(__VA_ARGS__)
typedef int (*coreclr_initialize_ptr)(
    const char *exePath, const char *appDomainFriendlyName, int propertyCount,
    const char **propertyKeys, const char **propertyValues, void **hostHandle,
    unsigned int *domainId);

typedef int (*coreclr_create_delegate_ptr)(
    void *hostHandle, unsigned int domainId, const char *assemblyName,
    const char *typeName, const char *methodName, void **delegate);

typedef void (*coreclr_set_error_writer_ptr)(void (*)(const char *));

// BepInEx entry point delegate type

static void *g_hostHandle = NULL;
static unsigned int g_domainId = 0;
static void CoreClrErrorWriter(const char *message) {
  LOG("[CoreCLR] %s", message ? message : "(null)");
}
static std::string BuildTpaList(fs::path dotnetRoot) {
    std::string tpaList = std::string();

    fs::path dlls = dotnetRoot / "net10.0";
    for (const auto& entry : fs::directory_iterator(dlls)) {
        tpaList.append(fs::absolute(entry.path()).string() + ";");
    }
    return tpaList;
}
coreclr_create_delegate_ptr createDelegate;
static std::string AppPaths;
extern "C" {
void SetAppPaths(const char* paths) {
    AppPaths = std::string(paths);
}
int LoadMethod(
        const char* AssemblyName,
        const char* TypeName,
        const char* MethodName,
        void** OutFunction) {
  int delegateHr =
      createDelegate(g_hostHandle, g_domainId, AssemblyName,
                     TypeName, MethodName,
                     OutFunction);
  return delegateHr;
}
int Host(const char* DotNetPath) {
  fs::path my_path = DotNetPath;
  fs::path executablePath = my_path.parent_path().parent_path();
  fs::path coreclr = my_path / "libcoreclr.dylib";
  void *coreclrHandle = dlopen(coreclr.c_str(), RTLD_NOW | RTLD_LOCAL);
  if (!coreclrHandle) {
    LOG("[Mod][CoreCLR] dlopen failed: %s", dlerror());
    return 100;
  }

  coreclr_initialize_ptr initialize =
      (coreclr_initialize_ptr)dlsym(coreclrHandle, "coreclr_initialize");
  createDelegate =
      (coreclr_create_delegate_ptr)dlsym(coreclrHandle,
                                         "coreclr_create_delegate");
  coreclr_set_error_writer_ptr setErrorWriter =
      (coreclr_set_error_writer_ptr)dlsym(coreclrHandle,
                                          "coreclr_set_error_writer");

  if (!initialize || !createDelegate) {
    LOG("[Mod][CoreCLR] Missing required CoreCLR exports");
    return 10;
  }

  if (setErrorWriter) {
    setErrorWriter(CoreClrErrorWriter);
  }

  const std::string trustedAssemblies = BuildTpaList(my_path);

  const char *propertyKeys[] = {"TRUSTED_PLATFORM_ASSEMBLIES",
                                "APP_PATHS",
                                "NATIVE_DLL_SEARCH_DIRECTORIES"};

  const char *propertyValues[] = {trustedAssemblies.c_str(),
                                  AppPaths.c_str(),
                                  DotNetPath };

  const char* exePath = executablePath.c_str();
  int initHr =
      initialize(exePath, "DotNet", 3, propertyKeys,
                 propertyValues, &g_hostHandle, &g_domainId);

  if (initHr < 0) {
    return initHr;
  }

  LOG("[Mod][CoreCLR] initialized (domain %u)", g_domainId);
  Hosted = true;
  return 0;
}
}