# NetPad

A cross-platform C# editor and playground.

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

You cannot, yet:

* Dump complex objects to the results console (`Coming soon!`)
* More complete NuGet package support
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
cd Apps/NetPad.Web/App
npm install

# Start the development web server
npm start
```

#### 2. Run the Electron app

```
# Install the Electron.NET CLI tool if you don't have it already
dotnet tool install ElectronNET.CLI -g

# Start the app
cd Apps/NetPad.Web/App
electronize start /manifest electron.manifest.js

# Or start the app in watch mode
electronize start /watch /manifest electron.manifest.js
```

#### Note

> Only the first electronize start is slow. Later runs are much faster.

## Packaging

The app is built and packaged
using [electron-builder](https://www.electron.build/). Build the app for the
desired platform from the root directory of `NetPad.Web`
project:

```
electronize build /target win /manifest electron.manifest.js
electronize build /target osx /manifest electron.manifest.js
electronize build /target linux /manifest electron.manifest.js
```

See the [Electron.NET docs](https://github.com/ElectronNET/Electron.NET#-build)
on how to package the app for additional CLI options,
and [electron-builder](https://www.electron.build/) for additional configuration
options for the `build`
object in `electron.manifest.js`.

### Bug

(**Update:** Found the issue and a fix, still working on the permanent change.)

There is a bug I've been experiencing when packaging for linux and running the
app where the packaged app fails to run. Not sure if this bug also manifests
itself on other platforms, I haven't tested packaging on platforms other than
linux so far.

To fix, do the following:

1. Add `"asar": false` in `electron.manifest.json` under the `"build"` node.
2. Run `electronize build /target linux`
3. Go to `NetPad.Web/bin/Desktop/linux-unpacked/resources`, and move the `bin`
   directory inside the `app` directory.
4. You can then run the `net-pad` executable inside the `linux-unpacked` dir.

## Resources:

* Docs: [Go](https://github.com/tareqimbasher/NetPad/tree/main/docs)
* More on SPA app startup and running
  tests: [Go](https://github.com/tareqimbasher/NetPad/tree/main/src/Apps/NetPad.Web/App)
