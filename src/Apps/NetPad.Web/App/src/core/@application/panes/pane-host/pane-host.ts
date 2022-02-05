import {IPaneHostViewStateController, Pane, PaneHostOrientation, PaneHostViewState} from "@application";
import {Util} from "@common";

export class PaneHost {
    public readonly id: string;
    protected readonly orientation: PaneHostOrientation = PaneHostOrientation.Right;
    protected viewState: PaneHostViewState = PaneHostViewState.Collapsed;
    protected _active?: Pane;

    private readonly panes: Set<Pane>;

    constructor(
        orientation: PaneHostOrientation,
        readonly viewStateController: IPaneHostViewStateController) {
        this.id = Util.newGuid();
        this.orientation = orientation;
        this.panes = new Set<Pane>();
    }

    public get active(): Pane | null | undefined {
        return this._active;
    }

    public activateOrCollapse(pane: Pane) {
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
        this._active = null;
    }

    public hasPane(pane: Pane): boolean {
        return this.panes.has(pane);
    }

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
