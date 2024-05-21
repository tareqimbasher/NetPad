import {PaneHost} from "@application";

/**
 * Controls the state of a pane host's view.
 */
export interface IPaneHostViewStateController {
    expand(paneHost: PaneHost): void;

    collapse(paneHost: PaneHost): void;
}
