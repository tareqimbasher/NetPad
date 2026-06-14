import {ILogger} from "aurelia";
import {IDisposable} from "@common";
import {IEventBus, IScriptService, ScriptDirectoryChangedEvent, ScriptSummary} from "@application";

/**
 * Server-synced list of scripts in the user's library.
 */
export class ScriptsStore {
    public scripts: ScriptSummary[] = [];

    private readonly logger: ILogger;
    private initializePromise?: Promise<void>;
    private readonly onChangedCallbacks = new Set<() => void>();

    constructor(
        @IScriptService private readonly scriptService: IScriptService,
        @IEventBus eventBus: IEventBus,
        @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(ScriptsStore));

        eventBus.subscribeToServer(ScriptDirectoryChangedEvent, msg => {
            this.setScripts(msg.scripts ?? []);
        });
    }

    /** Performs a one-time REST fetch of the scripts list. Idempotent: always returns the initial promise. */
    public initialize(): Promise<void> {
        return this.initializePromise ??= this.fetch();
    }

    /**
     * Registers a callback invoked whenever {@link scripts} changes (server push or initial fetch).
     * Dispose the returned handle to unsubscribe.
     */
    public onChanged(callback: () => void): IDisposable {
        this.onChangedCallbacks.add(callback);
        return {dispose: () => this.onChangedCallbacks.delete(callback)};
    }

    private async fetch(): Promise<void> {
        try {
            this.setScripts(await this.scriptService.getScripts());
        } catch (err) {
            this.logger.error("Error loading scripts", err);
        }
    }

    private setScripts(scripts: ScriptSummary[]) {
        this.scripts = scripts;
        this.fireChanged();
    }

    private fireChanged() {
        for (const callback of this.onChangedCallbacks) {
            try {
                callback();
            } catch (err) {
                this.logger.error("A ScriptsStore onChanged callback threw:", err);
            }
        }
    }
}
