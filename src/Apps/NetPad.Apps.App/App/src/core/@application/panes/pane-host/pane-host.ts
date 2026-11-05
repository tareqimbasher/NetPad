import {Constructable, IHydratedParentController} from "aurelia";
import {
    IPaneHostViewStateController,
    IWindowService,
    KeyCombo,
    Pane,
    PaneHostOrientation,
    PaneHostViewMode
} from "@application";
import {DisposableCollection, Util} from "@common";

/**
 * Hosts a set of panes. A PaneHost can host any number of panes. A pane cannot exist in multiple PaneHosts at once.
 */
export class PaneHost {
    public readonly id: string;
    public readonly orientation: PaneHostOrientation = PaneHostOrientation.Right;
    public element: HTMLElement | null | undefined;
    public hasFocus = false;

    protected _viewMode: PaneHostViewMode = PaneHostViewMode.Collapsed;
    protected _active: Pane | undefined;

    private readonly _panes: Pane[] = [];
    private focusReturnTarget: HTMLElement | null = null;
    private hideKeyBinding = new KeyCombo({shift: true, key: "Escape"});
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
     * The panes in this host, in the order they were added.
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

            const focusInHandler = (ev: FocusEvent) => {
                this.hasFocus = true;

                if (!this.element?.contains(ev.relatedTarget as Node | null)) {
                    this.focusReturnTarget = ev.relatedTarget instanceof HTMLElement ? ev.relatedTarget : null;
                }
            };
            const focusOutHandler = (ev: FocusEvent) => {
                if (!this.element?.contains(ev.relatedTarget as Node | null)) {
                    this.hasFocus = false;
                }
            };

            this.element.addEventListener("keydown", tabKeysHandler);
            this.element.addEventListener("focusin", focusInHandler);
            this.element.addEventListener("focusout", focusOutHandler);
            this.disposables.add(() => {
                this.element?.removeEventListener("keydown", tabKeysHandler);
                this.element?.removeEventListener("focusin", focusInHandler);
                this.element?.removeEventListener("focusout", focusOutHandler);
            });
        }
    }

    private detached() {
        this.disposables.dispose();
    }

    /** The user's open/close action: unlike {@link expand}, opening a pane also focuses the host. */
    public toggle(pane?: Pane) {
        if (pane === this.active && this.viewMode === PaneHostViewMode.Expanded) {
            this.collapse();
            return;
        }

        this.expand(pane);
        this.element?.focus();
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

    /** Collapses the host and hands any focus inside it back to where it came from. */
    public collapse(pane?: Pane) {
        const shouldCollapse = this.viewMode !== PaneHostViewMode.Collapsed
            && (!pane || this._active === pane);

        if (!shouldCollapse) return;

        if (this.orientation === PaneHostOrientation.FloatingWindow) {
            this.windowService.close();
            return;
        }

        const hadFocus = !!this.element?.contains(document.activeElement);

        this.viewStateController.collapse(this);
        this._viewMode = PaneHostViewMode.Collapsed;
        this._active = undefined;

        if (hadFocus) {
            this.returnFocus();
        }
    }

    // A collapsed host is still focusable at 0px, so focus left in it swallows keystrokes. The return
    // target can be such a host too.
    private returnFocus() {
        const target = this.focusReturnTarget;
        this.focusReturnTarget = null;

        const box = target?.isConnected ? target.getBoundingClientRect() : undefined;

        if (target && box && box.width > 0 && box.height > 0) {
            target.focus();
        } else if (document.activeElement instanceof HTMLElement) {
            document.activeElement.blur();
        }
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
