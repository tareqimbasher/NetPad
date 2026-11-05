import {IPaneHostViewStateController, Pane, PaneHost, PaneHostOrientation} from "@application";
import {Constructable, DI} from "aurelia";

export interface IPaneManager {
    createPaneHost(orientation: PaneHostOrientation, viewStateController?: IPaneHostViewStateController): PaneHost;
    addPaneToHost<TPane extends Pane>(paneType: Constructable<TPane>, paneHost: PaneHost): TPane;
    /** Toggles the pane with this id, if it is registered in this window. See `PaneIds`. */
    toggle(paneId: string): void;
    /** Expands the pane with this id, if it is registered in this window. See `PaneIds`. */
    expand(paneId: string): void;
}

export const IPaneManager = DI.createInterface<IPaneManager>();
