import {ILogger} from "aurelia";
import {IDisposable} from "@common";
import {IEventBus, ISession, RecentScriptsChangedEvent} from "@application";

/**
 * Server-synced list of recently opened script file paths.
 */
export class RecentScriptsStore {
    public recentScripts: string[] = [];

    private readonly logger: ILogger;
    private serverPushedRecents = false;
    private initializePromise?: Promise<void>;
    private readonly onChangedCallbacks = new Set<() => void>();

    constructor(
        @ISession private readonly session: ISession,
        @IEventBus eventBus: IEventBus,
        @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(RecentScriptsStore));

        eventBus.subscribeToServer(RecentScriptsChangedEvent, msg => {
            this.serverPushedRecents = true;
            this.setRecentScripts(msg.recentScripts ?? []);
        });
    }

    /** Performs a one-time REST fetch of the recents list. Idempotent: always returns the initial promise. */
    public initialize(): Promise<void> {
        return this.initializePromise ??= this.fetch();
    }

    /** Removes a single entry. The backend responds with a {@link RecentScriptsChangedEvent}. */
    public async remove(scriptPath: string): Promise<void> {
        await this.session.removeRecent(scriptPath);
    }

    /** Clears all entries. The backend responds with a {@link RecentScriptsChangedEvent}. */
    public async clear(): Promise<void> {
        await this.session.clearRecent();
    }

    /**
     * Registers a callback invoked whenever {@link recentScripts} changes (server push or initial
     * fetch). Dispose the returned handle to unsubscribe.
     */
    public onChanged(callback: () => void): IDisposable {
        this.onChangedCallbacks.add(callback);
        return {dispose: () => this.onChangedCallbacks.delete(callback)};
    }

    private async fetch(): Promise<void> {
        try {
            const paths = await this.session.getRecent();
            // A server push landed while this fetch was in flight; it's authoritative, so don't
            // overwrite it with our now-stale snapshot.
            if (this.serverPushedRecents) return;
            this.setRecentScripts(paths);
        } catch (err) {
            this.logger.error("Failed to fetch recent scripts:", err);
            if (this.serverPushedRecents) return;
            this.setRecentScripts([]);
        }
    }

    private setRecentScripts(paths: readonly string[]) {
        this.recentScripts = [...paths];
        this.fireChanged();
    }

    private fireChanged() {
        for (const callback of this.onChangedCallbacks) {
            try {
                callback();
            } catch (err) {
                this.logger.error("A RecentScriptsStore onChanged callback threw:", err);
            }
        }
    }
}
