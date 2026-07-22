import {ThemeMode} from "@application";
import {AppTheme, ThemeGround} from "./app-theme";
import {ThemeTokens} from "./theme-tokens";

export interface ThemeBootEntry {
    cssClasses: string;
    groundColor: string;
}

export interface ThemeBootPayload {
    mode: ThemeMode;
    dark: ThemeBootEntry;
    light: ThemeBootEntry;
}

/**
 * Mirrors the active theme into localStorage so that `index.html` can paint the window in the
 * right color before the SPA (and with it the real settings) has loaded. Settings remain the
 * source of truth, this is only a cache and may be missing or stale.
 *
 * Both grounds are cached rather than the resolved one: in `System` mode the ground follows a
 * machine preference that can change while the app is closed, so the resolution has to happen when
 * the window boots, not when the cache was written.
 *
 * The reader is an inline script at the top of `index.html`. The key and the payload shape must
 * stay in sync with it.
 */
export class ThemeBootCache {
    public static readonly storageKey = "netpad.theme.boot";

    /** Caches both of a family's grounds, plus the mode `index.html` resolves them against. */
    public static writeFor(family: string, mode: ThemeMode) {
        ThemeBootCache.write({
            mode,
            dark: ThemeBootCache.entry(family, "dark"),
            light: ThemeBootCache.entry(family, "light"),
        });
    }

    private static entry(family: string, ground: ThemeGround): ThemeBootEntry {
        const cssClasses = AppTheme.cssClass(family, ground);

        return {
            cssClasses,
            groundColor: ThemeTokens.read(cssClasses, ["bg0"]).get("bg0") ?? "",
        };
    }

    private static write(payload: ThemeBootPayload) {
        if ([payload.dark, payload.light].some(entry => !entry.cssClasses || !entry.groundColor)) {
            return;
        }

        try {
            localStorage.setItem(ThemeBootCache.storageKey, JSON.stringify(payload));
        } catch (ex) {
            console.error("Failed to write theme boot cache to localStorage", ex);
        }
    }
}
