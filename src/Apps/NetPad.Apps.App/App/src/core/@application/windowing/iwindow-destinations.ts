import {DI} from "aurelia";

/**
 * A window whose content is divided into routed destinations.
 */
export interface IWindowDestinations {
    /** Shows the destination at this route. An unknown route changes nothing. */
    goTo(route: string): void;
}

export const IWindowDestinations = DI.createInterface<IWindowDestinations>();
