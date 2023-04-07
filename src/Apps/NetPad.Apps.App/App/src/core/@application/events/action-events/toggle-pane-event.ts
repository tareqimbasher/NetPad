import {Constructable} from "aurelia";
import {Pane} from "@application/panes/pane";

export class TogglePaneEvent {
    constructor(public paneType: Constructable<Pane>) {
    }
}
