#!/usr/bin/env bash

###
### Used to push the NetPad CLI (npad) .nupkg files to NuGet.org
###

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
NUPKG_DIR="$SCRIPT_DIR/../nupkgs"

if [ -z "$SCRIPT_DIR" ]; then
  echo "Could not determine script dir"
  exit 1
fi

if [ -z "$NUPKG_DIR" ]; then
  echo "Could not find nupkg dir"
  exit 1
fi

echo "DEST_DIR = $NUPKG_DIR"

dotnet nuget push "$NUPKG_DIR/*.nupkg" \
  --api-key ${NUGET_API_KEY} \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
