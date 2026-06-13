import {ILogger} from "aurelia";
import {WithDisposables} from "@common";
import {IBackgroundService, ISession} from "@application";
import {getCurrentWebview} from "@tauri-apps/api/webview";

/**
 * Listens for OS file drops on the Tauri webview and opens any dropped `.netpad` files.
 */
export class TauriDragDropBackgroundService extends WithDisposables implements IBackgroundService {
    private readonly logger: ILogger;

    constructor(
        @ISession private readonly session: ISession,
        @ILogger logger: ILogger
    ) {
        super();
        this.logger = logger.scopeTo(nameof(TauriDragDropBackgroundService));
    }

    public async start(): Promise<void> {
        const unlisten = await getCurrentWebview().onDragDropEvent(async event => {
            if (event.payload.type !== "drop") return;

            const paths = event.payload.paths ?? [];
            const netpadFiles = paths.filter(p => p.toLowerCase().endsWith(".netpad"));

            for (const path of netpadFiles) {
                await this.session.openByPath(path).catch(() => undefined);
            }
        });

        this.addDisposable(unlisten);
    }

    public stop(): void {
        this.dispose();
    }
}
