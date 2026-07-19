/**
 * Mirrors the active theme into localStorage so that `index.html` can paint the window in the
 * right color before the SPA (and with it the real settings) has loaded. Settings remain the
 * source of truth; this is only a cache and may be missing or stale.
 *
 * The reader is an inline script at the top of `index.html` — the key and the payload shape must
 * stay in sync with it.
 */
export class ThemeBootCache {
    public static readonly storageKey = "netpad.theme.boot";

    /**
     * @param cssClasses The theme classes to put on the document element.
     * @param groundColor The theme's ground color (the `--bg0` token), used for the pre-boot paint.
     */
    public static write(cssClasses: string, groundColor: string) {
        if (!cssClasses || !groundColor) return;

        try {
            localStorage.setItem(ThemeBootCache.storageKey, JSON.stringify({cssClasses, groundColor}));
        } catch {
            // localStorage can be unavailable (private mode, quota). Losing the cache only costs
            // us the pre-boot paint, so there is nothing to recover from here.
        }
    }
}
