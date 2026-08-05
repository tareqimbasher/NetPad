import {DI} from "aurelia";

/**
 * A window that can be deep-linked into. If the window contains internal destinations (sub views/pages, tabs...etc.)
 * then the destination can be switched to. It also declares what params the window uses that, if changed, require
 * the window to reload.
 */
export interface IWindowDestinations {
    /**
     * The query params of a window that cannot change without the window needing to reload.
     *
     * For example: if a window requires a "script-id" param and opens for a specific script, and
     * then the window is called again to open with a different script-id, then the window will
     * reload to load the script corresponding to the new script id, assuming it cannot load the
     * new script in-place. In this case the script-id is an identity param.
     *
     * If however, a window has a "tab" param for example that's used to decide which tab to open/focus,
     * and the window is called again to open with a different tab, nothing critical to the content of
     * the window has changed, and it can just {@link goTo} the tab without having to reload.
     */
    readonly identityParams: string[];

    /** Shows the destination at this route. An unknown route changes nothing. */
    goTo(route: string): void;
}

export const IWindowDestinations = DI.createInterface<IWindowDestinations>();
