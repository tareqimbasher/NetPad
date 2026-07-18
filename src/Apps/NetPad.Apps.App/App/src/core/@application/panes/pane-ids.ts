/**
 * The ids of the panes that ship with the app.
 *
 * A pane id is protocol vocabulary, like a command id: it lets app-wide code (shortcuts, menus,
 * the command palette) target a pane without depending on the window that implements it.
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
