import {ILogger, IObserverLocator, singleton} from "aurelia";
import {LocalStorageBacked} from "@common";

@singleton()
export class WorkAreaAppearance extends LocalStorageBacked {
    public style: "minimal" | "bold" = "minimal";
    public size: "comfy" | "compact" = "comfy";

    private logger: ILogger;

    constructor(
        @IObserverLocator observerLocator: IObserverLocator,
        @ILogger logger
    ) {
        super("work-area.appearance");
        this.logger = logger.scopeTo(nameof(WorkAreaAppearance));

        this.migrateOlderVersions();

        super.autoSave(observerLocator, [
            nameof(this.style),
            nameof(this.size),
        ]);
    }

    private migrateOlderVersions() {
        const olderVersions = [
            "script-environments.header-style.value"
        ];

        for (const olderVersion of olderVersions) {
            const oldValue = localStorage.getItem(olderVersion);
            if (oldValue) {
                this.logger.info(
                    `Migrating older settings from ${olderVersion}. Latest settings were: `,
                    localStorage.getItem(this.localStorageKey))

                localStorage.setItem(this.localStorageKey, oldValue);

                localStorage.removeItem(olderVersion);

                // Only the first matching old version will be migrated
                break;
            }
        }
    }
}
