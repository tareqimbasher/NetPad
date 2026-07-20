import {Constructable, IHydratedParentController} from "aurelia";
import {
    IPaneHostViewStateController,
    IWindowService,
    KeyCombo,
    Pane,
    PaneHostOrientation,
    PaneHostViewMode
} from "@application";
import {DisposableCollection, KeyCode, Util} from "@common";

/**
 * Hosts a set of panes. A PaneHost can host any number of panes. A pane cannot exist in multiple PaneHosts at once.
 */
export class PaneHost {
    public readonly id: string;
    public readonly orientation: PaneHostOrientation = PaneHostOrientation.Right;
    public element: HTMLElement | null | undefined;

    protected _viewMode: PaneHostViewMode = PaneHostViewMode.Collapsed;
    protected _active: Pane | undefined;

    private readonly _panes: Pane[] = [];
    private hideKeyBinding = new KeyCombo().withShiftKey().withKey(KeyCode.Escape);
    private disposables = new DisposableCollection();

    constructor(
        orientation: PaneHostOrientation,
        private readonly viewStateController: IPaneHostViewStateController,
        private readonly windowService: IWindowService
    ) {
        this.id = Util.newGuid();
        this.orientation = orientation;
    }

    /**
     * The panes in this host, in the order they were added. Rails render their toggles in this
     * order.
     */
    public get panes(): ReadonlyArray<Pane> {
        return this._panes;
    }

    /**
     * The active pane within this pane host.
     */
    public get active(): Pane | null | undefined {
        return this._active;
    }

    public get viewMode(): PaneHostViewMode {
        return this._viewMode;
    }

    private attached(initiator: IHydratedParentController) {
        this.element = initiator.host;

        if (this.element) {
            const tabKeysHandler = (ev: Event) => {
                if (this.hideKeyBinding.matches(ev as KeyboardEvent)) {
                    this.collapse();
                    ev.preventDefault();
                }
            };

            this.element.addEventListener("keydown", tabKeysHandler);
            this.disposables.add(() => this.element?.removeEventListener("keydown", tabKeysHandler));
        }
    }

    private detached() {
        this.disposables.dispose();
    }

    public toggle(pane?: Pane) {
        if (pane === this.active && this.viewMode === PaneHostViewMode.Expanded)
            this.collapse();
        else
            this.expand(pane);
    }

    public expand(pane?: Pane) {
        if (!pane) {
            if (this._panes.length === 0) {
                throw new Error("No panes are added to this host.");
            }

            pane = this._panes[0];
        }

        this._active = pane;

        if (this.viewMode !== PaneHostViewMode.Expanded) {
            this.viewStateController.expand(this);
            this._viewMode = PaneHostViewMode.Expanded;
        }
    }

    public collapse(pane?: Pane) {
        const shouldCollapse = this.viewMode !== PaneHostViewMode.Collapsed
            && (!pane || this._active === pane);

        if (!shouldCollapse) return;

        if (this.orientation === PaneHostOrientation.FloatingWindow) {
            this.windowService.close();
            return;
        }

        this.viewStateController.collapse(this);
        this._viewMode = PaneHostViewMode.Collapsed;
        this._active = undefined;
    }

    public hasPane(pane: Pane): boolean {
        return this._panes.includes(pane);
    }

    public getPane<TPane extends Pane>(paneType: Constructable<TPane>): TPane | null {
        for (const pane of this._panes) {
            if (pane instanceof paneType)
                return pane as TPane;
        }
        return null;
    }

    public getPaneById(paneId: string): Pane | null {
        for (const pane of this._panes) {
            if (pane.id === paneId)
                return pane;
        }
        return null;
    }

    public addPane(pane: Pane) {
        if (!this.hasPane(pane)) {
            this._panes.push(pane);
        }

        pane.setHost(this);
    }

    public removePane(pane: Pane) {
        const ix = this._panes.indexOf(pane);
        if (ix >= 0) {
            this._panes.splice(ix, 1);
        }

        if (this._active === pane) {
            this._active = undefined;
        }
    }
}
