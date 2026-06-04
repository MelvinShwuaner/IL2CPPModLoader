#include <cstdlib>
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
extern "C"
{
void SetAssetManager(jobject assetManager)
{
    JNIEnv* env = nullptr;
    g_jvm->AttachCurrentThread(&env, nullptr);
    g_assetManager = AAssetManager_fromJava(env, assetManager);
}

// Returns malloc'd buffer — caller (mono) must free it
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

void FreeAssetBuffer(void* buffer)
{
    free(buffer);
}
}