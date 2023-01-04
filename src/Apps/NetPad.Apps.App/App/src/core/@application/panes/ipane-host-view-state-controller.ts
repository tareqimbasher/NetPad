import {PaneHost} from "@application";

export interface IPaneHostViewStateController {
    expand(paneHost: PaneHost);

    collapse(paneHost: PaneHost);
}
