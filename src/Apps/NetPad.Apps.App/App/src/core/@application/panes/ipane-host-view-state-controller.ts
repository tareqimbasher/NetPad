import {PaneHost} from "@application";

export interface IPaneHostViewStateController {
    expand(paneHost: PaneHost): void;

    collapse(paneHost: PaneHost): void;
}
