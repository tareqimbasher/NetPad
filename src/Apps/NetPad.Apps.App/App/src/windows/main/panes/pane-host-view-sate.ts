import {PaneHost, PaneHostViewMode} from "@application";

export interface IPaneHostsState {
    bottom?: IPaneHostState;
    left?: IPaneHostState;
    right?: IPaneHostState;
}

export interface IPaneHostState {
    expanded: boolean;
    lastExpandedSize: number | undefined;
}

export class PaneHostViewState {
    public readonly selector: string;
    public shouldBeExpanded = false;
    public size = 0;
    public lastExpandedSize: number | undefined;

    constructor(public readonly paneHost: PaneHost, private readonly defaultPaneHostSizePercent: number) {
        this.selector = `pane-host[data-id='${paneHost.id}']`;
    }

    public get isCurrentlyExpanded(): boolean {
        return this.paneHost.viewMode === PaneHostViewMode.Expanded
    }

    public updateCurrentSize(currentSize: number) {
        this.size = this.isCurrentlyExpanded ? currentSize : 0;
        this.recordLastExpandedSize();
    }

    public calculateNewSize() {
        if (this.shouldBeExpanded && this.isCurrentlyExpanded) return;

        if (this.shouldBeExpanded) {
            if (this.lastExpandedSize) this.size = this.lastExpandedSize;
            else this.size = this.defaultPaneHostSizePercent;
        } else {
            this.size = 0;
        }

        this.recordLastExpandedSize();
    }

    private recordLastExpandedSize() {
        if (this.size) {
            this.lastExpandedSize = this.size;
        }
    }
}
