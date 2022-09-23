# NetPad

A cross-platform C# editor and playground.

[![build](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml)
[![CodeQL](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/codeql-analysis.yml)

![preview-main](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview-main.png?raw=true)

## Motivation

We love [LinqPad](https://www.linqpad.net/), but we also miss its tremendous
utility when running on non-Windows platforms. This is an effort to develop an
open-source, web-enabled, cross-platform alternative.

## Requirements

* [.NET SDK 6.x](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [EF Core tools 6.x](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

## Contribution

This is a work in progress and still in alpha. All
contributions are welcome!

## Features :tada:

* The basics:
    * Write, save and run your own scripts
    * Manage namespaces
    * Standard code editor features
* Dump complex objects to the results console
* Auto-save: If you close NetPad, it will auto-save your changes and
  restore them the next time you fire it up
* Add NuGet packages
* Reference assemblies from disk
* OmniSharp integration:
    * CodeLens
    * Code completion (Intellisense)
    * Semantic highlighting
    * Hover for documentation
    * Format document
    * Go-to implementation
    * Find references
    * Action suggestions
    * Diagnostics

## Roadmap :construction:

* Add database connections
* More OmniSharp integrations:
    * Code fix suggestions
    * Format selection
    * Format as you type
    * Go-to definition
    * Go-to symbol
    * Rename symbol
    * ...and more
* Workspaces/Sessions
* More results console features:
    * Support for more complex objects in the results console, ex: Dumping
      images
    * Exporting results to different file formats, or to the clipboard
    * CTRL-F to find text
* Plugins

## Tech Stack :computer:

* Electron.NET ([github](https://github.com/ElectronNET/Electron.NET))
* ASP.NET Core
* Aurelia 2 ([docs](https://docs.aurelia.io/))

When the Electron app is started, it launches an ASP.NET Core backend that
serves the Aurelia SPA app. Communication between the SPA and ASP.NET Core
backend occurs via REST API calls and Electron's IPC protocol.

## Development

### Requirements

* Node v18+ (npm v8+)
* .NET SDK 6.x
* EF Core tools 6.x

### Running the app

NetPad can be run as a desktop app, via Electron, or as a web application
accessed with a web browser.

#### NetPad as a Desktop app

#### 1. Run the SPA

```
# Install dependencies
cd Apps/NetPad.Apps.App/App
npm install

# Start the development web server
npm start
```

#### 2. Run the .NET app

The first time you run the Electron version of NetPad:

```
# Install the Electron.NET CLI tool
dotnet tool install ElectronNET.CLI -g
```

Then:

```
# Start the app in watch mode
cd Apps/NetPad.Apps.App
electronize start /watch /manifest electron.manifest.js

# Or to start the app without watch mode
electronize start /manifest electron.manifest.js /PublishSingleFile false
```

###### Note

> Only the first electronize start is slow. Later runs are much faster.

#### NetPad as a Web app

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
dotnet run
```

and access the app via your web browser, ex: `http://localhost:5000`

## Packaging :package:

The app is built and packaged
using [electron-builder](https://www.electron.build/). Build the app for the
desired platform from the root directory of `NetPad.Web`
project:

```
electronize build /target win /manifest electron.manifest.js /PublishSingleFile false
electronize build /target osx /manifest electron.manifest.js /PublishSingleFile false
electronize build /target linux /manifest electron.manifest.js /PublishSingleFile false
```

See the [Electron.NET docs](https://github.com/ElectronNET/Electron.NET#-build)
for additional CLI options when packaging the app,
and [electron-builder](https://www.electron.build/) for additional configuration
options.

## Resources :books::

* Docs: [Go](https://github.com/tareqimbasher/NetPad/tree/main/docs)
* More on SPA app startup and running
  tests: [Go](https://github.com/tareqimbasher/NetPad/tree/main/src/Apps/NetPad.Apps.App/App)
