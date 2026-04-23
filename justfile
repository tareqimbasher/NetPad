set positional-arguments

default:
    @just --list --unsorted

var_sln := "src"
var_api := "src/Apps/NetPad.Apps.App"
var_spa := "src/Apps/NetPad.Apps.App/App"
var_cli := "src/Apps/NetPad.Apps.Cli"
var_tauri := "src/Apps/NetPad.Apps.Shells.Tauri/TauriApp"
var_rid_os := if os() == "windows" { "win" } else if os() == "macos" { "osx" } else { os() }
var_rid_arch := if arch() == "x86_64" { "x64" } else if arch() == "aarch64" { "arm64" } else { arch() }
var_rid := var_rid_os + "-" + var_rid_arch

# ─── GENERAL ──────────────────────────────────────────────────────────

# Prints all important paths
[group('general')]
paths:
    @echo "API:                 {{ var_api }}"
    @echo "SPA:                 {{ var_spa }}"
    @echo "CLI:                 {{ var_cli }}"
    @echo "Electron - Manifest: {{ var_api }}/electron.manifest.js (and electron.manifest.dev.js)"
    @echo "Electron - Hook:     {{ var_api }}/ElectronHostHook"
    @echo "Tauri:               {{ var_tauri }}"
    @echo "Rust:                {{ var_tauri }}/src-tauri"

# Print the current app version
[group('general')]
version:
    @grep -oP '(?<=<InformationalVersion>).*(?=</InformationalVersion>)' "{{ var_api }}/NetPad.Apps.App.csproj"

# Bump version across all config files (usage: just bump-version 0.13.0)
[group('general')]
bump-version version:
    #!/usr/bin/env bash
    set -euo pipefail

    echo "Bumping version to {{ version }}..."

    # NetPad.Apps.App.csproj
    sed -i'' -e 's|<AssemblyVersion>.*</AssemblyVersion>|<AssemblyVersion>{{ version }}</AssemblyVersion>|' "{{ var_api }}/NetPad.Apps.App.csproj"
    sed -i'' -e 's|<FileVersion>.*</FileVersion>|<FileVersion>{{ version }}</FileVersion>|' "{{ var_api }}/NetPad.Apps.App.csproj"
    sed -i'' -e 's|<InformationalVersion>.*</InformationalVersion>|<InformationalVersion>{{ version }}</InformationalVersion>|' "{{ var_api }}/NetPad.Apps.App.csproj"

    # electron.manifest.js
    sed -i'' -e 's|buildVersion: ".*"|buildVersion: "{{ version }}"|' "{{ var_api }}/electron.manifest.js"

    # tauri.conf.json5
    sed -i'' -e 's|"version": ".*"|"version": "{{ version }}"|' "{{ var_tauri }}/src-tauri/tauri.conf.json5"

    # Cargo.toml (only the package version, not dependency versions)
    sed -i'' -e '0,/^version = ".*"/s|^version = ".*"|version = "{{ version }}"|' "{{ var_tauri }}/src-tauri/Cargo.toml"

    echo "Updated to {{ version }}:"
    echo "  {{ var_api }}/NetPad.Apps.App.csproj"
    echo "  {{ var_api }}/electron.manifest.js"
    echo "  {{ var_tauri }}/src-tauri/tauri.conf.json5"
    echo "  {{ var_tauri }}/src-tauri/Cargo.toml"

# Verify prerequisites are installed (node, dotnet, cargo, etc.)
[group('general')]
doctor:
    #!/usr/bin/env bash
    set -euo pipefail
    ok=true
    check() {
        if command -v "$1" &>/dev/null; then
            printf "  %-14s %s\n" "$1" "$($1 $2 2>&1 | grep -m1 '[0-9]\.[0-9]')"
        else
            printf "  %-14s MISSING\n" "$1"
            ok=false
        fi
    }
    echo "Checking prerequisites..."
    check node --version
    check npm --version
    check dotnet --version
    check cargo --version
    check rustc --version
    check just --version
    echo ""
    echo "Checking .NET tools..."
    check dotnet-ef --version
    echo ""
    if $ok; then
        echo "All prerequisites found."
    else
        echo "Some prerequisites are missing!"
        exit 1
    fi

