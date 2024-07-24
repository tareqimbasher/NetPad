import {Constructable} from "aurelia";
import {Pane} from "@application/panes/pane";

/**
 * Instructs app to toggle a specific pane.
 */
export class TogglePaneCommand {
    constructor(public paneType: Constructable<Pane>) {
    }
}
