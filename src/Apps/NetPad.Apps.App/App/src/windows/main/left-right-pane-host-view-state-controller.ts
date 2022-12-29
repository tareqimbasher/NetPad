import {IPaneHostViewStateController, PaneHost, PaneHostViewState} from "@application";
import Split from "split.js";
import {PLATFORM} from "aurelia";

export class LeftRightPaneHostViewStateController implements IPaneHostViewStateController {
    private readonly stateKey = "main-window-pane-hosts-state";
    private _left?: PaneHostSplitInfo | undefined;
    private _right?: PaneHostSplitInfo | undefined;
    private split?: Split.Instance;

    constructor(
        private readonly mainContentSelector: string,
        private readonly leftPaneHostGetter?: () => PaneHost,
        private readonly rightPaneHostGetter?: () => PaneHost) {
        PLATFORM.setTimeout(() => this.loadState(), 1);
    }

    private get left(): PaneHostSplitInfo | undefined {
        if (!this._left && this.leftPaneHostGetter) {
            this._left = new PaneHostSplitInfo(this.leftPaneHostGetter());
        }
        return this._left;
    }

    private get right(): PaneHostSplitInfo | undefined {
        if (!this._right && this.rightPaneHostGetter) {
            this._right = new PaneHostSplitInfo(this.rightPaneHostGetter());
        }
        return this._right;
    }

    public expand(target: PaneHost) {
        if (this.left) {
            this.left.shouldBeExpanded = this.left.isCurrentlyExpanded || this.left.paneHost === target;
        }

        if (this.right) {
            this.right.shouldBeExpanded = this.right.isCurrentlyExpanded || this.right.paneHost === target;
        }

        this.doSplit();
    }

    public collapse(target: PaneHost) {
        if (this.left) {
            this.left.shouldBeExpanded = this.left.isCurrentlyExpanded && this.left.paneHost !== target;
        }

        if (this.right) {
            this.right.shouldBeExpanded = this.right.isCurrentlyExpanded && this.right.paneHost !== target;
        }

        this.doSplit();
    }

    public hasSavedState(): boolean {
        return !!localStorage.getItem(this.stateKey);
    }

    private doSplit() {
        const currentSizes = this.split?.getSizes();

        if (currentSizes) {
            this.updateCurrentSizes(currentSizes);
        }

        this.left?.calculateNewSize();
        this.right?.calculateNewSize();

        const elements: string[] = [];
        const sizes: number[] = [];

        if (this.left?.shouldBeExpanded) {
            elements.push(this.left.selector);
            sizes.push(this.left.size);
        }

        elements.push(this.mainContentSelector);
        sizes.push(100 - ((this.left?.size || 0) + (this.right?.size || 0)));

        if (this.right?.shouldBeExpanded) {
            elements.push(this.right.selector);
            sizes.push(this.right.size);
        }

        this.split?.destroy();

        this.split = Split(elements, {
            direction: 'horizontal',
            gutterSize: 6,
            sizes: sizes,
            snapOffset: 0,
            onDragEnd: (sizes: number[]) => {
                this.updateCurrentSizes(sizes);
                this.saveState();
            }
        });

        this.saveState();
    }

    private updateCurrentSizes(currentSizes: number[]) {
        this.left?.updateCurrentSize(currentSizes, 0);
        this.right?.updateCurrentSize(currentSizes, this.left?.isCurrentlyExpanded ? 2 : 1);
    }

    private saveState() {
        const state: IPaneHostsState = {};
        if (this.left) {
            state.left = {
                expanded: this.left.shouldBeExpanded,
                lastExpandedSize: this.left.lastExpandedSize
            };
        }

        if (this.right) {
            state.right = {
                expanded: this.right.shouldBeExpanded,
                lastExpandedSize: this.right.lastExpandedSize
            };
        }

        localStorage.setItem(this.stateKey, JSON.stringify(state));
    }

    private loadState() {
        const json = localStorage.getItem(this.stateKey);
        const state: IPaneHostsState = !json ? {} : JSON.parse(json);

        if (state.left && this.left) {
            this.left.lastExpandedSize = state.left.lastExpandedSize;
            if (state.left.expanded) {
                this.left.paneHost.activateOrCollapse();
            }
        }

        if (state.right && this.right) {
            this.right.lastExpandedSize = state.right.lastExpandedSize;
            if (state.right.expanded) {
                this.right.paneHost.activateOrCollapse();
            }
        }
    }
}

class PaneHostSplitInfo {
    public readonly selector: string;
    public shouldBeExpanded = false;
    public size = 0;
    public lastExpandedSize: number | undefined;

    constructor(public readonly paneHost: PaneHost) {
        this.selector = `pane-host[data-id='${paneHost.id}']`;
    }

    public get isCurrentlyExpanded(): boolean {
        return this.paneHost.viewState === PaneHostViewState.Expanded
    }

    public updateCurrentSize(sizes: number[], index: number) {
        this.size = this.isCurrentlyExpanded ? sizes[index] : 0;
        this.recordLastExpandedSize();
    }

    public calculateNewSize() {
        if (this.shouldBeExpanded && this.isCurrentlyExpanded) return;

        if (this.shouldBeExpanded) {
            if (this.lastExpandedSize) this.size = this.lastExpandedSize;
            else this.size = 15;
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

interface IPaneHostsState {
    left?: IPaneHostState;
    right?: IPaneHostState;
}

interface IPaneHostState {
    expanded: boolean;
    lastExpandedSize: number | undefined;
}
