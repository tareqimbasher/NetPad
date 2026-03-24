# <div style="display: flex; align-items: center; gap: 7px"><img src="https://github.com/tareqimbasher/netpad/blob/main/src/Apps/NetPad.Apps.App/wwwroot/logo/circle/32x32.png?raw=true" /> NetPad</div>

A cross-platform C# editor and playground.

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/tareqimbasher/NetPad/build.yml?label=main)](https://github.com/tareqimbasher/NetPad/actions)
[![GitHub Release](https://img.shields.io/github/v/release/tareqimbasher/NetPad?color=%23097bbb)](https://github.com/tareqimbasher/NetPad/releases/latest)
[![npad cli version](https://img.shields.io/nuget/vpre/NetPad.CLI?label=npad%20cli&color=097bbb)](https://tareqimbasher.github.io/NetPad/#/wiki/CLI)
![GitHub commits since latest release](https://img.shields.io/github/commits-since/tareqimbasher/netpad/latest)
[![Discord](https://img.shields.io/discord/1121067424146522162?label=discord&color=%235864F2)](https://discord.gg/FrgzNBYQFW)

![NetPad preview](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview.png?raw=true)

## Get Started

NetPad is a C# playground that lets you run C# code instantly, without the hassle of creating and
managing projects. Open NetPad, start coding, hit Run, and see your output immediately. It's that
simple.

- **Prototyping and Testing:** Quickly prototype and test code snippets before incorporating them
  into your projects.
- **Data Visualization:** Visualize data interactively for better insights and analysis.
- **Database Queries:** Query databases using LINQ or SQL effortlessly.
- **Learn and Experiment:** Experiment with new C# features or start learning C# in an intuitive and
  accessible environment.
- **Utility Scripts:** Create and save your own utility or administration scripts for repeated use.

See [Features](https://github.com/tareqimbasher/NetPad?tab=readme-ov-file#features-rocket).

**If you like this project, please star it :star2: and consider [sponsoring](https://github.com/sponsors/tareqimbasher)!**

## Motivation

This project aims to create an open-source, cross-platform alternative to LINQPad. NetPad can run as
a native desktop app or in a web browser, and aims to cover the most commonly used features while 
introducing new ones of its own.

## Discord

Join the [Discord server](https://discord.gg/FrgzNBYQFW) to collaborate, ask questions and get the
latest announcements.

## Requirements

The following must be installed to use NetPad:

* [.NET SDK](https://dotnet.microsoft.com/en-us/download) (v6 or later)

Additional requirement if you plan to create and use database connections:

* [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (v6 or later)

## Download

### Official Installers

**[Download Now!](https://github.com/tareqimbasher/NetPad/releases)**

NetPad comes in 2 variants:

- **Electron:** The current stable package. Installers start with `netpad`
- **Native (vNext):** Uses a native desktop shell built with [Tauri](https://tauri.app/) instead of Electron, resulting in a lighter package that uses fewer system resources. Installers start with `netpad_vnext`

Both variants have the same features and are equally maintained. The Native (vNext) variant is planned to become the main package, at which point the Electron variant may be deprecated.

> [!NOTE]
>On **macOS**
> see [this](https://tareqimbasher.github.io/NetPad/#/wiki/Troubleshooting?id=netpad-is-damaged-and-can39t-be-opened)
> if you have trouble opening NetPad.

### Community Packages

These packages are maintained by community members.

| Installer                                                                                                                                                     | Channel    | Command                                              |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------|------------|------------------------------------------------------|
| [![AUR Version](https://img.shields.io/aur/version/netpad-bin)](https://aur.archlinux.org/packages/netpad-bin)                                                | **stable** | `yay -S netpad-bin`                                  |
| [![AUR Version](https://img.shields.io/aur/version/netpad-vnext-bin)](https://aur.archlinux.org/packages/netpad-vnext-bin)                                    | **vNext**  | `yay -S netpad-vnext-bin`                            |
| [![WinGet Package Version](https://img.shields.io/winget/v/TareqImbasher.NetPad?color=%23097bbb)](https://winstall.app/apps/TareqImbasher.NetPad)             | **stable** | `winget install --id=TareqImbasher.NetPad  -e`       |
| [![WinGet Package Version](https://img.shields.io/winget/v/TareqImbasher.NetPad.vNext?color=%23097bbb)](https://winstall.app/apps/TareqImbasher.NetPad.vNext) | **vNext**  | `winget install --id=TareqImbasher.NetPad.vNext  -e` |

## Updates

NetPad checks for updates on startup and will notify you when a new version is available.
The latest version can always be downloaded from
the [Releases](https://github.com/tareqimbasher/NetPad/releases) page.

## Wiki

The [Wiki](https://tareqimbasher.github.io/NetPad/#/wiki/) is a great place to find more information
about NetPad. See the [Troubleshooting](https://tareqimbasher.github.io/NetPad/#/wiki/Troubleshooting)
page if you run into any issues.

## Features :rocket:

* Scripting:
    * Write, save and run your own scripts
    * Manage namespaces
    * Per-script .NET SDK version selection
    * Auto-open unsaved scripts from previous session on launch
* Output & Visualization:
    * Dump complex objects to the results console
    * Export results to Excel or HTML
    * User-defined results styling
* Database:
    * Add database connections and servers
    * Query with LINQ or SQL
* Packages & References:
    * Add NuGet packages to scripts
    * Reference assemblies from disk
* CLI:
    * Run scripts from the command-line using [npad](https://tareqimbasher.github.io/NetPad/#/wiki/CLI)
* Code Analysis:
    * Syntax Tree Visualizer
    * IL Viewer
* Editor:
    * Vim keybindings
    * User Secrets management
* Code Intelligence (OmniSharp):
    * Code Completion (Intellisense)
    * Semantic Highlighting
    * CodeLens
    * Inlay Hints
    * Hover for Documentation
    * Go-to implementation
    * Find References
    * Find Symbol
    * Rename Symbol
    * Action Suggestions
    * Diagnostics
    * Document Highlighting
    * Contextual code folding
    * Format document/selection/on-type

## Recently Added in [v0.11.0](https://github.com/tareqimbasher/NetPad/releases/tag/v0.11.0) :sparkles:

* **.NET 10 Support:** Support for the .NET 10 SDK.
* **NetPad CLI (`npad`):** Run scripts, standalone C# files, and inline code directly from the terminal. Install with `dotnet tool install -g netpad.cli`. [Wiki](https://tareqimbasher.github.io/NetPad/#/wiki/CLI)
* **IL Viewer:** Inspect the generated IL for your scripts in the Code pane.
* **User Secrets Manager:** Manage and access user secrets via a dedicated pane and `Util.Secrets`. [Wiki](https://tareqimbasher.github.io/NetPad/#/wiki/Util?id=user-secrets)
* **Oracle Data Connections:** Oracle database connections are now supported.

## Planned Features :construction:

* Debugging
* Hyperlink driven Lazy-loading of results, and a DataGrid view
* Benchmark your code
* Referencing other scripts
* Export a script as a "ready to run" .NET app
* Export a script as a C# project
* Git tracking of script changes
* Workspaces/Sessions
* Plugins

## Contribution & Support

All Pull Requests, feedback and contributions are welcome! Please read
the [Contributing guidelines](./CONTRIBUTING.md) for more information about how to contribute and
build/run the project.

A special thanks to NetPad's wonderful <a href="https://github.com/sponsors/tareqimbasher">
sponsors</a>. Sponsorships help pay for macOS builds and cross-platform testing and helps me
maintain this project.

<a href="https://github.com/mattjcowan"><img src="https://github.com/mattjcowan.png" width="50px" alt="mattjcowan" /></a>
&nbsp;&nbsp;<a href="https://github.com/lpreiner"><img src="https://github.com/lpreiner.png" width="50px" alt="lpreiner" /></a>
&nbsp;&nbsp;<a href="https://github.com/ChristopherHaws"><img src="https://github.com/ChristopherHaws.png" width="50px" alt="ChristopherHaws" /></a>
&nbsp;&nbsp;<a href="https://github.com/OddSkancke"><img src="https://github.com/OddSkancke.png" width="50px" alt="OddSkancke" /></a>
&nbsp;&nbsp;<a href="https://github.com/SimonNyvall"><img src="https://github.com/SimonNyvall.png" width="50px" alt="SimonNyvall" /></a>
&nbsp;&nbsp;

If you enjoy using NetPad and would like to support its continued development,
consider [sponsoring](https://github.com/sponsors/tareqimbasher) the project. A small contribution
helps immensely with maintenance and the addition of new features.
Thank you for your support! :heart:

<br/>
<br/>
<img src="https://api.star-history.com/svg?repos=tareqimbasher/NetPad&type=Date" />
<br/>
<br/>

## Tech Stack :computer:

* **.NET** for the backend.
* **ElectronSharp** ([github](https://github.com/theolivenbaum/electron-sharp)) for the Electron
  desktop app and Tauri ([docs](https://tauri.app/)) for the native desktop app.
* **Aurelia 2** ([docs](https://docs.aurelia.io/)) for the UI.

## Build

See [CONTRIBUTING.md](./CONTRIBUTING.md) for instructions on how to build and run NetPad from
source. NetPad can be run as a desktop app or as a web application accessed with a web browser.

## Resources :books::

* [Wiki](https://tareqimbasher.github.io/NetPad)
* [Build & Run](./CONTRIBUTING.md)
* [Technical Docs](https://tareqimbasher.github.io/NetPad/#/technical-docs/)
