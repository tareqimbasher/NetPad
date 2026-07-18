import {Constructable, resolve} from "aurelia";
import {IPaneManager, Pane, PaneHost, PaneHostOrientation} from "@application";
import {WindowBase} from "./window-base";
import template from "./floating-pane-window-base.html";

export const windowTemplate = template;

/**
 * A base for windows that render a single pane as a window.
 */
export class FloatingPaneWindowBase<TPane extends Pane> extends WindowBase {
    private readonly paneManager: IPaneManager;
    public paneHost: PaneHost;
    public pane: TPane;

    constructor(private readonly paneType: Constructable<TPane>, protected readonly windowName: string) {
        super();
        this.paneManager = resolve(IPaneManager);
        document.title = windowName;
    }

    public attached() {
        this.paneHost = this.paneManager.createPaneHost(PaneHostOrientation.FloatingWindow);

        this.pane = this.paneManager.addPaneToHost(this.paneType, this.paneHost);

        setTimeout(() => this.pane.activate(), 1);
    }
}