# Run `npm install` for all package.json files
[group('general')]
npm-install-all:
    cd "{{ var_spa }}" && npm install
    cd "{{ var_api }}/ElectronHostHook" && npm install
    cd "{{ var_tauri }}" && npm install

# ─── WEB ──────────────────────────────────────────────────────────────

# Web: run the Web backend
[group('web')]
web-run-backend watch="true":
    {{ if watch == "true" { "dotnet watch run" } else { "dotnet run" } }} --project "{{ var_api }}"

# Web: run the Web frontend
[group('web')]
web-run-frontend:
    npm run start-web --prefix "{{ var_spa }}"

# Web: build a local release version (self-contained, runtime: {{var_rid}})
[group('web')]
web-build-release:
    npm run build-web --prefix "{{ var_spa }}"
    dotnet publish "{{ var_api }}" -c Release -r "{{ var_rid }}" --self-contained -o "{{ var_api }}/bin/publish"

# Web: run the built release version
[group('web')]
web-run-release *args:
    "{{ var_api }}/bin/publish/NetPad.Apps.App" "$@"

# ─── TAURI ────────────────────────────────────────────────────────────

# Tauri: run the Tauri backend
[group('tauri')]
tauri-run-backend watch="true":
    {{ if watch == "true" { "dotnet watch run" } else { "dotnet run" } }} --project "{{ var_api }}" -- --tauri

# Tauri: run the Tauri frontend
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-run-frontend:
    npx tauri dev

# Tauri: build a release binary for Windows x64
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-build-release-win-x64:
    npx tauri build -c src-tauri/tauri.conf.win-x64.json5

# Tauri: build a release binary for Windows ARM64
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-build-release-win-arm64:
    npx tauri build -c src-tauri/tauri.conf.win-arm64.json5

# Tauri: build a release binary for Linux x64
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-build-release-linux-x64:
    npx tauri build -c src-tauri/tauri.conf.linux-x64.json5

# Tauri: build a release binary for Linux ARM64
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-build-release-linux-arm64:
    npx tauri build -c src-tauri/tauri.conf.linux-arm64.json5

# Tauri: build a release binary for macOS x64
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-build-release-mac-x64:
    npx tauri build --target x86_64-apple-darwin -c src-tauri/tauri.conf.mac-x64.json5

# Tauri: build a release binary for macOS ARM64
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-build-release-mac-arm64:
    npx tauri build --target aarch64-apple-darwin -c src-tauri/tauri.conf.mac-arm64.json5

# Tauri: print Tauri environment info
[group('tauri')]
[working-directory('src/Apps/NetPad.Apps.Shells.Tauri/TauriApp')]
tauri-info:
    npx tauri info

# ─── ELECTRON ─────────────────────────────────────────────────────────

# Electron: run the Electron backend
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-run-backend watch="true":
    MSYS_NO_PATHCONV=1 electron-sharp start {{ if watch == "true" { "/watch" } else { "" } }} /manifest electron.manifest.js

# Electron: run the Electron backend (macOS ARM64)
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-run-backend-mac-arm64 watch="true":
    MSYS_NO_PATHCONV=1 electron-sharp start {{ if watch == "true" { "/watch" } else { "" } }} /manifest electron.manifest.js /target custom "osx-arm64;mac" /electron-arch arm64

# Electron: run the Electron backend (Linux ARM64)
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-run-backend-linux-arm64 watch="true":
    MSYS_NO_PATHCONV=1 electron-sharp start {{ if watch == "true" { "/watch" } else { "" } }} /manifest electron.manifest.js /target custom "linux-arm64;linux" /electron-arch arm64

