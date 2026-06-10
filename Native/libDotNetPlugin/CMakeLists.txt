cmake_minimum_required(VERSION 3.22)
project(DotNetPlugin)

add_library(DotNetPlugin SHARED
        dotnet.cpp
        assetmanager.cpp
)

target_include_directories(DotNetPlugin PRIVATE
        ${CMAKE_SOURCE_DIR}/include
)

if(CMAKE_SYSTEM_NAME STREQUAL "iOS")
    set_target_properties(DotNetPlugin PROPERTIES
            XCODE_ATTRIBUTE_ENABLE_BITCODE "NO"
            XCODE_ATTRIBUTE_CODE_SIGNING_REQUIRED "NO"
            XCODE_ATTRIBUTE_CODE_SIGN_IDENTITY ""
            OSX_ARCHITECTURES "arm64"
    )

    target_link_libraries(DotNetPlugin
            "-framework Foundation"
            "-framework UIKit"
    )

elseif(ANDROID)
    target_link_libraries(DotNetPlugin
            android
            log
    )
endif()

target_compile_features(DotNetPlugin PRIVATE cxx_std_17)