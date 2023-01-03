#!/usr/bin/env bash

###
### Used to build and distribute packages of the Electron app to a dist/ folder.
###
### Example how to run:
###
### Build and distribute for all OS's
### package-electron.sh -b
###
### Build and distribute for Windows only
### package-electron.sh -bt win
###
### Distribute (without building) for all OS's
### package-electron.sh
###
### Distribute for Linux only
### package-electron.sh -t linux
###
### Clean (delete dist folder) before building and distributing for OS X only
### package-electron.sh -bct osx
###

RED="\e[31m"
GREEN="\e[32m"
ENDCOLOR="\e[0m"

while getopts "bcht:" flag; do
  case "${flag}" in
  b) build="true" ;;
  t) os=${OPTARG} ;;
  c) clean="true" ;;
  h)
    echo "help:"
    echo "   -b:   Builds the electron app. If this is omitted, the script will only distribute to dist directory."
    echo "   -t:   Target a specific os. Options: linux, osx, win"
    echo "   -h:   Print help section and exit"
    exit 0
    ;;
  *) exit 1 ;;
  esac
done

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
APP_DIR=$(cd "$SCRIPT_DIR/../src/Apps/NetPad.Apps.App" && pwd)
PACKAGES_SOURCE_DIR="$APP_DIR/bin/Desktop"
PACKAGES_DEST_DIR="$SCRIPT_DIR/../dist"

printf "%b" "${GREEN}# SCRIPT_DIR:           $SCRIPT_DIR${ENDCOLOR}\n"
printf "%b" "${GREEN}# APP_DIR:              $APP_DIR${ENDCOLOR}\n"
printf "%b" "${GREEN}# PACKAGES_SOURCE_DIR:  $PACKAGES_SOURCE_DIR${ENDCOLOR}\n"
printf "%b" "${GREEN}# PACKAGES_DEST_DIR:    $PACKAGES_DEST_DIR${ENDCOLOR}\n"

if [ -z "$SCRIPT_DIR" ]; then
  echo "Could not determine script dir"
  exit 1
fi

if [ -z "$APP_DIR" ]; then
  echo "Could not determine app dir"
  exit 1
fi

if [ -z "$PACKAGES_SOURCE_DIR" ]; then
  echo "Could not determine source package dir"
  exit 1
fi

if [ -z "$PACKAGES_DEST_DIR" ]; then
  echo "Could not determine destination package dir"
  exit 1
fi

# Create the DEST directory
if [[ "$clean" == "true" ]]; then
  rm -rf "$PACKAGES_DEST_DIR"
fi

mkdir -p "$PACKAGES_DEST_DIR"

distribute() {
  local filename="$1"
  local os="$2"

  # We don't want to remove the "os" from .zip files
  [[ $filename == *.zip ]] && new_name=$filename || new_name=${filename/-$os/}

  if [[ "$filename" != "$new_name" ]]; then
    echo "   - Renaming: $filename -> $new_name"
    mv -- "$filename" "$new_name"
  fi

  echo "   - Copying: $new_name"
  mv "$new_name" "$PACKAGES_DEST_DIR/$new_name"
}

if [[ -z $os ]] || [[ $os == "linux" ]]; then
  printf "%b" "${GREEN}\n# LINUX packages\n${ENDCOLOR}"

  if [[ $build == "true" ]]; then
    echo "- Building..."
    cd "$APP_DIR" || exit 1
    electronize build /manifest electron.manifest.js /PublishSingleFile false /target linux
  fi

  echo "- Distributing..."
  cd "$PACKAGES_SOURCE_DIR" || exit 1
  for filename in *-linux.*; do
    if [[ -d $filename ]]; then
      continue
    fi

    if [[ $filename != *.pacman ]] && [[ $filename != *.AppImage ]] && [[ $filename != *.deb ]] && [[ $filename != *.rpm ]] && [[ $filename != *.zip ]]; then
      continue
    fi

    distribute "$filename" "linux"
  done
fi

if [[ -z $os ]] || [[ $os == "osx" ]]; then
  printf "%b" "${GREEN}\n# OSX packages\n${ENDCOLOR}"

  if [[ $build == "true" ]]; then
    echo "- Building..."
    cd "$APP_DIR" || exit 1
    electronize build /manifest electron.manifest.js /PublishSingleFile false /target osx
  fi

  echo "- Distributing..."
  cd "$PACKAGES_SOURCE_DIR" || exit 1
  for filename in *-mac.*; do
    if [[ -d $filename ]]; then
      continue
    fi

    if [[ $filename != *.zip ]]; then
      continue
    fi

    distribute "$filename" "mac"
  done
fi

if [[ -z $os ]] || [[ $os == "win" ]]; then
  printf "%b" "${GREEN}\n# WINDOWS packages\n${ENDCOLOR}"

  if [[ $build == "true" ]]; then
    echo "- Building..."
    cd "$APP_DIR" || exit 1
    electronize build /manifest electron.manifest.js /PublishSingleFile false /target win
  fi

  echo "- Distributing..."
  cd "$PACKAGES_SOURCE_DIR" || exit 1
  for filename in *-win.*; do
    if [[ -d $filename ]]; then
      continue
    fi

    if [[ $filename != *.exe* ]] && [[ $filename != *.zip ]]; then
      continue
    fi

    distribute "$filename" "win"
  done
fi
