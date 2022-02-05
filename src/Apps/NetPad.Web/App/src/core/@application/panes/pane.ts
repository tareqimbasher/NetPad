import {PaneHost} from "@application";

export abstract class Pane {
    protected _name: string;
    protected _host?: PaneHost;

    protected constructor(name: string, public readonly icon?: string) {
        this._name = name;
    }

    public get name(): string {
        return this._name;
    }

    public get host(): PaneHost | null | undefined {
        return this._host;
    }

    public setHost(paneHost: PaneHost) {
        this._host = paneHost;
    }

    public activateOrCollapse() {
        this.host.activateOrCollapse(this);
    }
}
