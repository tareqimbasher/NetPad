import {DI, IContainer} from "aurelia";
import {IPaneHostViewStateController, Pane, PaneHost, PaneHostOrientation} from "@application";

export interface IPaneManager {
    get paneHosts(): ReadonlyArray<PaneHost>;
    createPaneHost(orientation: PaneHostOrientation, viewStateController: IPaneHostViewStateController): PaneHost;
    movePaneToHost<TPane extends Pane>(paneType: any, paneHost: PaneHost): TPane;
    activateOrCollapse(pane: Pane): void;
    activateOrCollapse(paneType: any): void;
}

export const IPaneManager = DI.createInterface<IPaneManager>();

export class PaneManager implements IPaneManager {
    private _paneHosts: PaneHost[] = [];

    constructor(@IContainer readonly container: IContainer) {
    }

    public get paneHosts(): ReadonlyArray<PaneHost> {
        return this._paneHosts;
    }

    public createPaneHost(orientation: PaneHostOrientation, viewStateController: IPaneHostViewStateController): PaneHost {
        const host = new PaneHost(orientation, viewStateController);
        this._paneHosts.push(host);
        return host;
    }

    public movePaneToHost<TPane extends Pane>(paneType: any, targetPaneHost: PaneHost): TPane {
        if (!(paneType.prototype instanceof Pane))
            throw new Error("paneType is not a type of Pane");

        let pane: Pane;

        const existing = this.findPaneAndHostByPaneType(paneType);
        if (existing) {
            pane = existing.pane;
            existing.paneHost.removePane(pane);
        }
        else
            pane = this.container.get(paneType);

        targetPaneHost.addPane(pane);

        return pane as TPane;
    }

    public activateOrCollapse(paneOrPaneType: Pane | any): void {
        let paneHost: PaneHost | undefined;
        let pane: Pane | undefined;

        if (paneOrPaneType instanceof Pane) {
            pane = paneOrPaneType;
            paneHost = pane.host || this.findPaneHostByPane(pane);
        }
        else {
            const paneAndHost = this.findPaneAndHostByPaneType(paneOrPaneType);
            paneHost = paneAndHost?.paneHost;
            pane = paneAndHost.pane;
        }

        paneHost?.activateOrCollapse(pane);
    }

    private findPaneHostByPane(pane: Pane): PaneHost | null {
        return this._paneHosts.find(h => h.hasPane(pane));
    }

    private findPaneAndHostByPaneType(paneType: any): {paneHost: PaneHost, pane: Pane} | null {
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
