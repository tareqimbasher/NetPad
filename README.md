# NetPad

A cross-platform C# editor and playground.

![preview-progressbars](https://github.com/tareqimbasher/netpad/blob/main/docs/images/preview-http.png?raw=true)

## Motivation

We love [LinqPad](https://www.linqpad.net/), but we miss its tremendous utility
when running on non-Windows platforms. This is an effort to develop an
open-source, web-enabled, cross-platform alternative.

## Contribution
This is a work in progress, and still in its infancy. We welcome all contributions!

You can currently:
* Write, save and run your own scripts
* Add additional namespaces to your script

You cannot, yet:
* Dump complex objects to the results console
* Add database connections
* Reference NuGet packages or your own assemblies
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
# Install deps
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
electronize start /watch
```

Be sure to edit your script directory in `appsettings.json`

#### Note

> Only the first electronize start is slow. Later runs are much faster.

## Packaging

Build the app for the desired platform from the root directory of `NetPad.Web`
project:

```
electronize build /target win
electronize build /target osx
electronize build /target linux
```

See the Electron.NET docs
on [how to package the app](https://github.com/ElectronNET/Electron.NET#-build)
for more details.

### Bug
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
