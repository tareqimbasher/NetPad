# <div style="display: flex; align-items: center; gap: 7px"><img src="https://github.com/tareqimbasher/netpad/blob/main/src/Apps/NetPad.Apps.App/wwwroot/logo/circle/32x32.png?raw=true" /> NetPad</div>

A cross-platform C# editor and playground.

[![build](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml)
[![CodeQL](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml)

![](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview.png?raw=true)

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

#### If you like this project, please star it :star2: and consider [contributing](https://github.com/sponsors/tareqimbasher).

## Motivation

We love LINQPad, but we miss its tremendous utility when working on non-Windows platforms.
This project aims to create an open-source, web-enabled, cross-platform alternative.

The goal isn't to reach 100% feature parity with LINQPad, but to offer an effective alternative that
covers features most commonly used and to introduce a few new useful ones.

## Requirements

The following must be installed to use NetPad:

* [.NET SDK](https://dotnet.microsoft.com/en-us/download) (v6 or later)

Additional requirement if you plan to create and use database connections:

* [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (.NET 5 or later)

## Download

You can download installers for Windows, macOS and Linux from
the [Releases](https://github.com/tareqimbasher/NetPad/releases) page.

On **macOS**
see [this](https://github.com/tareqimbasher/NetPad/wiki/Troubleshooting#netpad-is-damaged-and-cant-be-opened-you-should-move-it-to-the-trash)
if you have trouble opening NetPad.

### Community

The community also maintains these packages:

**Windows**

    winget install NetPad
    winget install "NetPad vNext"

**Arch Linux (AUR)**

    yay -S netpad-bin

## Updates

NetPad automatically checks for updates each time you start the application and will notify
you when a new version is available.

Automatic updates are not supported, but will be added in the future to make updating
as seamless as possible. Stay tuned for future updates!

The latest version can be downloaded from
the [Releases](https://github.com/tareqimbasher/NetPad/releases) page.

## Wiki

The [Wiki](https://github.com/tareqimbasher/NetPad/wiki) is a great place to find more information
about NetPad.

## Troubleshooting

See the [Troubleshooting](https://github.com/tareqimbasher/NetPad/wiki/Troubleshooting) section of
the Wiki.

## Contribution

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

Join our [Discord server](https://discord.gg/FrgzNBYQFW) to collaborate and see the latest
announcements.

## Features :rocket:

* The basics:
    * Write, save and run your own scripts.
    * Manage namespaces.
    * Standard code editor features powered by Monaco editor.
    * Auto-open unsaved scripts from previous session on launch.
* Dump complex objects to the results console and export results to Excel or HTML.
* Choose the .NET SDK version you want to use per script.
* Add database connections and query them with LINQ or T-SQL.
* Add NuGet packages and reference assemblies from disk.
* Syntax Tree Visualizer.
* User-defined results styling.
* LSP powered by OmniSharp:
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

## Roadmap :construction:

* Debugging
* Support for more database providers (Oracle, Mongo...etc)
* Hyperlink driven Lazy-loading of results, and a DataGrid view
* Benchmark your code
* Referencing other scripts
* Ability to run a script from the command-line
* IL Visualizer
* Export a script as a "ready to run" .NET app
* Export a script as a C# project
* Git tracking of script changes
* Workspaces/Sessions
* Plugins

<br/>
<br/>
<img src="https://api.star-history.com/svg?repos=tareqimbasher/NetPad&type=Date" />
<br/>
<br/>

## Tech Stack :computer:

* .NET
* Electron.NET ([github](https://github.com/ElectronNET/Electron.NET))
* Aurelia 2 ([docs](https://docs.aurelia.io/))

### How it works

NetPad runs an ASP.NET web app that hosts a web user interface. It can be
packaged as a desktop app or served and accessed on any browser.

Communication between the user interface and the ASP.NET backend occurs via HTTP
and SignalR.

## Build

See [CONTRIBUTING.md](./CONTRIBUTING.md) for instructions on how to build and run NetPad from
source. NetPad can be run as a desktop app or as a web application accessed with a web browser.

## Resources :books::

* Docs: [Go](https://github.com/tareqimbasher/NetPad/tree/main/docs)
* More on SPA app startup and running
  tests: [Go](https://github.com/tareqimbasher/NetPad/tree/main/src/Apps/NetPad.Apps.App/App)
* Build: [Go](./CONTRIBUTING.md#manually-start-the-project)
