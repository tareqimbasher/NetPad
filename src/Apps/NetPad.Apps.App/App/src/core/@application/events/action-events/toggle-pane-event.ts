import {Constructable} from "aurelia";
import {Pane} from "@application/panes/pane";

/**
 * An action event instructing system to toggle a specific pane.
 */
export class TogglePaneEvent {
    constructor(public paneType: Constructable<Pane>) {
    }
}
