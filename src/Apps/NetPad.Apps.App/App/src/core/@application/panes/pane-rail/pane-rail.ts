import {bindable, resolve} from "aurelia";
import {IKeybindingManager, Pane, PaneHost} from "@application";

interface RailGroup {
    host: PaneHost;
    /** Whether the group sits at the far end of the rail. */
    end: boolean;
}

/**
 * The strip of pane toggles down one side of a window.
 */
export class PaneRail {
    @bindable public side: "left" | "right" = "left";
    @bindable public host?: PaneHost;
    @bindable public endHost?: PaneHost;
    public groups: RailGroup[] = [];

    private readonly keybindingManager = resolve(IKeybindingManager);

    public binding() {
        this.updateGroups();
    }

    public hostChanged() {
        this.updateGroups();
    }

    public endHostChanged() {
        this.updateGroups();
    }

    private updateGroups() {
        const groups = [{host: this.host, end: false}, {host: this.endHost, end: true}];
        this.groups = groups.filter((group): group is RailGroup => !!group.host);
    }

    public tooltip(pane: Pane): string {
        return pane.commandId ? this.keybindingManager.describe(pane.commandId, pane.name) : pane.name;
    }
}
