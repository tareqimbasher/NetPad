import {IWindowDestinations} from "@application/windowing/iwindow-destinations";

export class ConnectionWindowDestinations implements IWindowDestinations {
    public readonly identityParams = ["data-connection-id", "copy", "is-server"];

    public goTo() {
        // No routed destinations.
    }
}
