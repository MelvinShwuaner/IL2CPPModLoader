#import <Foundation/Foundation.h>
#import <dispatch/dispatch.h>
#import <dlfcn.h>
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
typedef void (*bepinex_start_delegate)(void);

static void *g_hostHandle = NULL;
static unsigned int g_domainId = 0;
static bepinex_start_delegate g_chainloaderDelegate = NULL;
static bool g_chainloaderCalled = false;

static void CoreClrErrorWriter(const char *message) {
  LOG("[Mod][CoreCLR] %s", message ? message : "(null)");
}


static const char* BuildTpaList(const char* coreClrRoot,
                              const char* managedAssemblyPath) {
  NSMutableArray<NSString *> *tpaEntries =
      [NSMutableArray arrayWithObject:managedAssemblyPath];
  NSFileManager *fileManager = [NSFileManager defaultManager];

  // Add BCL assemblies from CoreCLR
  NSString *bclPath = [coreClrRoot stringByAppendingPathComponent:@"net10.0"];
  NSArray<NSString *> *bclFiles = [fileManager contentsOfDirectoryAtPath:bclPath
                                                                   error:nil];
  for (NSString *file in bclFiles) {
    if ([file hasSuffix:@".dll"]) {
      [tpaEntries addObject:[bclPath stringByAppendingPathComponent:file]];
    }
  }

  // Add BepInEx core assemblies to TPA
  NSString *bepinexCorePath = ResolveBepInExCorePath();
  if (bepinexCorePath) {
    NSArray<NSString *> *coreFiles =
        [fileManager contentsOfDirectoryAtPath:bepinexCorePath error:nil];
    for (NSString *file in coreFiles) {
      if ([file hasSuffix:@".dll"]) {
        NSString *fullPath =
            [bepinexCorePath stringByAppendingPathComponent:file];
        // Avoid duplicates
        if (![tpaEntries containsObject:fullPath]) {
          [tpaEntries addObject:fullPath];
        }
      }
    }
  }

  return [tpaEntries componentsJoinedByString:@":"];
}
coreclr_create_delegate_ptr createDelegate;
extern "C" {
int LoadMethod(
        const char* AssemblyPath,
        const char* TypeName,
        const char* MethodName,
        void** OutFunction) {

}
}
int Host(const char* DotNetPath) {

  void *coreclrHandle = dlopen(DotNetPath, RTLD_NOW | RTLD_LOCAL);
  if (!coreclrHandle) {
    LOG("[Mod][CoreCLR] dlopen failed: %s", dlerror());
    return -100;
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
    return -10;
  }

  if (setErrorWriter) {
    setErrorWriter(CoreClrErrorWriter);
  }

  const char* trustedAssemblies = BuildTpaList(coreClrRoot, managedAssemblyPath);

  // Build native search paths including BepInEx core directory
  NSMutableArray<NSString *> *nativeSearchPathsArr = [NSMutableArray array];
  [nativeSearchPathsArr addObject:coreClrRoot];
  [nativeSearchPathsArr
      addObject:[[[NSBundle mainBundle] bundlePath]
                    stringByAppendingPathComponent:@"Frameworks"]];

  NSString *bepinexCorePath = ResolveBepInExCorePath();
  if (bepinexCorePath) {
    [nativeSearchPathsArr addObject:bepinexCorePath];
  }

  const char* nativeSearchPaths =
      [nativeSearchPathsArr componentsJoinedByString:@":"];

  // Build APP_PATHS to include BepInEx core
  NSMutableArray<NSString *> *appPathsArr = [NSMutableArray array];
  [appPathsArr addObject:coreClrRoot];
  if (bepinexCorePath) {
    [appPathsArr addObject:bepinexCorePath];
  }
  const char* appPaths = [appPathsArr componentsJoinedByString:@":"];

  const char *propertyKeys[] = {"TRUSTED_PLATFORM_ASSEMBLIES", "APP_PATHS",
                                "APP_NI_PATHS",
                                "NATIVE_DLL_SEARCH_DIRECTORIES"};

  const char *propertyValues[] = {trustedAssemblies,
                                  appPaths,
                                  appPaths,
                                  nativeSearchPaths };

  const char* exePath = [[[NSBundle mainBundle] executablePath]
      stringByDeletingLastPathComponent];
  int initHr =
      initialize(exePath, "DotNet", 4, propertyKeys,
                 propertyValues, &g_hostHandle, &g_domainId);

  if (initHr < 0) {
    LOG("[Mod][CoreCLR] coreclr_initialize failed: 0x%08X", initHr);
    return -1;
  }

  LOG("[Mod][CoreCLR] initialized (domain %u)", g_domainId);

  // Call BepInEx entry point via coreclr_create_delegate
  LOG("[Mod][CoreCLR] Loading BepInEx entry point...");

  bepinex_start_delegate startDelegate = NULL;
  int delegateHr =
      createDelegate(g_hostHandle, g_domainId, "BepInEx.Unity.IL2CPP",
                     "BepInEx.Unity.IL2CPP.StarlightEntrypoint", "StartIOS",
                     (void **)&startDelegate);

  if (delegateHr < 0 || !startDelegate) {
    LOG("[Mod][CoreCLR] coreclr_create_delegate failed: 0x%08X", delegateHr);
    LOG("[Mod][CoreCLR] Make sure BepInEx.Unity.IL2CPP.dll is in "
          @"Documents/BepInEx/core/");
    return;
  }

  LOG("[Mod][CoreCLR] Calling StarlightEntrypoint.StartIOS()");
  startDelegate();
  LOG("[Mod][CoreCLR] BepInEx preloader started");

  // Get chainloader delegate but DON'T call it yet
  // It will be called when Internal_ActiveSceneChanged is invoked
  int chainloaderHr =
      createDelegate(g_hostHandle, g_domainId, "BepInEx.Unity.IL2CPP",
                     "BepInEx.Unity.IL2CPP.StarlightEntrypoint", "StartChainloader",
                     (void **)&g_chainloaderDelegate);

  if (chainloaderHr < 0 || !g_chainloaderDelegate) {
    LOG("[Mod][CoreCLR] Failed to get StartChainloader delegate: 0x%08X", chainloaderHr);
    return;
  }

  LOG("[Mod][CoreCLR] Chainloader delegate ready, waiting for scene load...");
}
}
