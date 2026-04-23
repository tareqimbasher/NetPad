<h1>Contributing to&nbsp;<img src="https://github.com/tareqimbasher/netpad/blob/main/src/Apps/NetPad.Apps.App/wwwroot/logo/circle/32x32.png?raw=true" width="32" height="32" style="vertical-align: middle;"/>&nbsp;NetPad</h1>

Thank you for considering contributing to **NetPad**! We're excited to have you join the project.

- [How to Contribute](#how-to-contribute-star)
- [Submitting Changes](#submitting-changes)
- [Project Structure](#project-structure)
- [Build & Run](#build--run-hammer_and_pick)
    - [Prerequisites](#prerequisites)
    - [Web Browser](#web-browser)
    - [Electron Desktop App](#electron-desktop-app)
    - [Tauri (Native Shell) Desktop App](#tauri-native-shell-desktop-app)
- [Packaging](#packaging-package)
    - [Web](#web)
    - [Electron](#electron)
    - [Tauri](#tauri)
- [Testing](#testing-test_tube)
- [Code Style & Checks](#code-style--checks)
- [Issue Reporting](#issue-reporting)
- [Documentation Guidelines](#documentation-guidelines)
- [License](#license)
- [Contact](#contact)
- [Acknowledgments](#acknowledgments)

## How to Contribute :star:

- Report bugs or request features through
  the [issue tracker](https://github.com/tareqimbasher/NetPad/issues).
- [Fork](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo)
  the repository, [create a branch](https://git-scm.com/docs/git-checkout), make your changes,
  and submit
  a [pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request)
  to the `main` branch.

## Submitting Changes

- **Branching Strategy**: Create a branch for your work
    - For new features or changes use the `feat/` prefix, example: `feat/my-new-feature`
    - For bugs or fixes use the `fix/` prefix, example: `fix/my-fix`
- **Commit Messages**: Use the imperative mood, e.g. `Add contributing guidelines`.
- **Pull Requests**: Ensure your PR includes **tests** and follows our coding standards. Include a
  small **description** of the changes as well as the **issue number** (if applicable).

#### Pull request example :arrow_down:

> Added contributing guidelines to make it easier for new contributors to get started.
>
> **Changes Made:**
>
> - Added a new CONTRIBUTING.md file.
> - Updated the README.md file to include a link to the new CONTRIBUTING.md file.

## Project Structure

NetPad has a .NET backend (C#) and an [Aurelia 2](https://aurelia.io/) TypeScript frontend. The
backend handles script compilation, execution, data connections, and more, while the frontend
provides the editor UI. The frontend can be hosted in different _shells_: a web browser, an Electron
desktop app, or a Tauri native desktop app.

```
src/
├── Core/NetPad.Runtime           # Core runtime: script execution, compilation, data connections
├── Apps/
│   ├── NetPad.Apps.App           # ASP.NET Core host + SPA frontend (App/ subdirectory)
│   ├── NetPad.Apps.Common        # Shared app services, CQRS handlers, EF Core data drivers
│   ├── NetPad.Apps.Cli           # CLI tool (npad)
│   ├── NetPad.Apps.ScriptHost    # Separate process for isolated script execution
│   ├── NetPad.Apps.Shells.Electron
│   ├── NetPad.Apps.Shells.Tauri
│   └── NetPad.Apps.Shells.Web
├── Plugins/NetPad.Plugins.OmniSharp  # Code intelligence (IntelliSense, diagnostics)
└── Tests/                        # Test projects mirror the main project structure
```

For a deeper dive into the architecture, IPC model, and startup flow, see
the [technical documentation](docs/technical-docs/).

## Build & Run :hammer_and_pick:

### Prerequisites

- [Node](https://nodejs.org/en/download) v22+
- [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) 9.x
- [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): install with
  `dotnet tool install --global dotnet-ef`
- (If contributing to the Tauri shell) [Rust toolchain](https://www.rust-lang.org/tools/install)
  — see [Tauri prerequisites](https://v2.tauri.app/start/prerequisites/) for platform-specific
  system dependencies
- (Recommended) [just](https://github.com/casey/just) command runner — a `justfile` is provided with
  recipes for building, running, testing, and linting. Run `just` to see all available tasks. If you
  prefer not to use `just`, you can find the equivalent raw commands in
  the [`justfile`](justfile) and run them directly.

Start by cloning, or forking, the repository:

```bash
git clone https://github.com/tareqimbasher/NetPad.git && cd NetPad
```

Then install all frontend dependencies:

```bash
just npm-install-all
```

<details>
<summary>Without <code>just</code></summary>

```bash
npm install --prefix src/Apps/NetPad.Apps.App/App
npm install --prefix src/Apps/NetPad.Apps.App/ElectronHostHook
npm install --prefix src/Apps/NetPad.Apps.Shells.Tauri/TauriApp
```

</details>

### Web Browser

1. Run the frontend

```bash
just web-run-frontend
```

2. Run the .NET backend

```bash
just web-run-backend
```

Navigate to the app from your web browser at [http://localhost:57940](http://localhost:57940).

<details>
<summary>Without <code>just</code></summary>

```bash
# Terminal 1: Frontend
cd src/Apps/NetPad.Apps.App/App
npm run start-web

# Terminal 2: Backend
cd src/Apps/NetPad.Apps.App
dotnet watch run --environment Development
```

</details>

> [!NOTE]
> Creating new SQLite database connections is not supported when NetPad is used in a browser.

### Electron Desktop App

1. Run the frontend

```bash
just electron-run-frontend
```

2. Run the Electron backend

```bash
just electron-run-backend

# For ARM:
just electron-run-backend-mac-arm64
just electron-run-backend-linux-arm64
just electron-run-backend-win-arm64
```

> [!TIP]
> You need the `electron-sharp` dotnet tool installed:
> `dotnet tool install ElectronSharp.CLI -g`

<details>
<summary>Without <code>just</code></summary>

```bash
# Frontend
cd src/Apps/NetPad.Apps.App/ElectronHostHook && npm install
cd ../App && npm install
npm start

# Backend
cd src/Apps/NetPad.Apps.App/
electron-sharp start /watch /manifest electron.manifest.js

# For ARM:
electron-sharp start /watch /manifest electron.manifest.js /target custom "osx-arm64;mac" /electron-arch arm64
electron-sharp start /watch /manifest electron.manifest.js /target custom "linux-arm64;linux" /electron-arch arm64
electron-sharp start /watch /manifest electron.manifest.js /target custom "win-arm64;win" /electron-arch arm64
```

</details>

> [!TIP]
> If invoking any of those commands gives you strange errors (like .NET 6 not installed), it means
> you've typed electronize instead of electron-sharp.

> [!TIP]
> The very first `electron-sharp start` is slow due to dependency downloads. Later runs are much
> faster!

### Tauri (Native Shell) Desktop App

You will need to have the [Rust toolchain](https://www.rust-lang.org/tools/install) installed to
build the Tauri app. See the [Tauri prerequisites](https://v2.tauri.app/start/prerequisites/) for
platform-specific system dependencies.

1. Run the .NET backend

```bash
just tauri-run-backend
```

2. Run the Tauri frontend (in another terminal)

```bash
just tauri-run-frontend
```

<details>
<summary>Without <code>just</code></summary>

```bash
# Terminal 1: Backend
cd src/Apps/NetPad.Apps.App
dotnet watch run --environment Development --tauri

# Terminal 2: Tauri frontend
cd src/Apps/NetPad.Apps.Shells.Tauri/TauriApp
npm install
npx tauri dev
```

</details>

## Packaging :package:

Production packaging is automated via the GitHub Actions release workflow — see
[`.github/workflows/release.yml`](.github/workflows/release.yml) for the full CI/CD definition.
The CLI is published separately via
[`.github/workflows/publish-cli.yml`](.github/workflows/publish-cli.yml). The sections below cover
how to build packages locally.

### Web

Build a self-contained .NET app with the SPA frontend bundled in, targeting your current host
OS/architecture:

```bash
just web-build-release
```

The output is a standalone ASP.NET Core app in `src/Apps/NetPad.Apps.App/bin/publish`, suitable for
distribution or deployment to any server or container.

<details>
<summary>Without <code>just</code></summary>

For a framework-dependent build (smaller, but requires .NET installed on the target server):

```bash
dotnet publish src/Apps/NetPad.Apps.App -c Release /p:WebBuild=true
```

The `/p:WebBuild=true` flag tells the build to use the `web` webpack target instead of
`electron-renderer`.

</details>

### Electron

The Electron app is built and packaged using
[electron-builder](https://www.electron.build/). Configuration is in the
[`electron.manifest.js`](src/Apps/NetPad.Apps.App/electron.manifest.js) file.

Build for your target platform:

```bash
# For x64:
just electron-build-release-win-x64
just electron-build-release-mac-x64
just electron-build-release-linux-x64

# For ARM64:
just electron-build-release-win-arm64
just electron-build-release-mac-arm64
just electron-build-release-linux-arm64
```

Packaged files can be found in the `bin/Desktop` folder.

> [!NOTE]
> To build `flatpak` files the `flatpak` and `flatpak-builder` packages need to be installed.

<details>
<summary>Without <code>just</code></summary>

From the root directory of the `NetPad.Apps.App` project:

```bash
# For x64:
electron-sharp build /target win /manifest electron.manifest.js /PublishSingleFile false
electron-sharp build /target osx /manifest electron.manifest.js /PublishSingleFile false
electron-sharp build /target linux /manifest electron.manifest.js /PublishSingleFile false

# For ARM64:
electron-sharp build /target custom "osx-arm64;mac" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false
electron-sharp build /target custom "linux-arm64;linux" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false
electron-sharp build /target custom "win-arm64;win" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false
```

</details>

See the [ElectronSharp docs](https://github.com/theolivenbaum/electron-sharp) for additional CLI
options when packaging the app, and [electron-builder](https://www.electron.build/) for additional
configuration options.

### Tauri

Tauri packaging uses platform-specific config files located in
[`src/Apps/NetPad.Apps.Shells.Tauri/TauriApp/src-tauri/`](src/Apps/NetPad.Apps.Shells.Tauri/TauriApp/src-tauri/).
Each config handles the `dotnet publish` step automatically as a `beforeBuildCommand`.

Build for your target platform:

```bash
# For x64:
just tauri-build-release-win-x64
just tauri-build-release-linux-x64
just tauri-build-release-mac-x64

# For ARM64:
just tauri-build-release-win-arm64
just tauri-build-release-linux-arm64
just tauri-build-release-mac-arm64
```

> [!NOTE]
> On Linux, you'll need additional system dependencies:
> `sudo apt-get install -y libwebkit2gtk-4.1-dev libappindicator3-dev librsvg2-dev patchelf`

<details>
<summary>Without <code>just</code></summary>

From the `src/Apps/NetPad.Apps.Shells.Tauri/TauriApp` directory:

```bash
# For x64:
npx tauri build -c src-tauri/tauri.conf.win-x64.json5
npx tauri build -c src-tauri/tauri.conf.linux-x64.json5
npx tauri build --target x86_64-apple-darwin -c src-tauri/tauri.conf.mac-x64.json5

# For ARM64:
npx tauri build -c src-tauri/tauri.conf.win-arm64.json5
npx tauri build -c src-tauri/tauri.conf.linux-arm64.json5
npx tauri build --target aarch64-apple-darwin -c src-tauri/tauri.conf.mac-arm64.json5
```

</details>

## Testing :test_tube:

### .NET Tests

```bash
just dotnet-test
```

To run a specific test by name:

```bash
dotnet test src --filter "FullyQualifiedName~ClassName.MethodName"
```

To run a specific test project:

```bash
dotnet test src/Tests/NetPad.Runtime.Tests
```

> [!NOTE]
> Integration tests use [Testcontainers](https://testcontainers.com/) (PostgreSQL) and require
> Docker. They are excluded from `just dotnet-test` by default. To run all tests including
> integration tests, use `just dotnet-test-all`.

### JavaScript Tests

```bash
just js-test
```

### Rust Tests

Only relevant when contributing to the Tauri shell's Rust code.

```bash
just rust-test
```

### Run Everything

To run all tests, lints, and checks in one go:

```bash
just check-all
```

<details>
<summary>Without <code>just</code></summary>

```bash
# .NET tests (excluding integration tests)
dotnet test src --filter "FullyQualifiedName!~IntegrationTests"

# JavaScript tests
npm test --prefix src/Apps/NetPad.Apps.App/App

# JavaScript linting
npm run lint --prefix src/Apps/NetPad.Apps.App/App
```

</details>

## Code Style & Checks

- **C#**: `TreatWarningsAsErrors` is enabled globally — your code must compile without warnings.
  Run `just dotnet-format` to auto-format before submitting.
- **JavaScript/TypeScript**: Run `just js-lint` to check for linting issues.
- **Rust** (Tauri shell): Run `just rust-lint` to check formatting and lint Rust code, or
  `just rust-format` to auto-format.
- Before opening a PR, run `just check-all` to validate tests and linting together.

## Issue Reporting

- **Bug Reports**: Include steps to reproduce and environment details.
- **Feature Requests**: Clearly describe the feature and why it's needed.

## Documentation Guidelines

- **User-facing documentation** (features, CLI usage, data connections, troubleshooting) lives
  in [`docs/wiki/`](docs/wiki/).
- **Technical documentation** (architecture, IPC, startup flow, internal guides) lives
  in [`docs/technical-docs/`](docs/technical-docs/).
- If your change affects user-visible behavior, update the relevant docs along with your code.

## License

By contributing, you agree that your contributions will be licensed under
the [MIT LICENSE](./LICENSE).

## Contact

If you have any questions, feel free to open an issue or contact us at
our [Discord Server](https://discord.gg/FrgzNBYQFW)

At our Discord, you can find the latest announcements, troubleshooting, ideas and feedback, and
more.

## Acknowledgments

Thank you to all who have contributed to NetPad! You can see all
contributors [here](https://github.com/tareqimbasher/NetPad/graphs/contributors).
