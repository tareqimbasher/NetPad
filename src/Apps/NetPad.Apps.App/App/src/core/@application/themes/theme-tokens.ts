/**
 * Reads the design tokens of a theme (the CSS custom properties the SCSS theme maps generate) so
 * that code which cannot use CSS (like Monaco, which wants hex strings in JS) still gets its colors
 * from the one place colors are defined.
 *
 * Values are read off a detached probe element carrying the theme's class rather than off the
 * document, so a theme can be read even when it is not the active one (the editor theme and the
 * app theme are separate settings). User custom CSS applies to the probe like any other rule, so
 * a user who redefines a token rethemes the editor with it.
 */
export class ThemeTokens {
    public static read(themeCssClass: string, names: readonly string[]): ReadonlyMap<string, string> {
        const probe = document.createElement("div");
        probe.className = themeCssClass;
        probe.style.display = "none";

        const host = document.body ?? document.documentElement;
        host.appendChild(probe);

        try {
            const computed = getComputedStyle(probe);
            return new Map(names.map(name => [name, computed.getPropertyValue(`--${name}`).trim()]));
        } finally {
            probe.remove();
        }
    }
}
