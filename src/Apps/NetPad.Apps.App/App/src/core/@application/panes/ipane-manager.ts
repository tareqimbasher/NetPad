import {IPaneHostViewStateController, Pane, PaneHost, PaneHostOrientation} from "@application";
import {Constructable, DI} from "aurelia";

export interface IPaneManager {
    createPaneHost(orientation: PaneHostOrientation, viewStateController?: IPaneHostViewStateController): PaneHost;

    addPaneToHost<TPane extends Pane>(paneType: Constructable<TPane>, paneHost: PaneHost): TPane;

    toggle(pane: Pane): void;

    toggle(paneType: unknown): void;
}

export const IPaneManager = DI.createInterface<IPaneManager>();
