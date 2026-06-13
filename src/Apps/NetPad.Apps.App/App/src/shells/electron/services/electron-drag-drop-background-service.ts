import {ILogger} from "aurelia";
import {WithDisposables} from "@common";
import {IBackgroundService, ISession} from "@application";
import {webUtils} from "electron";

/**
 * Handles drag-and-drop of `.netpad` files onto the Electron window.
 */
export class ElectronDragDropBackgroundService extends WithDisposables implements IBackgroundService {
    private readonly logger: ILogger;

    constructor(
        @ISession private readonly session: ISession,
        @ILogger logger: ILogger
    ) {
        super();
        this.logger = logger.scopeTo(nameof(ElectronDragDropBackgroundService));
    }

    public start(): Promise<void> {
        const dragOverHandler = (event: DragEvent) => {
            event.preventDefault();
        };

        const dropHandler = (event: DragEvent) => {
            event.preventDefault();

            const files = event.dataTransfer?.files;
            if (!files || files.length === 0) return;

            const paths: string[] = [];
            for (let i = 0; i < files.length; i++) {
                const file = files.item(i);
                if (!file) continue;
                try {
                    const path = webUtils.getPathForFile(file);
                    if (path && path.toLowerCase().endsWith(".netpad")) {
                        paths.push(path);
                    }
                } catch (err) {
                    this.logger.error("Failed to resolve file path from drop:", err);
                }
            }

            void (async () => {
                for (const path of paths) {
                    await this.session.openByPath(path).catch(() => undefined);
                }
            })();
        };

        document.addEventListener("dragover", dragOverHandler);
        document.addEventListener("drop", dropHandler);

        this.addDisposable(() => document.removeEventListener("dragover", dragOverHandler));
        this.addDisposable(() => document.removeEventListener("drop", dropHandler));

        return Promise.resolve();
    }

    public stop(): void {
        this.dispose();
    }
}
