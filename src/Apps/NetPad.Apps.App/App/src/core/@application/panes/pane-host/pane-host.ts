import {IPaneHostViewStateController, Pane, PaneHostOrientation, PaneHostViewMode} from "@application";
import {Util} from "@common";
import {Constructable, IHydratedParentController} from "aurelia";

export class PaneHost {
    public readonly id: string;
    public readonly orientation: PaneHostOrientation = PaneHostOrientation.Right;
    public element: HTMLElement | null | undefined;

    protected _viewMode: PaneHostViewMode = PaneHostViewMode.Collapsed;
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

    public get viewMode(): PaneHostViewMode {
        return this._viewMode;
    }

    protected set viewMode(value) {
        this._viewMode = value;
    }

    private attached(initiator: IHydratedParentController) {
        this.element = initiator.host;
    }

    public toggle(pane?: Pane) {
        if (pane === this.active && this.viewMode === PaneHostViewMode.Expanded)
            this.collapse();
        else
            this.expand(pane);
    }

    public expand(pane?: Pane) {
        if (!pane) {
            if (this.panes.size > 0) {
                pane = [...this.panes][0];
            } else {
                throw new Error("No panes are added to this host.");
            }
        }

        this._active = pane;

        if (this.viewMode !== PaneHostViewMode.Expanded) {
            this.viewStateController.expand(this);
            this.viewMode = PaneHostViewMode.Expanded;
        }
    }

    public collapse() {
        if (this.viewMode === PaneHostViewMode.Collapsed) return;
        this.viewStateController.collapse(this);
        this.viewMode = PaneHostViewMode.Collapsed;
        this._active = undefined;
    }

    public hasPane(pane: Pane): boolean {
        return this.panes.has(pane);
    }

    public getPane<TPane extends Pane>(paneType: Constructable<TPane>): TPane | null {
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
