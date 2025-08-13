import {app} from "electron";

/**
 * Wires up a "native paste" keyboard handler for Monaco in newer Electron versions.
 *
 * Background:
 *   - This issue surfaced after switching from Electron.NET (which used Electron v23) to
 *     ElectronSharp (which uses Electron v36).
 *   - In recent Chromium/Electron builds (Electron 34+), `document.execCommand('paste')`
 *     (which Monaco uses internally) no longer works.
 *   - This causes Cmd+V / Ctrl+V and Monaco's context menu "Paste" to silently fail,
 *     even if the copied text came from within the same app.
 *   - Using Electron's `webContents.paste()` triggers the OS-native paste command,
 *     which works reliably with Monaco.
 *
 * TODO: Remove this workaround when Monaco updates to use Electron's native clipboard API. Upstream issue:
 *       https://github.com/microsoft/monaco-editor/issues/4855
 */
export class MonacoPasteFix {
    public static init() {
const isMac = process.platform === "darwin";
app.on("browser-window-created", (_, win) => {
    win.webContents.on("before-input-event", (event, input) => {
        const isCmdOrCtrl = isMac ? input.meta === true : input.control === true;

        const hasShift =
            input.shift === true ||
            input.modifiers.includes("shift");

        const hasAlt =
            input.alt === true ||
            input.modifiers.includes("alt");

        // Prefer code (layout-agnostic)
        const isV = input.code === "KeyV" || input.key === "v";

        const shouldPaste =
            input.type === 'keyDown' &&
            isCmdOrCtrl &&
            !hasShift &&
            !hasAlt &&
            isV;

        if (shouldPaste) {
            // Native paste path (works with Monaco)
            win.webContents.paste();
            event.preventDefault();
        }
    });
});
    }
}
