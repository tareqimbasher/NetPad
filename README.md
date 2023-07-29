# <div style="display: flex; align-items: center; gap: 7px"><img src="https://github.com/tareqimbasher/netpad/blob/main/src/Apps/NetPad.Apps.App/wwwroot/logo/circle/32x32.png?raw=true" /> NetPad</div>

**A cross-platform C# editor and playground.**

[![build](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml)
[![CodeQL](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml)

|                                               Dark                                                |                                               Light                                                |
|:-------------------------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------:|
| ![](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview-main-dark.png?raw=true) | ![](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview-main-light.png?raw=true) |

## Motivation

We love [LINQPad](https://www.linqpad.net/), but we also miss its tremendous utility when not working on Windows. This
is an effort to develop an open-source, web-enabled, cross-platform alternative.

The goal isn't to reach 100% feature parity with LinqPad, but to offer an acceptable alternative that covers the
features developers most commonly need, and if we're lucky, a few new useful ones!

#### If you like the project, please star it :star2:!

## Requirements

The following must be be installed to use NetPad:

* [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet) (v6 or v7)
* [EF Core tools (.NET 5 or higher)](https://learn.microsoft.com/en-us/ef/core/cli/dotnet):
  (*optional*) needed to create and use database connections.

## Download

[Download](https://github.com/tareqimbasher/NetPad/releases/latest) the latest version or
an older version from the [Releases](https://github.com/tareqimbasher/NetPad/releases) page.

**Note:** NetPad is not a signed application. On macOS when you run NetPad the first time you will prompted with a message saying the app is damaged and can't be opened. To resolve this run this in a terminal:

```shell
sudo xattr -cr /path/to/NetPad.app
```

## Updates

NetPad checks for updates on startup and will let you know when a new version is available.
While it cannot _yet_ install the latest update automatically, that feature is in the works!

The latest version can be downloaded from
the [Releases](https://github.com/tareqimbasher/NetPad/releases/latest) page.

## Contribution

This is a work in progress and still in preview. All contributions are welcome!

If you like NetPad, please consider [sponsoring](https://github.com/sponsors/tareqimbasher) it with a cup of coffee ($5)
a month, every bit helps me maintain this project and deliver more features. Thank you :heart:

Join our new [Discord server](https://discord.gg/FrgzNBYQFW) to collaborate with the community.

## Features :rocket:

* The basics:
    * Write, save and run your own scripts
    * Manage namespaces
    * Standard code editor features powered by Monaco editor
* Dump complex objects to the results console and export results to Excel or HTML
* Choose the .NET SDK version you want to use per script
* Add and use database connections (supports Microsoft SQL Server and PostgreSQL, more coming soon!)
* Run SQL queries
* Auto-save: When you close NetPad, it will auto-save your changes and restore them the next time you fire it up
* Add NuGet packages
* Reference assemblies from disk
* Monaco Editor & OmniSharp offer an editor experience similar to Visual Studio Code:
    * Code completion (Intellisense)
    * Semantic highlighting
    * CodeLens
    * Inlay hits
    * Hover for documentation
    * Format document
    * Go-to implementation
    * Find references
    * Action suggestions
    * Diagnostics
    * Document highlighting

## Roadmap :construction:

* Debugging
* Support for more database providers
* Hyperlink driven Lazy-loading of results, and a DataGrid view
* Benchmark your code
* Referencing other scripts
* View your code's Syntax Tree
* View your code as IL
* Export a script as a "ready to run" .NET app
* Export a script as a C# project
* Ability to run a script from the command-line
* Git tracking of script changes
* More advanced `Dump()` capabilities:
  * Dump HTML controls and interact with them
  * Dump images, SVGs, and bitmaps
  * Dump code with syntax highlighting
* Quality of Life:
    * User-defined results styling
    * Workspaces/Sessions
* More OmniSharp integrations:
    * Code fix suggestions
    * Format selection
    * Format as you type
    * Go-to definition
    * Go-to symbol
    * Rename symbol
    * ...and more
* Plugins & Theming

---

## Tech Stack :computer:

* .NET Core
* Electron.NET ([github](https://github.com/ElectronNET/Electron.NET))
* Aurelia 2 ([docs](https://docs.aurelia.io/))

### How it works: In a nutshell

NetPad runs an ASP.NET web app that hosts a web-based user interface. It can be
packaged as an Electron desktop app or served and accessed on any browser.

Communication between the user interface and the ASP.NET backend occurs via HTTP
calls and SignalR.

## Development

This section describes how to build and run NetPad from source.
NetPad can be run as an Electron desktop app or as a web application
accessed with a web browser.

### Requirements

* Node v16+ (npm v8+)
* .NET SDK 6.x
* EF Core tools 6.x (or higher)

### NetPad as an Electron desktop app

#### 1. Run the SPA

```
# Install dependencies
cd Apps/NetPad.Apps.App/App
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
cd Apps/NetPad.Apps.App

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

and access the app via your web browser, ex: `http://localhost:57930`

## Packaging :package:

The Electron app is built and packaged using
[electron-builder](https://www.electron.build/). Configuration is in the `electron.manifest.js` file.

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

###### Note

> Alternatively, on Linux, the `/scripts/package-electron.sh` script can be used
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
