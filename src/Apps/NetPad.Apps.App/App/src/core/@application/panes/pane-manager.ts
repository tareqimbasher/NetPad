import {DI, IContainer} from "aurelia";
import {IPaneHostViewStateController, Pane, PaneHost, PaneHostOrientation, TogglePaneEvent} from "@application";
import {IEventBus} from "@domain";

export interface IPaneManager {
    get paneHosts(): ReadonlyArray<PaneHost>;

    createPaneHost(orientation: PaneHostOrientation, viewStateController: IPaneHostViewStateController): PaneHost;

    addPaneToHost<TPane extends Pane>(paneType: unknown, paneHost: PaneHost): TPane;

    activateOrCollapse(pane: Pane): void;

    activateOrCollapse(paneType: unknown): void;
}

export const IPaneManager = DI.createInterface<IPaneManager>();

export class PaneManager implements IPaneManager {
    private _paneHosts: PaneHost[] = [];

    constructor(@IContainer private readonly container: IContainer, @IEventBus private readonly eventBus: IEventBus) {
        eventBus.subscribe(TogglePaneEvent, message => this.activateOrCollapse(message.paneType));
    }

    public get paneHosts(): ReadonlyArray<PaneHost> {
        return this._paneHosts;
    }

    public createPaneHost(orientation: PaneHostOrientation, viewStateController: IPaneHostViewStateController): PaneHost {
        const host = new PaneHost(orientation, viewStateController);
        this._paneHosts.push(host);
        return host;
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    public addPaneToHost<TPane extends Pane>(paneType: any, targetPaneHost: PaneHost): TPane {
        if (!(paneType.prototype instanceof Pane))
            throw new Error("paneType is not a type of Pane");

        let pane: Pane;

        const existing = this.findPaneAndHostByPaneType(paneType);
        if (existing) {
            pane = existing.pane;
            existing.paneHost.removePane(pane);
        } else
            pane = this.container.get(paneType);

        targetPaneHost.addPane(pane);

        return pane as TPane;
    }

    public activateOrCollapse(paneOrPaneType: Pane | unknown): void {
        let paneHost: PaneHost | undefined;
        let pane: Pane | undefined;

        if (paneOrPaneType instanceof Pane) {
            pane = paneOrPaneType;
            paneHost = pane.host || this.findPaneHostByPane(pane);
        } else {
            const paneAndHost = this.findPaneAndHostByPaneType(paneOrPaneType);
            paneHost = paneAndHost?.paneHost;
            pane = paneAndHost.pane;
        }

        paneHost?.activateOrCollapse(pane);
    }

    private findPaneHostByPane(pane: Pane): PaneHost | null {
        return this._paneHosts.find(h => h.hasPane(pane));
    }

    private findPaneAndHostByPaneType(paneType: unknown): { paneHost: PaneHost, pane: Pane } | null {
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
