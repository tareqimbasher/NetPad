
#  <div style="display: flex; align-items: center; gap: 7px">Contributing to<img src="https://github.com/tareqimbasher/netpad/blob/main/src/Apps/NetPad.Apps.App/wwwroot/logo/circle/32x32.png?raw=true"/>NetPad</div>

Last updated: August 25th, 2024 :book:

Thank you for considering contributing to **NetPad**! We’re excited to have you join the project.

- [How to Contribute](#how-to-contribute-star)
- [Submitting Changes](#submitting-changes)
- [Creating a Development Environment](#creating-a-development-environment)
  - [Prerequisites](#prerequisites)
  - [Manually Start the Project](#manually-start-the-project)
    - [NetPad as an Electron desktop app](#netpad-as-an-electron-desktop-app-wrench)
    - [NetPad as a Web App (accessed from a browser)](#netpad-as-a-web-app-accessed-from-a-browser-spider_web)
  - [Packaging](#packaging-package)
- [Issue Reporting](#issue-reporting)
- [Documentation Guidelines](#documentation-guidelines)
- [License](#license)
- [Contact](#contact)
- [Acknowledgments](#acknowledgments)

## How to Contribute :star:
- Report bugs or request features through the [issue tracker](https://github.com/tareqimbasher/NetPad/issues).
- [Fork](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo) the repository, [create a branch](https://git-scm.com/docs/git-checkout), make your [changes](https://stackoverflow.com/questions/76776374/how-do-i-add-commit-and-push-code-to-git-using-git-bash-command-at-once#answer-76776375), and submit a [pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/creating-a-pull-request) to the `main` branch.

## Submitting Changes
- **Branching Strategy**: Create a feature branch for your work. For example, `feat/my-new-feature`.
- **Commit Messages**: Follow this format: `Add contributing guidelines`.
- **Pull Requests**: Ensure your PR includes **tests** and follows our coding standards. A small **description** of the changes as well as the **issue number** (if applicable).

### Pull request example :arrow_down:

> Added contributing guidelines to make it easier for new contributors to get started.
>
> **Changes Made:**
> 
> - Added a new CONTRIBUTING.md file.
> - Updated the README.md file to include a link to the new CONTRIBUTING.md file.

## Creating a Development Environment
### Prerequisites
- [Node](https://nodejs.org/en/download/prebuilt-installer/current) v16+ (npm v8+)
- [.NET](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) SDK 7.x
- [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (.NET 5 or later)

### Manually Start the Project
1. Fork and clone the repository and navigate to the project directory.
```bash
git clone https://github.com/<Your github username>/NetPad.git && cd NetPad
```

#### NetPad as an Electron desktop app :wrench:

1. Run the SPA (Single Page Application)

```bash
# Install dependencies
cd src/Apps/NetPad.Apps.App/ElectronHostHook
npm install

cd ../App
npm install

# Start the development web server
npm start
```

2. Run the .NET app
Install the Electron.NET CLI tool

> [!IMPORTANT]
> To be able to run the electronize command with Electron.NET CLI, you need to have .NET 6 SDK installed.

```bash
dotnet tool install ElectronNET.CLI -g
```

3. Start the app

> [!NOTE]
> Only the very first `electronize start` is slow. Later runs are much faster!

```bash
cd src/Apps/NetPad.Apps.App/

# Start in watch mode (remove '/watch' to run without watch mode)
electronize start /watch /manifest electron.manifest.dev.js

# For macOS ARM, append the following to the 'electronize start' command
/target custom "osx-arm64;mac" /electron-arch arm64
```

#### NetPad as a Web App (accessed from a browser) :spider_web:

> [!NOTE]
> SQLite database connections are not supported when NetPad is ran as a web app in the browser.

1. Run the SPA (Single Page Application)

```bash
# Install dependencies
cd src/Apps/NetPad.Apps.App/
npm install
```

2. Start the development web server
```bash
cd src/Apps/NetPad.Apps.App/
dotnet watch run --environment Development
```

Access the app via your web browser at [http://localhost:57940](http://localhost:57940).

### Packaging :package:

The Electron app is built and packaged using
[electron-builder](https://www.electron.build/). Configuration is in the `electron.manifest.js`
file.

Build the app for the desired platform from the root directory of the `NetPad.Apps.App` project:

```bash
# For x64:
electronize build /target win /manifest electron.manifest.js /PublishSingleFile false
electronize build /target osx /manifest electron.manifest.js /PublishSingleFile false
electronize build /target linux /manifest electron.manifest.js /PublishSingleFile false

# For macOS ARM:
electronize build /target custom "osx-arm64;mac" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false
```

Packaged files can be found in the `bin/Desktop` folder.

> [!NOTE]
> To Build `flatpak` files the `flatpak` and `flatpak-builder` packages need to be installed. On Linux, the `/scripts/package-electron.sh` script can be used to package the Electron app, in which case packaged files can be found in the `/dist/` folder.

See the [Electron.NET docs](https://github.com/ElectronNET/Electron.NET#-build)
for additional CLI options when packaging the app,
and [electron-builder](https://www.electron.build/) for additional configuration
options.

## Issue Reporting
- **Bug Reports**: Include steps to reproduce and environment details.
- **Feature Requests**: Clearly describe the feature and why it’s needed.

## Documentation Guidelines
Documentation contributions are welcome. And includes in the `/docs` directory as well as in the [README.md](./README.md) and [CONTRIBUTING.md](./CONTRIBUTING.md) files.

* Don't add images or other media files with excessive size.
* Use [camelCase](https://en.wikipedia.org/wiki/Camel_case) for directory names.
* Use [Title Case](https://apastyle.apa.org/style-grammar-guidelines/capitalization/title-case) for file names.

> [!IMPORTANT]
> The naming conventions mentioned above do not apply for the .NET projects nor the Electron projects. Follow the conventions of the respective technologies.

## License
By contributing, you agree that your contributions will be licensed under the [MIT LICENSE](./LICENSE).

## Contact
If you have any questions, feel free to open an issue or contact us at our [Discord Server](https://discord.gg/FrgzNBYQFW)

At our Discord, you can find the latest announcements, troubleshooting, ideas and feedback, and more.

## Acknowledgments
<div style="display: flex; align-items: center; gap: 7px">Thank you for your contributions and helping make <img width="20px" src="https://github.com/tareqimbasher/netpad/blob/main/src/Apps/NetPad.Apps.App/wwwroot/logo/circle/32x32.png?raw=true"/><b>NetPad</b> better!</div>