# Electron: run the Electron backend (Windows ARM64)
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-run-backend-win-arm64 watch="true":
    MSYS_NO_PATHCONV=1 electron-sharp start {{ if watch == "true" { "/watch" } else { "" } }} /manifest electron.manifest.js /target custom "win-arm64;win" /electron-arch arm64

# Electron: run the Electron frontend
[group('electron')]
electron-run-frontend:
    npm run start --prefix "{{ var_spa }}"

# Electron: build a release binary for Windows x64
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-build-release-win-x64:
    MSYS_NO_PATHCONV=1 electron-sharp build /target win /manifest electron.manifest.js /PublishSingleFile false

# Electron: build a release binary for Windows ARM64
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-build-release-win-arm64:
    MSYS_NO_PATHCONV=1 electron-sharp build /target custom "win-arm64;win" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false

# Electron: build a release binary for Linux x64
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-build-release-linux-x64:
    MSYS_NO_PATHCONV=1 electron-sharp build /target linux /manifest electron.manifest.js /PublishSingleFile false

# Electron: build a release binary for Linux ARM64
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-build-release-linux-arm64:
    MSYS_NO_PATHCONV=1 electron-sharp build /target custom "linux-arm64;linux" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false

# Electron: build a release binary for macOS x64
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-build-release-mac-x64:
    MSYS_NO_PATHCONV=1 electron-sharp build /target osx /manifest electron.manifest.js /PublishSingleFile false

# Electron: build a release binary for macOS ARM64
[group('electron')]
[working-directory('src/Apps/NetPad.Apps.App')]
electron-build-release-mac-arm64:
    MSYS_NO_PATHCONV=1 electron-sharp build /target custom "osx-arm64;mac" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false

# ─── CLI ──────────────────────────────────────────────────────────────

# CLI: run the npad CLI
[group('cli')]
cli-run *args:
    dotnet run --project "{{ var_cli }}" -- "$@"

# CLI: build npad
[group('cli')]
cli-build:
    dotnet build "{{ var_cli }}"

# CLI: build a local release version of npad (self-contained, runtime: {{var_rid}})
[group('cli')]
cli-build-release:
    dotnet publish "{{ var_cli }}" -c Release -r "{{ var_rid }}" --self-contained -o "{{ var_cli }}/bin/publish"

# CLI: run the built release version of npad
[group('cli')]
cli-run-release *args:
    "{{ var_cli }}/bin/publish/npad" "$@"

# CLI: pack npad as a NuGet package
[group('cli')]
cli-nuget-pack:
    bash scripts/nuget-pack-npad.sh

# CLI: push npad NuGet package (requires NUGET_API_KEY)
[group('cli')]
cli-nuget-push:
    bash scripts/nuget-push-npad.sh

# ─── DOCS ────────────────────────────────────────────────────────────

# Docs: serve documentation site locally with docsify
[group('docs')]
docs-serve port="3000":
    npx docsify-cli serve docs --port {{ port }}

# ─── DOTNET ───────────────────────────────────────────────────────────

# .NET: restore NuGet packages
[group('dotnet')]
dotnet-restore:
    dotnet restore "{{ var_sln }}"

# .NET: build solution
[group('dotnet')]
dotnet-build:
    dotnet build "{{ var_sln }}"

# .NET: build solution in Release configuration
[group('dotnet')]
dotnet-build-release:
    dotnet build "{{ var_sln }}" -c Release

# .NET: clean solution
[group('dotnet')]
dotnet-clean:
    dotnet clean "{{ var_sln }}"

# .NET: format code
[group('dotnet')]
dotnet-format:
    dotnet format "{{ var_sln }}"

# .NET: run tests
[group('dotnet')]
dotnet-test:
    dotnet test "{{ var_sln }}" --filter "FullyQualifiedName!~IntegrationTests"

# .NET: run tests including integration tests
[group('dotnet')]
dotnet-test-all:
    dotnet test "{{ var_sln }}"

