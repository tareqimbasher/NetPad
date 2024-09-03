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
    npx tauri build -c src-tauri/tauri.conf.linux-x64.json5
elif [ "${platform}" == "Mac" ]; then
    echo "Building Tauri app for Mac..."
    # npx tauri build -c src-tauri/tauri.conf.macos.json5
fi

#export WEBKIT_DISABLE_DMABUF_RENDERER=1
