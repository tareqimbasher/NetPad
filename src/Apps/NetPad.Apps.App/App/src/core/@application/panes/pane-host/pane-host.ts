import {IPaneHostViewStateController, Pane, PaneHostOrientation, PaneHostViewState} from "@application";
import {Util} from "@common";

export class PaneHost {
    public readonly id: string;
    public readonly orientation: PaneHostOrientation = PaneHostOrientation.Right;
    protected _viewState: PaneHostViewState = PaneHostViewState.Collapsed;
    protected _active: Pane | undefined;

    private readonly panes: Set<Pane>;

    constructor(
        orientation: PaneHostOrientation,
        private readonly viewStateController: IPaneHostViewStateController
    ) {
        this.id = Util.newGuid();
        this.orientation = orientation;
        this.panes = new Set<Pane>();
    }

    public get active(): Pane | null | undefined {
        return this._active;
    }

    public get viewState(): PaneHostViewState {
        return this._viewState;
    }

    protected set viewState(value) {
        this._viewState = value;
    }

    public activateOrCollapse(pane?: Pane) {
        if (!pane) {
            if (this.panes.size > 0) {
                pane = [...this.panes][0];
            }
            else {
                throw new Error("No panes are added to this host.");
            }
        }

        if (pane === this.active && this.viewState === PaneHostViewState.Expanded) {
            this.collapse();
            return;
        }

        this._active = pane;

        if (this.viewState === PaneHostViewState.Collapsed)
            this.expand();
    }

    public expand() {
        if (this.viewState === PaneHostViewState.Expanded) return;
        this.viewStateController.expand(this);
        this.viewState = PaneHostViewState.Expanded;
    }

    public collapse() {
        if (this.viewState === PaneHostViewState.Collapsed) return;
        this.viewStateController.collapse(this);
        this.viewState = PaneHostViewState.Collapsed;
        this._active = undefined;
    }

    public hasPane(pane: Pane): boolean {
        return this.panes.has(pane);
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    public getPane<TPane extends Pane>(paneType: any): TPane | null {
        for (const pane of this.panes) {
            if (pane instanceof paneType)
                return pane as TPane;
        }
        return null;
    }

    public addPane(pane: Pane) {
        this.panes.add(pane);
        pane.setHost(this);
    }

    public removePane(pane: Pane) {
        this.panes.delete(pane);
    }
}
