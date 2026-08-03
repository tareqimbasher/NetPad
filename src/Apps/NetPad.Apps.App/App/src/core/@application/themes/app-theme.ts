import {ThemeBackground, ThemeMode} from "@application";

/**
 * The light or the dark side of a theme family.
 */
export type ThemeGround = "dark" | "light";

/** A fully specified theme: a family, one of its grounds, and the background. */
export interface ResolvedTheme {
    family: string;
    ground: ThemeGround;
    background: ThemeBackground;
}

/** A palette the app can paint with, in both of its grounds. */
export interface ThemeFamily {
    /** Stored in settings, shown in the UI, and the middle part of the `theme-<id>-<ground>` CSS classes. */
    id: string;
    /** What this family calls each of its grounds, as the UI names them. */
    groundNames: Record<ThemeGround, string>;
}

/**
 * Turns the two theme settings (which family, and which mode) into the CSS class that carries
 * that theme's tokens, and tracks the desktop's light/dark preference so `System` mode can follow
 * it.
 *
 * Three terms are used throughout: a **family** is a palette (`inkwell`), a **ground** is its light
 * or dark side, and a **mode** is what the user picked (`Dark`, `Light`, or `System`, which
 * resolves to a ground at runtime). On top of those sits the background, which decides whether the
 * grounds come from the family or from the neutral set every family shares.
 */
export class AppTheme {
    /** Shared by every theme class. */
    public static readonly cssClassPrefix = "theme-";

    /**
     * Sits alongside a theme class on the same element and swaps that family's grounds for the
     * neutral set. Mirrors `$neutral-background-class` in `styles/themes/_registry.scss`.
     */
    public static readonly neutralBackgroundClass = `${AppTheme.cssClassPrefix}neutral-bg`;

    /** The family to fall back on when settings name one that is not recognized. */
    public static readonly defaultFamily = "inkwell";

    /**
     * The families that ship with a stylesheet. Keep in step with `styles/themes/_registry.scss`.
     */
    public static readonly families: readonly ThemeFamily[] = [
        {id: AppTheme.defaultFamily, groundNames: {dark: "ink", light: "vellum"}},
        {id: "cobalt", groundNames: {dark: "cobalt", light: "frost"}},
        {id: "gunmetal", groundNames: {dark: "gunmetal", light: "porcelain"}},
        {id: "rosewood", groundNames: {dark: "rosewood", light: "magnolia"}},
        {id: "graphite", groundNames: {dark: "graphite", light: "chalk"}},
    ];

    /** Both grounds, for callers that need to do something once per side of a family. */
    public static readonly grounds: readonly ThemeGround[] = ["dark", "light"];

    /**
     * Builds the CSS class that carries a theme's tokens, such as `theme-inkwell-dark`. Putting that
     * class on an element themes it and everything inside it.
     */
    public static cssClass(family: string, ground: ThemeGround): string {
        return `${AppTheme.cssClassPrefix}${AppTheme.resolveFamily(family).id}-${ground}`;
    }

    /**
     * Every class an element needs to be themed: the family's theme class, and the neutral
     * background modifier when the background setting asks for it.
     */
    public static cssClasses(theme: ResolvedTheme): string {
        const themeClass = AppTheme.cssClass(theme.family, theme.ground);

        return theme.background === "Neutral" ? `${themeClass} ${AppTheme.neutralBackgroundClass}` : themeClass;
    }

    /**
     * Applies the theme to the HTML document.
     */
    public static applyToDocument(family: string, mode: ThemeMode, background: ThemeBackground) {
        const root = document.documentElement;
        const classes = AppTheme.cssClasses({family, ground: AppTheme.resolveGround(mode), background});

        root.classList.remove(...[...root.classList].filter(c => c.startsWith(AppTheme.cssClassPrefix)));
        root.classList.add(...classes.split(" "));

        // The pre-boot paint from index.html has served its purpose. Hand the background back to
        // the stylesheet so it keeps up with theme changes.
        root.style.removeProperty("background-color");
    }

    /**
     * Looks a family up by id. If not found returns the default family.
     */
    public static resolveFamily(family: string | undefined): ThemeFamily {
        return AppTheme.families.find(f => f.id === family)
            ?? AppTheme.families.find(f => f.id === AppTheme.defaultFamily)!;
    }

    /**
     * Determines which ground a mode uses.
     */
    public static resolveGround(mode: ThemeMode): ThemeGround {
        return mode === "Light" ? "light" : mode === "Dark" ? "dark" : AppTheme.systemGround();
    }

    /** The ground the machine is currently asking for. */
    public static systemGround(): ThemeGround {
        return AppTheme.systemPrefersDark().matches ? "dark" : "light";
    }

    /**
     * Calls `handler` whenever the machine switches between light and dark. Returns a function that unsubscribes.
     */
    public static onSystemGroundChanged(handler: () => void): () => void {
        const query = AppTheme.systemPrefersDark();
        query.addEventListener("change", handler);
        return () => query.removeEventListener("change", handler);
    }

    private static systemPrefersDark(): MediaQueryList {
        return window.matchMedia("(prefers-color-scheme: dark)");
    }
}
