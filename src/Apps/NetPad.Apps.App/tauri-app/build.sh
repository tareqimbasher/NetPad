#!/usr/bin/env bash

if ! command -v npm &> /dev/null
then
    echo "npm could not be found. Executing 'nvm use 18'"
    nvm use 18
fi

echo "Building .NET App"
cd ../
dotnet publish -r linux-x64 -c "Release" --no-self-contained /p:PublishReadyToRun=true /p:PublishSingleFile=false /p:WebBuild=true

echo "Building Tauri App"
cd tauri-app
npx tauri build



#export WEBKIT_DISABLE_DMABUF_RENDERER=1
