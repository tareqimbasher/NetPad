# Concepts

## Shell

A shell is a container where the SPA is rendered within. There are 3 types of shells:

1. **Web Browser:** SPA is inside a regular web browser.
2. **Electron:** SPA is loaded inside an Electron window.
3. **Tauri:** SPA is loaded inside a Tauri window.

The shell is responsible for native-platform features like native menus, window management (create,
minimize, maximize...etc.), desktop notifications, taskbar functions...etc.

NetPad abstracts interactions with the shell. The `/shells` folder contains the implementations of
these abstractions for all 3 shells.

## Windows

A window is an application entry point. When a physical window is created/opened, we need to know
which part of the SPA app should be rendered. For example, the "page" that is rendered when the Main
window is opened is different that the "page" that is rendered the Settings window is opened.

The `/windows` folder contains all the windows that make up the application. Each window in this
folder is an application entry-point, and can be thought of as its own "mini-app". When a physical
window is opened, `main.ts` runs first which will then run the proper `windows/*/window.ts` file
that corresponds to the requested window.

For example, when a physical window is opened for "Settings," `main.ts` will run the file
`/windows/settings/window.ts`. But when the "Main" window is opened, `main.ts` will run the file
`/windows/main/window.ts`.

# Startup

When a physical window is opened `main.ts` is always the first file that runs. It will read the URL
which will tell it which shell and which window was requested. Using this information, it will:

1. Configure and bootstrap the app for the requested shell (ie. use the implementation that
   corresponds to the requested shell: Electron, Tauri, Web) by calling the proper
   `/shells/*/*-shell.ts` file.
2. Then configure and bootstrap the requested window (entry-point) by calling the proper
   `/windows/*/window.ts` file.

# Dependency Registration

Dependencies are registered in multiple places:

1. First, dependencies that are common to all shells and windows (entire app) are registered in
   `main.ts`.
2. Then, dependencies that are shell-specific are registered inside `/shells/*/*-shell.ts` files.
3. Finally, dependencies that the window-specific ("mini-app" specific) are registered inside
   `/windows/*/window.ts` files.

# Folders

- `core/@application`: contains app-specific re-usable code used throughout the entire application.
  This includes things like components, services, API client code...etc
- `core/@common`: contains generic re-usable code that is not app or framework specific.
- `core/@plugins`: contains NetPad plugins
- `shells`: contains implementations for each of the 3 shells.
- `styles`: contains all app-wide CSS styles.
- `windows`: contains all windows that make up the application.
