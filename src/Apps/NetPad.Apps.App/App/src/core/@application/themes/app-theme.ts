import {ThemeMode} from "@application";

/**
 * The light or the dark side of a theme family.
 */
export type ThemeGround = "dark" | "light";

/** A palette the app can paint with, in both of its grounds. */
export interface ThemeFamily {
    /** Stored in settings, and the middle part of the `theme-<id>-<ground>` CSS classes. */
    id: string;
    /** What this family calls each of its grounds, as the UI names them. */
    groundNames: Record<ThemeGround, string>;
}

/**
 * Turns the two theme settings (which family, and which mode) into the CSS class that carries
 * that theme's tokens, and tracks the desktop's light/dark preference so `System` mode can follow
 * it.
 *
 * Three terms are used throughout: a **family** is a palette (`netpad`), a **ground** is its light
 * or dark side, and a **mode** is what the user picked (`Dark`, `Light`, or `System`, which
 * resolves to a ground at runtime).
 */
export class AppTheme {
    /** Shared by every theme class. */
    public static readonly cssClassPrefix = "theme-";

    /** The family to fall back on when settings name one that is not recognized. */
    public static readonly defaultFamily = "netpad";

    /** The families that ship with a stylesheet. Keep in step with `styles/themes/_index.scss`. */
    public static readonly families: readonly ThemeFamily[] = [
        {id: AppTheme.defaultFamily, groundNames: {dark: "ink", light: "vellum"}},
    ];

    /** Both grounds, for callers that need to do something once per side of a family. */
    public static readonly grounds: readonly ThemeGround[] = ["dark", "light"];

    /**
     * Builds the CSS class that carries a theme's tokens, such as `theme-netpad-dark`. Putting that
     * class on an element themes it and everything inside it.
     *
     * An unknown family resolves to the default family.
     */
    public static cssClass(family: string, ground: ThemeGround): string {
        return `${AppTheme.cssClassPrefix}${AppTheme.resolveFamily(family).id}-${ground}`;
    }

    /**
     * Applies the theme to the document.
     */
    public static applyToDocument(family: string, mode: ThemeMode) {
        const root = document.documentElement;
        const classes = AppTheme.cssClass(family, AppTheme.resolveGround(mode))
            .split(" ")
            .filter(c => c.length > 0);

        root.classList.remove(...[...root.classList].filter(c => c.startsWith(AppTheme.cssClassPrefix)));
        root.classList.add(...classes);

        // The pre-boot paint from index.html has served its purpose. Hand the background back to
        // the stylesheet so it keeps up with theme changes.
        root.style.removeProperty("background-color");
    }

    /**
     * Looks a family up by id. If not found returns the default family.
     */
    public static resolveFamily(family: string | undefined): ThemeFamily {
        return AppTheme.families.find(f => f.id === family) ?? AppTheme.families[0];
    }

    /**
     * Determines which ground a mode paints.
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
