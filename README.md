# NetPad

A cross-platform C# editor and playground.

[![build](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml/badge.svg)](https://github.com/tareqimbasher/NetPad/actions/workflows/build.yml)

![preview-main](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview-main.png?raw=true)

## Motivation

We love [LinqPad](https://www.linqpad.net/), but we also miss its tremendous
utility when running on non-Windows platforms. This is an effort to develop an
open-source, web-enabled, cross-platform alternative.

## Contribution

This is a work in progress, and still in its infancy. We welcome all
contributions!

You can currently:

* Write, save and run your own scripts
* Add additional namespaces to your script
* Reference and use your own assemblies in scripts
* Add and use NuGet packages in scripts (`Basic Support`)
* Dump complex objects to the results console

You cannot, yet:

* More complete NuGet package support (`Coming soon!`)
* Add database connections
* Enjoy code-completion features in the editor

## Tech Stack

* Electron.NET ([github](https://github.com/ElectronNET/Electron.NET))
* ASP.NET Core
* Aurelia 2 ([docs](https://docs.aurelia.io/))

When the Electron app is started, it launches an ASP.NET Core backend that
serves the Aurelia SPA app. Communication between the SPA and ASP.NET Core
backend occurs via REST API calls and Electron's IPC protocol.

## Getting Started

#### 1. Run the SPA

```
# Install dependencies
cd Apps/NetPad.Apps.App/App
npm install

# Start the development web server
npm start
```

#### 2. Run the main app

NetPad can run as an Electron app, or as a regular web application and accessed
with a web browser.

##### Electron

```
# Install the Electron.NET CLI tool if you don't have it already
dotnet tool install ElectronNET.CLI -g

# Start the app in watch mode
cd Apps/NetPad.Apps.App
electronize start /watch /manifest electron.manifest.js

# Or start the app without watch mode
electronize start /manifest electron.manifest.js /PublishSingleFile false
```

###### Note

> Only the first electronize start is slow. Later runs are much faster.

##### Web Application

```
cd Apps/NetPad.Apps.App
dotnet run
```

and access the app via your web browser, ex: `http://localhost:5000`

## Packaging

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

## Resources:

* Docs: [Go](https://github.com/tareqimbasher/NetPad/tree/main/docs)
* More on SPA app startup and running
  tests: [Go](https://github.com/tareqimbasher/NetPad/tree/main/src/Apps/NetPad.Web/App)
