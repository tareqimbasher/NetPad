#!/usr/bin/env bash

###
### Used to pack the NetPad CLI (npad)
###

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
PROJ_DIR="$SCRIPT_DIR/../src/Apps/NetPad.Apps.Cli"
DEST_DIR="$SCRIPT_DIR/../nupkgs"

if [ -z "$SCRIPT_DIR" ]; then
  echo "Could not determine script dir"
  exit 1
fi

if [ -z "$PROJ_DIR" ]; then
  echo "Could not determine project dir"
  exit 1
fi

echo "PROJ_DIR = $PROJ_DIR"
echo "DEST_DIR = $DEST_DIR"

mkdir -p $DEST_DIR

dotnet pack "$PROJ_DIR" -c Release -o "$DEST_DIR" \
  -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg \
  -p:ContinuousIntegrationBuild=true
