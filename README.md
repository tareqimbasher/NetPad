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

* [.NET SDK](https://dotnet.microsoft.com/en-us/download) (v6 or greater)

Additional requirements only if you plan to create and use database connections:

* [.NET 6 Runtime](https://dotnet.microsoft.com/en-us/download) (not needed if .NET 6 SDK is
  installed)
* [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (.NET 5 or later)

## Download

[Download](https://github.com/tareqimbasher/NetPad/releases) the latest version for free!

For folks on **Arch Linux** or **Manjaro**, you can also install it from
the [AUR](https://aur.archlinux.org/packages/netpad-bin).

On **macOS**
see [this](https://github.com/tareqimbasher/NetPad/wiki/Troubleshooting#netpad-is-damaged-and-cant-be-opened-you-should-move-it-to-the-trash)
if you have trouble opening NetPad.

## Updates

NetPad automatically checks for updates each time you start the application and will notify
you when a new version is available.

Currently, automatic updates are not supported, but this feature will be added to make updating
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

All Pull Requests, feedback and contributions are welcome!

Also a special thanks to the NetPad's wonderful <a href="https://github.com/sponsors/tareqimbasher">
sponsors</a>. Sponsorships help pay for macOS builds and helps me maintain this project.

<a href="https://github.com/mattjcowan"><img src="https://github.com/mattjcowan.png" width="50px" alt="mattjcowan" /></a>
&nbsp;&nbsp;
<a href="https://github.com/lpreiner"><img src="https://github.com/lpreiner.png" width="50px" alt="lpreiner" /></a>
&nbsp;&nbsp;
<a href="https://github.com/jdonohoo"><img src="https://github.com/jdonohoo.png" width="50px" alt="jdonohoo" /></a>
&nbsp;&nbsp;
<a href="https://github.com/ChristopherHaws"><img src="https://github.com/ChristopherHaws.png" width="50px" alt="ChristopherHaws" /></a>
&nbsp;&nbsp;
<a href="https://github.com/smashedtoatoms"><img src="https://github.com/smashedtoatoms.png" width="50px" alt="smashedtoatoms" /></a>
&nbsp;&nbsp;
<a href="https://github.com/pelsedyr"><img src="https://github.com/pelsedyr.png" width="50px" alt="pelsedyr" /></a>
&nbsp;&nbsp;

If you enjoy using NetPad and would like to support its continued development,
consider [sponsoring](https://github.com/sponsors/tareqimbasher) the project. A small contribution,
like the cost of a cup of coffee ($5) per month, helps immensely with maintenance and the addition
of new features. Thank you for your support! :heart:

Join our [Discord server](https://discord.gg/FrgzNBYQFW) to collaborate with the community.

## Features :rocket:

* The basics:
    * Write, save and run your own scripts
    * Manage namespaces
    * Standard code editor features powered by Monaco editor
    * Auto-open unsaved scripts from previous session on launch
* Dump complex objects to the results console
* Export results to Excel or HTML
* Choose the .NET SDK version you want to use per script
* Add database connections and query them with LINQ
* Run SQL queries
* Add NuGet packages
* Reference assemblies from disk
* Syntax Tree Visualizer
* User-defined results styling
* Monaco Editor & OmniSharp offer an editor experience similar to Visual Studio Code:
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
* Support for more database providers
* Hyperlink driven Lazy-loading of results, and a DataGrid view
* Benchmark your code
* Referencing other scripts
* IL Visualizer
* Export a script as a "ready to run" .NET app
* Export a script as a C# project
* Ability to run a script from the command-line
* Git tracking of script changes
* Quality of Life:
    * Workspaces/Sessions
* Plugins & Theming

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

NetPad runs an ASP.NET web app that hosts a web-based user interface. It can be
packaged as an Electron desktop app or served and accessed on any browser.

Communication between the user interface and the ASP.NET backend occurs via HTTP
and SignalR.

## Build

This section describes how to build and run NetPad from source.
NetPad can be run as an Electron desktop app or as a web application
accessed with a web browser.

### Requirements

* Node v16+ (npm v8+)
* .NET SDK 7.x
* EF Core tools 5.x or later

### NetPad as an Electron desktop app

#### 1. Run the SPA

```
# Install dependencies
cd src/Apps/NetPad.Apps.App/ElectronHostHook
npm install

cd ../App
npm install

# Start the development web server
npm start
```

#### 2. Run the .NET app

Install the Electron.NET CLI tool:

```
dotnet tool install ElectronNET.CLI -g
```

Start the app:

```
cd src/Apps/NetPad.Apps.App

# Start in watch mode (remove '/watch' to run without watch mode)
electronize start /watch /manifest electron.manifest.dev.js

# For macOS ARM, append the following to the 'electronize start' command
/target custom "osx-arm64;mac" /electron-arch arm64
```

###### Note

> Only the very first `electronize start` is slow. Later runs are much faster.

### NetPad as a Web app (accessed from a browser)

#### 1. Run the SPA

```
# Install dependencies
cd Apps/NetPad.Apps.App/App
npm install

# Start the development web server
npm run start-web
```

#### 2. Run the .NET app

```
cd Apps/NetPad.Apps.App
dotnet watch run --environment Development
```

and access the app via your web browser, ex: `http://localhost:57940`

## Packaging :package:

The Electron app is built and packaged using
[electron-builder](https://www.electron.build/). Configuration is in the `electron.manifest.js`
file.

Build the app for the desired platform from the root directory of the `NetPad.Apps.App` project:

```
# For x64:
electronize build /target win /manifest electron.manifest.js /PublishSingleFile false
electronize build /target osx /manifest electron.manifest.js /PublishSingleFile false
electronize build /target linux /manifest electron.manifest.js /PublishSingleFile false

# For macOS ARM:
electronize build /target custom "osx-arm64;mac" /electron-arch arm64 /manifest electron.manifest.js /PublishSingleFile false
```

Packaged files can be found in the `bin/Desktop` folder.

###### Notes

> 1. To build flatpak files the `flatpak` and `flatpak-builder` packages need to be installed.
> 2. On Linux, the `/scripts/package-electron.sh` script can be used
     > to package the Electron app, in which case packaged files can be found in
     > the `/dist/` folder.


See the [Electron.NET docs](https://github.com/ElectronNET/Electron.NET#-build)
for additional CLI options when packaging the app,
and [electron-builder](https://www.electron.build/) for additional configuration
options.

## Resources :books::

* Docs: [Go](https://github.com/tareqimbasher/NetPad/tree/main/docs)
* More on SPA app startup and running
  tests: [Go](https://github.com/tareqimbasher/NetPad/tree/main/src/Apps/NetPad.Apps.App/App)
