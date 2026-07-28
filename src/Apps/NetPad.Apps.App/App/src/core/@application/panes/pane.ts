import {ILogger, resolve} from "aurelia";
import {IconName, PaneHost, PaneHostOrientation, PaneHostViewMode} from "@application";

export abstract class Pane {
    protected _name: string;
    protected _commandId?: string;
    protected _host?: PaneHost;
    protected logger: ILogger;

    /**
     * @param id A stable, unique identifier for this pane.
     */
    protected constructor(public readonly id: string, name: string, public readonly icon?: IconName, public readonly showNameInHeader: boolean = true) {
        this._name = name;
        this.logger = resolve(ILogger).scopeTo(this.constructor.name)
    }

    public get name(): string {
        return this._name;
    }

    public get host(): PaneHost | null | undefined {
        return this._host;
    }

    /** The command that toggles this pane, if one exists. */
    public get commandId(): string | undefined {
        return this._commandId;
    }

    public get isOpen(): boolean {
        return this.host?.viewMode === PaneHostViewMode.Expanded
            && this.host?.active === this;
    }

    public get orientation(): PaneHostOrientation | undefined {
        return this.host?.orientation;
    }

    public get isWindow(): boolean {
        return this.host?.orientation === PaneHostOrientation.FloatingWindow;
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

    /**
     * An optional count shown as a badge on this pane's rail toggle. 0 shows no badge.
     */
    public get badgeCount(): number {
        return 0;
    }

    /** The text shown in the rail badge. If {@link badgeCount} is larger than 99, shows "99+". */
    public get badgeText(): string {
        return this.badgeCount > 99 ? "99+" : this.badgeCount.toString();
    }

    public togglesWith(commandId: string) {
        this._commandId = commandId;
    }
}
