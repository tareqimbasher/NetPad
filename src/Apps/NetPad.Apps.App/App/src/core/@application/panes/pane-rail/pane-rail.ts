import {bindable} from "aurelia";
import {Pane, PaneHost} from "@application";

/**
 * The strip of pane toggles down one side of a window.
 */
export class PaneRail {
    @bindable public side: "left" | "right" = "left";
    @bindable public host?: PaneHost;
    @bindable public endHost?: PaneHost;

    public toggle(host: PaneHost, pane: Pane) {
        host.toggle(pane);

        if (pane.isOpen) {
            host.element?.focus();
        }
    }

    public tooltip(pane: Pane): string {
        const keyCombo = pane.shortcut?.keyCombo.asString;
        return keyCombo ? `${pane.name} (${keyCombo})` : pane.name;
    }
}
