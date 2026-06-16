cmake_minimum_required(VERSION 3.22)
project(DotNetPlugin)

if(ANDROID)
    add_library(DotNetPlugin SHARED
            dotnet_android.cpp
            assetmanager.cpp
    )
else ()
    add_library(DotNetPlugin SHARED
            dotnet_ios.cpp
            assetmanager.cpp
    )
endif ()

target_include_directories(DotNetPlugin PRIVATE
        ${CMAKE_SOURCE_DIR}/include
)

if(ANDROID)
    target_link_libraries(DotNetPlugin
            android
            log
    )
else ()
    set_target_properties(DotNetPlugin PROPERTIES
            XCODE_ATTRIBUTE_ENABLE_BITCODE "NO"
            XCODE_ATTRIBUTE_CODE_SIGNING_REQUIRED "NO"
            XCODE_ATTRIBUTE_CODE_SIGN_IDENTITY ""
            OSX_ARCHITECTURES "arm64"
            FRAMEWORK TRUE
            MACOSX_FRAMEWORK_IDENTIFIER com.melvin.shwuaner
            VERSION 1.0.0
            SOVERSION 1.0.0
    )

    target_link_libraries(DotNetPlugin
            "-framework Foundation"
            "-framework UIKit"
    )
endif ()

target_compile_features(DotNetPlugin PRIVATE cxx_std_17)