import {Constructable, IContainer} from "aurelia";
import {
    IEventBus,
    IPaneHostViewStateController,
    IWindowService,
    Pane,
    PaneHost,
    PaneHostOrientation
} from "@application";
import {IPaneManager} from "./ipane-manager";
import {TogglePaneCommand} from "@application/panes/toggle-pane-command";

export class PaneManager implements IPaneManager {
    private _paneHosts: PaneHost[] = [];

    constructor(@IContainer private readonly container: IContainer, @IEventBus private readonly eventBus: IEventBus) {
        eventBus.subscribe(TogglePaneCommand, message => this.toggle(message.paneType));
    }

    public get paneHosts(): ReadonlyArray<PaneHost> {
        return this._paneHosts;
    }

    public createPaneHost(orientation: PaneHostOrientation, viewStateController?: IPaneHostViewStateController): PaneHost {
        // Use an empty controller if not provided
        viewStateController ??= {
            expand: (_) => {
            },
            collapse: (_) => {
            },
        };

        const host = new PaneHost(orientation, viewStateController, this.container.get(IWindowService));
        this._paneHosts.push(host);
        return host;
    }

    public addPaneToHost<TPane extends Pane>(paneType: Constructable<TPane>, targetPaneHost: PaneHost): TPane {
        if (!(paneType.prototype instanceof Pane))
            throw new Error("paneType is not a type of Pane");

        let pane: TPane;

        const existing = this.findPaneAndHostByPaneType(paneType);
        if (existing) {
            pane = existing.pane as TPane;
            existing.paneHost.removePane(pane);
        } else {
            pane = this.container.get(paneType) as TPane;
        }

        targetPaneHost.addPane(pane);

        return pane as TPane;
    }

    public toggle<TPane extends Pane>(paneOrPaneType: Pane | Constructable<TPane>): void {
        let paneHost: PaneHost | undefined;
        let pane: Pane | undefined;

        if (paneOrPaneType instanceof Pane) {
            pane = paneOrPaneType;
            paneHost = pane.host || this.findPaneHostByPane(pane);
        } else {
            const paneAndHost = this.findPaneAndHostByPaneType(paneOrPaneType);
            paneHost = paneAndHost?.paneHost;
            pane = paneAndHost?.pane;
        }

        if (!paneHost) return;

        paneHost.toggle(pane);
    }

    private findPaneHostByPane(pane: Pane): PaneHost | undefined {
        return this._paneHosts.find(h => h.hasPane(pane));
    }

    private findPaneAndHostByPaneType<TPane extends Pane>(paneType: Constructable<TPane>): IPaneInfo | null {
        for (const paneHost of this._paneHosts) {
            const pane = paneHost.getPane(paneType);
            if (pane) {
                return {
                    paneHost: paneHost,
                    pane: pane
                };
            }
        }

        return null;
    }
}

interface IPaneInfo {
    paneHost: PaneHost;
    pane: Pane;
}
