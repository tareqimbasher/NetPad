/**
 * Reads the design tokens of a theme (the CSS custom properties the SCSS theme maps generate) so
 * that code which cannot use CSS (like Monaco, which wants hex strings in JS) still gets its colors
 * from the one place colors are defined.
 *
 * Values come from a hidden probe element carrying the theme's class, so a theme that is not the
 * active one can be read too. User custom CSS applies to the probe, so a token the user redefines rethemes the
 * editor with it.
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
