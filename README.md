# NetPad
A cross-platform C# editor and playground.

## Motivation
We love [LinqPad](https://www.linqpad.net/), but we miss its tremendous utility when 
running on non-Windows platforms. This is an effort to develop an open-source, web-enabled, 
cross-platform alternative. 

## Tech Stack
* Electron.NET ([github](https://github.com/ElectronNET/Electron.NET))
* ASP.NET Core
* Aurelia 2 ([docs](https://docs.aurelia.io/))

When the Electron app is started, it launches an ASP.NET Core backend that serves 
the Aurelia SPA app. Communication between the SPA and ASP.NET Core backend occurs 
via REST API calls and Electron's IPC protocol.

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

### Resources:
* Docs: [Go](https://github.com/tareqimbasher/NetPad/tree/main/docs)
* More on SPA app startup and running tests: [Go](https://github.com/tareqimbasher/NetPad/tree/main/src/Apps/NetPad.Web/App)