# .NET: generate TypeScript API clients using NSwag
[group('dotnet')]
dotnet-ts-gen:
    dotnet run --project "{{ var_api }}" -- --swagger

# .NET: check if generated TypeScript API clients are up to date
[group('dotnet')]
dotnet-ts-gen-check:
    #!/usr/bin/env bash
    set -euo pipefail
    dotnet run --project "{{ var_api }}" -- --swagger
    if ! git diff --quiet -- "{{ var_spa }}/src/core/@application/api.ts" "{{ var_spa }}/src/core/@plugins/*/api.ts"; then
        echo "Generated API clients are out of date. Run 'just dotnet-ts-gen' and commit the changes."
        exit 1
    fi
    echo "API clients are up to date."

# .NET: list outdated NuGet packages
[group('dotnet')]
dotnet-outdated:
    dotnet list "{{ var_sln }}" package --outdated

# ─── JAVASCRIPT ───────────────────────────────────────────────────────

# JavaScript: build SPA (Electron target)
[group('javascript')]
js-build-electron:
    npm run build --prefix "{{ var_spa }}"

# JavaScript: build SPA (Web target)
[group('javascript')]
js-build-web:
    npm run build-web --prefix "{{ var_spa }}"

# JavaScript: run all linters (eslint, htmlhint, stylelint)
[group('javascript')]
js-lint:
    npm run lint --prefix "{{ var_spa }}"

# JavaScript: run tests
[group('javascript')]
js-test:
    npm run test --prefix "{{ var_spa }}"

# JavaScript: analyze webpack bundle (interactive server)
[group('javascript')]
js-analyze:
    npm run analyze --prefix "{{ var_spa }}"

# JavaScript: analyze webpack bundle (static HTML report)
[group('javascript')]
js-analyze-static:
    npm run analyze --prefix "{{ var_spa }}" -- --env analyzeStatic

# JavaScript: update browserslist database
[group('javascript')]
js-update-browsers:
    cd "{{ var_spa }}" && npx update-browserslist-db@latest

# JavaScript: list outdated npm packages
[group('javascript')]
js-outdated:
    npm outdated --prefix "{{ var_spa }}" || true

# ─── RUST ─────────────────────────────────────────────────────────────

# Rust: check rust code
[group('rust')]
rust-check:
    cargo check --manifest-path "{{ var_tauri }}/src-tauri/Cargo.toml"

# Rust: build rust code
[group('rust')]
rust-build:
    cargo build --manifest-path "{{ var_tauri }}/src-tauri/Cargo.toml"

# Rust: run rust tests
[group('rust')]
rust-test:
    cargo test --manifest-path "{{ var_tauri }}/src-tauri/Cargo.toml"

# Rust: format rust code
[group('rust')]
rust-format:
    cargo fmt --all --manifest-path "{{ var_tauri }}/src-tauri/Cargo.toml"

# Rust: check rust code formatting and run clippy
[group('rust')]
rust-lint:
    cargo fmt --all --check --manifest-path "{{ var_tauri }}/src-tauri/Cargo.toml"
    cargo clippy --all-targets --manifest-path "{{ var_tauri }}/src-tauri/Cargo.toml"

# ─── ALL ──────────────────────────────────────────────────────────────

# Clean all build artifacts (bin, obj, dist, node_modules/.cache)
[group('all')]
[confirm("This will delete all build artifacts. Continue?")]
clean-all:
    dotnet clean "{{ var_sln }}"
    rm -rf "{{ var_spa }}/dist"
    rm -rf "{{ var_spa }}/node_modules/.cache"
    find "{{ var_sln }}" -type d \( -name bin -o -name obj \) -exec rm -rf {} + 2>/dev/null || true

# Run all tests
[group('all')]
test-all: dotnet-test js-test rust-test

# Run all tests, lints, and checks
[group('all')]
check-all: dotnet-test js-test js-lint rust-test rust-lint
