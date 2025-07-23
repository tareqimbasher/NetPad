<h1>Contributing to&nbsp;<img src="https://github.com/tareqimbasher/netpad/blob/main/src/Apps/NetPad.Apps.App/wwwroot/logo/circle/32x32.png?raw=true" width="32" height="32" style="vertical-align: middle;"/>&nbsp;NetPad</h1>

Last updated: Nov 28th, 2024 :book:

Thank you for considering contributing to **NetPad**! We’re excited to have you join the project.

- [How to Contribute](#how-to-contribute-star)
- [Submitting Changes](#submitting-changes)
- [Build & Run](#build--run-hammer_and_pick)
    - [Prerequisites](#prerequisites)
    - [Web Browser](#web-browser)
    - [Electron Desktop App](#electron-desktop-app)
    - [Tauri (Native) App](#tauri-native-app)
    - [Packaging](#packaging-package)
- [Issue Reporting](#issue-reporting)
- [Documentation Guidelines](#documentation-guidelines)
- [License](#license)
- [Contact](#contact)
- [Acknowledgments](#acknowledgments)

## How to Contribute :star:

- Report bugs or request features through
  the [issue tracker](https://github.com/tareqimbasher/NetPad/issues).
- [Fork](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo)
  the repository, [create a branch](https://git-scm.com/docs/git-checkout), make
  your [changes](https://stackoverflow.com/questions/76776374/how-do-i-add-commit-and-push-code-to-git-using-git-bash-command-at-once#answer-76776375),
  and submit
  a [pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request)
  to the `main` branch.

## Submitting Changes

- **Branching Strategy**: Create a branch for your work
    - For new features or changes use the `feat/` prefix, example: `feat/my-new-feature`
    - For bugs or fixes use the `fix` prefix, example: `fix/my-fix`
- **Commit Messages**: Follow this format: `Add contributing guidelines`.
- **Pull Requests**: Ensure your PR includes **tests** and follows our coding standards. A small *
  *description** of the changes as well as the **issue number** (if applicable).

#### Pull request example :arrow_down:

> Added contributing guidelines to make it easier for new contributors to get started.
>
> **Changes Made:**
>
> - Added a new CONTRIBUTING.md file.
> - Updated the README.md file to include a link to the new CONTRIBUTING.md file.

## Build & Run :hammer_and_pick:

NetPad has a .NET backend that does all the heavy lifting and a JavaScript interface. The user
interface can be hosted in a number of different environments, or _shells_:

1. **Web Browser**: where you access the NetPad interface from any web browser.
2. **Electron Desktop App**: a cross-platform desktop application that uses Electron.js to host the
   NetPad interface.
3. **Tauri Desktop App**: a cross-platform native desktop application written in Rust that hosts the
   NetPad interface. It is smaller than Electron and uses less system resources.

### Prerequisites

- [Node](https://nodejs.org/en/download) v22+
- [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) 9.x
- [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

Start by cloning, or forking, the repository:

```bash
git clone https://github.com/tareqimbasher/NetPad.git && cd NetPad
```

### Web Browser

1. Run the JavaScript SPA

```bash
# Install dependencies
cd src/Apps/NetPad.Apps.App/App/
npm install

# Start the development web server
npm run start-web
```

2. Start the development web server

```bash
cd src/Apps/NetPad.Apps.App/
dotnet watch run --environment Development
```

Navigate to the app from your web browser at [http://localhost:57940](http://localhost:57940).

> [!NOTE]
> Creating new SQLite database connections is not supported when NetPad is used in a browser.

### Electron Desktop App

1. Run the JavaScript SPA

```bash
# Install dependencies
cd src/Apps/NetPad.Apps.App/ElectronHostHook
npm install

cd ../App
npm install

# Start the development web server
npm start
```

2. Run the Electron app

```bash
cd src/Apps/NetPad.Apps.App/

# Install the `electron-sharp` dotnet tool
dotnet tool install ElectronSharp.CLI -g

# Start in watch mode (remove '/watch' to run without watch mode)
electron-sharp start /watch /manifest electron.manifest.dev.js

# For macOS ARM, append the following to the 'electron-sharp start' command
/target custom "osx-arm64;mac" /electron-arch arm64
```

> [!HINT]
> If invoking any of those commands gives you strange errors (like .NET 6 not installed), it means
> you've typed electronize instead of electron-sharp.

> [!TIP]
> The very first `electron-sharp start` is slow due to dependency downloads. Later runs are much
> faster!

### Tauri (Native Shell) Desktop App

You will need to have the [Rust toolchain](https://www.rust-lang.org/tools/install)  installed to
build the Tauri app.

1. Install JavaScript SPA dependencies

```bash
cd src/Apps/NetPad.Apps.App/App
npm install
```

2. Run the .NET app

```bash
cd src/Apps/NetPad.Apps.App
dotnet watch run --environment Development --tauri
```

3. Run the Tauri app

```bash
cd src/Apps/NetPad.Apps.Shells.Tauri/TauriApp
npm install
npx tauri dev
```

### Packaging :package:

The Electron app is built and packaged using
[electron-builder](https://www.electron.build/). Configuration is in the `electron.manifest.js`
file.

Build the app for the desired platform from the root directory of the `NetPad.Apps.App` project:

```bash
# For x64:
electron-sharp build /target win /manifest electron.manifest.js /PublishSingleFile false
electron-sharp build /target osx /manifest electron.manifest.js /PublishSingleFile false
electron-sharp build /target linux /manifest electron.manifest.js /PublishSingleFile false

# For macOS ARM:
electron-sharp build /target custom "osx-arm64;mac" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false
```

Packaged files can be found in the `bin/Desktop` folder.

> [!IMPORTANT]
> To Build `flatpak` files the `flatpak` and `flatpak-builder` packages need to be installed. On
> Linux, the `/scripts/package-electron.sh` script can be used to package the Electron app, in which
> case packaged files can be found in the `/dist/` folder.

See the [ElectronSharp docs](https://github.com/theolivenbaum/electron-sharp) for additional CLI
options when packaging the app, and [electron-builder](https://www.electron.build/) for additional
configuration options.

## Issue Reporting

- **Bug Reports**: Include steps to reproduce and environment details.
- **Feature Requests**: Clearly describe the feature and why it’s needed.

## License

By contributing, you agree that your contributions will be licensed under
the [MIT LICENSE](./LICENSE).

## Contact

If you have any questions, feel free to open an issue or contact us at
our [Discord Server](https://discord.gg/FrgzNBYQFW)

At our Discord, you can find the latest announcements, troubleshooting, ideas and feedback, and
more.
