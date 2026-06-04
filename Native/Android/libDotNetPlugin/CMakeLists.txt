cmake_minimum_required(VERSION 3.22)
project(DotNetPlugin)

add_library(DotNetPlugin SHARED
        library.cpp
        assetmanager.cpp
)

target_include_directories(DotNetPlugin PRIVATE
        ${CMAKE_SOURCE_DIR}/include
)

target_link_libraries(DotNetPlugin PRIVATE
        ${CMAKE_SOURCE_DIR}/lib/android-arm64/libnethost.a
        android
        log
)

target_compile_features(DotNetPlugin PRIVATE cxx_std_17)