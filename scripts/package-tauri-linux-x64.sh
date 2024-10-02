#!/usr/bin/env bash

set -o nounset
set -o errexit

RED="\033[0;31m"
GREEN="\033[0;32m"
LCYAN="\033[1;36m"
NC="\033[0m"

if ! command -v dotnet &> /dev/null; then
    echo "Could not find 'dotnet'."
    exit 1
fi

if ! command -v npm &> /dev/null; then
    echo "Could not find 'npm'."
    exit 1
fi

if ! command -v cargo &> /dev/null; then
    echo "Could not find 'cargo'."
    exit 1
fi

SCRIPT_DIR=$(cd "$(dirname "$0")" && pwd)
APP_DIR=$(cd "$SCRIPT_DIR/../src/Apps/NetPad.Apps.Shells.Tauri/TauriApp" && pwd)
SPA_APP_DIR=$(cd "$SCRIPT_DIR/../src/Apps/NetPad.Apps.App/App" && pwd)
PACKAGES_SOURCE_DIR="$APP_DIR/src-tauri/target/x86_64-unknown-linux-gnu/release"
PACKAGES_DEST_DIR="$SCRIPT_DIR/../dist/tauri"

if [ -z "$SCRIPT_DIR" ]; then
  echo "Could not determine script dir"
  exit 1
fi

if [ -z "$APP_DIR" ]; then
  echo "Could not determine app dir"
  exit 1
fi

if [ -z "$SPA_APP_DIR" ]; then
  echo "Could not determine SPA app dir"
  exit 1
fi

if [ -z "$PACKAGES_DEST_DIR" ]; then
  echo "Could not determine destination package dir"
  exit 1
fi

printf "%b" "${GREEN}# SCRIPT_DIR:${NC}           $SCRIPT_DIR\n"
printf "%b" "${GREEN}# APP_DIR:${NC}              $APP_DIR\n"
printf "%b" "${GREEN}# SPA_APP_DIR:${NC}          $SPA_APP_DIR\n"
printf "%b" "${GREEN}# PACKAGES_SOURCE_DIR:${NC}  $PACKAGES_DEST_DIR\n"
printf "%b" "${GREEN}# PACKAGES_DEST_DIR:${NC}    $PACKAGES_DEST_DIR\n"

# Create the DEST directory
# if [[ "$clean" == "true" ]]; then
#   rm -rf "$PACKAGES_DEST_DIR"
# fi

mkdir -p "$PACKAGES_DEST_DIR"

print_section_div() {
    printf '%*s\n' "${COLUMNS:-$(tput cols)}" '' | tr ' ' =
}

copy() {
    local dir="$1"

    for filename in "$dir"/*; do
        if [[ -d $filename ]]; then
            continue
        fi

        if [[ $filename != *.zip ]] && [[ $filename != *.pacman ]] && [[ $filename != *.AppImage ]] && [[ $filename != *.deb ]] && [[ $filename != *.rpm ]] && [[ $filename != *.flatpak ]] && [[ $filename != *.snap ]]; then
            continue
        fi

        echo "   - Copying: $filename"
        mv "$filename" "$PACKAGES_DEST_DIR/"
    done
}

# Ensure npm packages are installed
printf "\n1. ${LCYAN}Installing npm packages in: $APP_DIR${NC}\n"
npm install --prefix "$APP_DIR"
print_section_div

printf "\n2. ${LCYAN}Installing npm packages in: $SPA_APP_DIR${NC}\n"
npm install --prefix "$SPA_APP_DIR"
print_section_div

cd "$APP_DIR"

printf "\n3. ${LCYAN}Packaging Tauri app${NC}\n"
#npx tauri build -c src-tauri/tauri.conf.linux-x64.json5 --target x86_64-unknown-linux-gnu
print_section_div

printf "\n4. ${LCYAN}Copying generated bundles${NC}\n"
copy "$PACKAGES_SOURCE_DIR/bundle/deb"
copy "$PACKAGES_SOURCE_DIR/bundle/rpm"
print_section_div

printf "\n5. ${LCYAN}Zipping standalone app${NC}\n"
cd "$PACKAGES_SOURCE_DIR"
zip -q -r "$PACKAGES_DEST_DIR/NetPad-vNext-0.8.0-linux-x86_64.zip" "NetPad vNext" "resources"
cd "$APP_DIR"
print_section_div

cd "$SCRIPT_DIR"
