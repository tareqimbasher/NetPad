/**
 * The ids of the panes that ship with the app. They let app-wide code target a pane without depending
 * on the window that implements it.
 */
export const PaneIds = {
    output: "output",
    explorer: "explorer",
    namespaces: "namespaces",
    code: "code",
    clipboard: "clipboard",
    memCache: "mem-cache",
    notifications: "notifications",
    secretsManager: "secrets-manager",
} as const;

export type PaneId = typeof PaneIds[keyof typeof PaneIds];
