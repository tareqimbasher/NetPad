import {DI} from "aurelia";

/**
 * A window that can be deep-linked into: it can switch to an internal destination, such as a tab. It also declares
 * what params the window uses that, if changed, require the window to reload.
 */
export interface IWindowDestinations {
    /**
     * The query params that cannot change without reloading the window, such as the `script-id` a window
     * was opened for. Other params, such as `tab`, change in place through {@link goTo}.
     */
    readonly identityParams: string[];

    /** Shows the destination at this route. An unknown route changes nothing. */
    goTo(route: string): void;
}

export const IWindowDestinations = DI.createInterface<IWindowDestinations>();
