import {bindable, resolve} from "aurelia";
import {IKeybindingManager, Pane, PaneHost} from "@application";

/**
 * The strip of pane toggles down one side of a window.
 */
export class PaneRail {
    @bindable public side: "left" | "right" = "left";
    @bindable public host?: PaneHost;
    @bindable public endHost?: PaneHost;

    private readonly keybindingManager = resolve(IKeybindingManager);

    public toggle(host: PaneHost, pane: Pane) {
        host.toggle(pane);

        if (pane.isOpen) {
            host.element?.focus();
        }
    }

    public tooltip(pane: Pane): string {
        const keyCombo = pane.commandId
            ? this.keybindingManager.getKeybinding(pane.commandId)?.keyCombo
            : undefined;

        return keyCombo?.isBound ? `${pane.name} (${keyCombo.asString()})` : pane.name;
    }
}
