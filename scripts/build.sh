#!/usr/bin/env bash

if ! command -v npm &> /dev/null
then
    echo "npm could not be found. Executing 'nvm use 18'"
    nvm use 18
fi

unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)     platform=Linux;;
    Darwin*)    platform=Mac;;
    *)          platform="${unameOut}"
esac

echo "Detected platform: ${platform}"

if [ "${platform}" == "Linux" ]; then
    echo "Building Tauri app for Linux..."
    npx tauri build -c src-tauri/tauri.conf.linux-x64.json5 --target x86_64-unknown-linux-gnu
elif [ "${platform}" == "Mac" ]; then
    echo "Building Tauri app for Mac..."
    npx tauri build -c src-tauri/tauri.conf.macos.json5 --target aarch64-apple-darwin
fi


# CHANGE Port

# Windows:
# npx tauri build -c src-tauri/tauri.conf.win-x64.json5 --target x86_64-pc-windows-msvc

# Linux:
# npx tauri build -c src-tauri/tauri.conf.linux-x64.json5 --target x86_64-unknown-linux-gnu
# Had to zip a standalone: zip -r "standalone.zip" "netpad-vnext" "resources"

# macOS x64:
# npx tauri build -c src-tauri/tauri.conf.mac-x64.json5 --target x86_64-apple-darwin

# macOS arm64:
# npx tauri build -c src-tauri/tauri.conf.mac-arm64.json5 --target aarch64-apple-darwin
