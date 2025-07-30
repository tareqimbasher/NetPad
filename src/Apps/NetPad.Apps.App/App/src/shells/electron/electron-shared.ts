// These are types and/or constants that are used in both the app (renderer process), and in the electron main
// process (ElectronHostHook folder). These are copied and should be kept in sync between the 2 files with the
// same name in the app and in the ElectronHostHook folder.

export const electronConstants = {
    ipcEventNames: {
        mainMenuBootstrap: "main-menu-bootstrap",
        getWindowState: "get-window-state",
        maximize: "maximize",
        minimize: "minimize",
        zoomIn: "zoom-in",
        zoomOut: "zoom-out",
        resetZoom: "reset-zoom",
        toggleFullScreen: "toggle-full-screen",
        toggleAlwaysOnTop: "toggle-always-on-top",
        toggleDeveloperTools: "toggle-developer-tools",
        openFileSelectorDialog: "open-file-selector-dialog",

        // Events that are forwarded from app eventbus to IPC Main process (see electron-event-sync.ts)
        appActivated: "AppActivatedEvent",
    }
};
