/**
 * The operating systems the UI tells apart. Only macOS differs: Cmd where others use Ctrl, modifier
 * glyphs, and an application menu.
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
