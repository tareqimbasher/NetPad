# Startup

As explained in the previous section, there are 3 shells that NetPad can run in. The way NetPad is started using each
shell is slightly different.

## Electron Shell

The following happens when a user starts the Electron version of NetPad:

1. The Electron main process starts first.
2. The Electron process opens a websocket that is used for two-way communication between .NET and Electron.
3. The Electron process then starts the .NET app passing it the port on which the websocket is listening. This port
   is used to initialize the ElectronSharp library and tells the .NET app that the Electron shell is in use.
4. After the .NET app has initialized, it calls into ElectronSharp to open the main NetPad window.
5. When the window opens, it loads the JS app by navigating to the proper URL hosted by the .NET app.

## Tauri Shell

The following happens when a user starts the Tauri version of NetPad:

1. The Rust app starts first and opens the main NetPad window.
2. It then starts the .NET app and passes it the `--tauri` argument. This tells the .NET app that the Tauri shell is in
   use.
3. The window that was opened waits for the .NET app to finish starting and become available.
4. When the .NET app starts, the window loads the JS app by navigating to the proper URL hosted by the .NET app.

## Web Browser Shell

This shell is not shipped with NetPad. It is your own web browser. In this case, NetPad is hosted on a server (or run
locally) and navigated to via a URL in your web browser.