import {PaneAction, PaneHost, Shortcut} from "@application";

export abstract class Pane {
    protected _name: string;
    protected _host?: PaneHost;
    protected _shortcut?: Shortcut;
    protected _actions: PaneAction[] = [];

    protected constructor(name: string, protected readonly icon?: string) {
        this._name = name;
    }

    public get name(): string {
        return this._name;
    }

    public get host(): PaneHost | null | undefined {
        return this._host;
    }

    public get shortcut(): Shortcut | null | undefined {
        return this._shortcut;
    }

    public get actions(): PaneAction[] {
        return this._actions;
    }

    public setHost(paneHost: PaneHost) {
        this._host = paneHost;
    }

    public activate() {
        this.host?.expand(this);
    }

    public hide() {
        this.host?.collapse(this);
    }

    public hasShortcut(shortcut?: Shortcut) {
        this._shortcut = shortcut;
    }
}
