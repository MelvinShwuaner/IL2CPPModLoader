#include <cstdlib>
#if defined(__ANDROID__)
#include <android/asset_manager.h>
#include <android/asset_manager_jni.h>
#include <jni.h>
#include <android/log.h>
#define LOG(...) __android_log_print(ANDROID_LOG_ERROR, "DotNetPlugin", __VA_ARGS__)
static AAssetManager* g_assetManager = nullptr;
static JavaVM* g_jvm = nullptr;

JNIEXPORT jint JNI_OnLoad(JavaVM* vm, void* reserved)
{
    g_jvm = vm;
    return JNI_VERSION_1_6;
}
extern "C" {
void SetAssetManager(jobject assetManager)
{
    JNIEnv* env = nullptr;
    g_jvm->AttachCurrentThread(&env, nullptr);
    g_assetManager = AAssetManager_fromJava(env, assetManager);
}
void* ReadAsset(const char* path, int* outSize)
{
    if (g_assetManager == nullptr)
    {
        LOG("AssetManager not set");
        return nullptr;
    }

    AAsset* asset = AAssetManager_open(g_assetManager, path, AASSET_MODE_BUFFER);
    if (asset == nullptr)
    {
        LOG("Asset not found: %s", path);
        return nullptr;
    }

    *outSize = AAsset_getLength(asset);
    void* buffer = malloc(*outSize);
    AAsset_read(asset, buffer, *outSize);
    AAsset_close(asset);

    return buffer;
}
}
#else
#include <sys/syslimits.h>
#define MAX_PATH PATH_MAX
#define LOG(...) NSLog(@__VA_ARGS__)
static char g_dataPath[PATH_MAX] = {};
void SetAssetManager(const char* dataPath)
{
    strncpy(g_dataPath, dataPath, PATH_MAX - 1);
}
void* ReadAsset(const char* path, int* outSize)
{
    char fullPath[PATH_MAX];
    snprintf(fullPath, PATH_MAX, "%s/%s", g_dataPath, path);

    FILE* f = fopen(fullPath, "rb");
    if (!f) { LOG("File not found: %s", fullPath); return nullptr; }

    fseek(f, 0, SEEK_END);
    *outSize = ftell(f);
    fseek(f, 0, SEEK_SET);

    void* buffer = malloc(*outSize);
    fread(buffer, 1, *outSize, f);
    fclose(f);
    return buffer;
}
#endif
extern "C"
{
void FreeAssetBuffer(void* buffer)
{
    free(buffer);
}
}