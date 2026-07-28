/**
 * The operating-system families the UI has to tell apart. Only macOS diverges: it uses Cmd where
 * every other platform uses Ctrl, renders modifiers as glyphs, and expects an application menu.
 */
export type OperatingSystem = "macos" | "other";

function detect(): OperatingSystem {
    // userAgentData is the non-deprecated source, but is Chromium-only; WebKitGTK (the Tauri
    // shell on Linux) and Firefox still need the platform string.
    const platform =
        (navigator as Navigator & { userAgentData?: { platform?: string } }).userAgentData?.platform
        ?? navigator.platform
        ?? "";

    return /mac/i.test(platform) ? "macos" : "other";
}

/**
 * The OS the UI is running on.
 */
export const currentOs: OperatingSystem = detect();
