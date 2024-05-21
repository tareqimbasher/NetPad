import Split from "split.js";
import {Lazy} from "@common";
import {IPaneHostViewStateController, PaneHost} from "@application";
import {IPaneHostsState, IPaneHostState, PaneHostViewState} from "./pane-host-view-sate";

/**
 * A pane host view state controller that splits the view horizontally or vertically with other content.
 */
export class SplitViewController implements IPaneHostViewStateController {
    private readonly stateKey = "main-window-pane-hosts-state";
    private split?: Split.Instance;
    private _viewStates: Lazy<PaneHostViewState[]>;
    private _elementsAndPaneHosts: Lazy<(HTMLElement | PaneHost)[]>;

    constructor(
        orderedElementsAndPaneHostsGetter: () => (HTMLElement | PaneHost)[],
        private readonly direction: "horizontal" | "vertical",
        private defaultPaneHostSizePercent: number,
    ) {
        setTimeout(() => this.loadState(), 1);

        this._elementsAndPaneHosts = new Lazy(orderedElementsAndPaneHostsGetter);
        this._viewStates = new Lazy(() => this._elementsAndPaneHosts.value
            .filter(x => x instanceof PaneHost)
            .map(ph => new PaneHostViewState(ph as PaneHost, this.defaultPaneHostSizePercent)));
    }

    public expand(target: PaneHost) {
        for (const viewState of this._viewStates.value) {
            viewState.shouldBeExpanded = viewState.isCurrentlyExpanded || viewState.paneHost === target;
        }

        this.doSplit();
    }

    public collapse(target: PaneHost) {
        for (const viewState of this._viewStates.value) {
            viewState.shouldBeExpanded = viewState.isCurrentlyExpanded && viewState.paneHost !== target;
        }

        this.doSplit();
    }

    private doSplit() {
        const currentSizes = this.split?.getSizes();

        if (currentSizes) {
            this.updateCurrentSizes(currentSizes);
        }

        for (const viewState of this._viewStates.value) {
            viewState.calculateNewSize();
        }

        const elements: HTMLElement[] = [];
        const sizes: number[] = [];

        // Determine size for each element that is not a PaneHost
        let nonPaneHostElementSize = 0;
        const nonPaneHostElementCount = this._elementsAndPaneHosts.value.filter(e => !(e instanceof PaneHost)).length;
        if (nonPaneHostElementCount > 0) {
            const paneElementsSizeTotal = this._viewStates.value.reduce((sum, x) => sum + x.size, 0);
            nonPaneHostElementSize = (100 - paneElementsSizeTotal) / nonPaneHostElementCount;
            if (nonPaneHostElementSize <= 0) nonPaneHostElementSize = 10;
        }

        for (const item of this._elementsAndPaneHosts.value) {
            if (item instanceof PaneHost) {
                if (!item.element)
                    throw new Error("PaneHost element is not set");

                const viewState = this._viewStates.value.find(vs => vs.paneHost === item);
                if (!viewState)
                    throw new Error("PaneHost does not have a view state");

                if (viewState.shouldBeExpanded) {
                    elements.push(item.element);
                    sizes.push(viewState.size);
                }
            } else {
                elements.push(item);
                sizes.push(nonPaneHostElementSize);
            }
        }

        this.split?.destroy();

        this.split = Split(elements, {
            direction: this.direction,
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
        for (let ix = 0; ix < currentSizes.length; ix++) {
            const element = this._elementsAndPaneHosts.value[ix];
            const isPaneHost = element instanceof PaneHost;

            if (!isPaneHost) continue;

            const currentSize = currentSizes[ix];
            this._viewStates.value.find(vs => vs.paneHost === element)?.updateCurrentSize(currentSize);
        }
    }

    public hasSavedState(): boolean {
        return !!localStorage.getItem(this.stateKey);
    }

    private getSavedState(): IPaneHostsState {
        const json = localStorage.getItem(this.stateKey);
        return !json ? {} : JSON.parse(json);
    }

    private loadState() {
        const state = this.getSavedState();

        for (const viewState of this._viewStates.value) {
            const savedState = state[viewState.paneHost.orientation.toLowerCase() as keyof typeof state] as IPaneHostState;
            if (savedState) {
                viewState.lastExpandedSize = savedState.lastExpandedSize;
                if (savedState.expanded) {
                    viewState.paneHost.expand();
                }
            }
        }
    }

    private saveState() {
        const state = this.getSavedState();

        for (const viewState of this._viewStates.value) {
            state[viewState.paneHost.orientation.toLowerCase() as keyof typeof state] = {
                expanded: viewState.shouldBeExpanded,
                lastExpandedSize: viewState.lastExpandedSize
            }
        }

        localStorage.setItem(this.stateKey, JSON.stringify(state));
    }
}
