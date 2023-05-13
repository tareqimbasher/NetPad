# NetPad

**A cross-platform C# editor and playground.**

[![build](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml)
[![CodeQL](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml)

|                                               Dark                                                |                                               Light                                                |
|:-------------------------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------:|
| ![](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview-main-dark.png?raw=true) | ![](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview-main-light.png?raw=true) |

## Motivation

We love [LinqPad](https://www.linqpad.net/), but we also miss its tremendous
utility when not working on Windows. This is an effort to develop an
open-source, web-enabled, cross-platform alternative.

The goal isn't to reach 100% feature parity with LinqPad, but
to offer an acceptable alternative that covers the features
developers most commonly need, and if we're lucky, a few new useful ones!

#### If you like the project, please star it :star2:!

## Requirements

The following must be be installed to run NetPad:

* [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0):
    * The environment variable `DOTNET_ROOT` (or `DOTNET_INSTALL_DIR`) must be
      set and point to the directory where .NET is installed.
    * You can have additional .NET SDKs installed but you still need .NET 6 SDK
      installed too. (.NET 7 support coming very soon!)
* [EF Core tools (.NET 6 or higher)](https://learn.microsoft.com/en-us/ef/core/cli/dotnet):
  (*optional*) needed to create and use database connections.

## Download

[Download](https://github.com/tareqimbasher/NetPad/releases/latest) the latest version or
an older version from the [Releases](https://github.com/tareqimbasher/NetPad/releases) page.

## Updates

NetPad checks for updates on startup and will let you know when a new version is available.
While it cannot _yet_ install the latest update automatically, that feature's in the works!

The latest version can be downloaded from
the [Releases](https://github.com/tareqimbasher/NetPad/releases/latest) page.

## Contribution

This is a work in progress and still in preview. All contributions are welcome!

## Features :tada:

* The basics:
    * Write, save and run your own scripts
    * Manage namespaces
    * Standard code editor features powered by Monaco editor
* Dump complex objects to the results console
* Export results to Excel or HTML
* Add and use database connections (currently only supports Microsoft SQL Server
  and PostgreSQL, more coming soon!)
* Auto-save: If you close NetPad, it will auto-save your changes and
  restore them the next time you fire it up
* Add NuGet packages
* Reference assemblies from disk
* Monaco Editor & OmniSharp offer an editor experience similar to Visual Studio Code:
    * Code completion (Intellisense)
    * CodeLens
    * Semantic highlighting
    * Hover for documentation
    * Format document
    * Go-to implementation
    * Find references
    * Action suggestions
    * Diagnostics

## Roadmap :construction:

* .NET 7 support (coming very soon..!)
* Debugging
* Support for more database providers
* Lazy-loading of results and a DataGrid view
* Run T-SQL queries
* Export a script as a "ready to run" .NET app
* Ability to run a script from the command-line
* Quality of Life:
    * Support for even more complex objects in the results console, ex: Dumping
      images
    * User-defined results styling
    * Ability to change the max result set length when dumping collections (default is 1000 items)
    * Workspaces/Sessions
* More OmniSharp integrations:
    * Code fix suggestions
    * Format selection
    * Format as you type
    * Go-to definition
    * Go-to symbol
    * Rename symbol
    * ...and more
* Plugins

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
* .NET SDK 5.x: if you plan on running the Electron app. This is not a
  dependency of NetPad itself but a dependency of the `electronize` development
  tool. It, currently, needs this to run.
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

# Start in watch mode
electronize start /watch /manifest electron.manifest.dev.js

# OR without watch mode
electronize start /manifest electron.manifest.dev.js /PublishSingleFile false
```

###### Note

> Only the first `electronize start` is slow. Later runs are much faster.

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
[electron-builder](https://www.electron.build/). Configuration is in
the `electron.manifest.js` file.

Build the app for the desired platform from the root directory
of the `NetPad.Apps.App` project:

```
electronize build /target win /manifest electron.manifest.js /PublishSingleFile false
electronize build /target osx /manifest electron.manifest.js /PublishSingleFile false
electronize build /target linux /manifest electron.manifest.js /PublishSingleFile false
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
