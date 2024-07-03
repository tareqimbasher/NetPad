import {IDisposable} from "@common";
import {IEventBus, Settings, SettingsUpdatedEvent} from "@application";
import {IBackgroundService} from "./ibackground-service";

/**
 * Used to update the main app Settings singleton when settings change.
 */
export class SettingsBackgroundService implements IBackgroundService {
    private settingsUpdatedEventSubscription: IDisposable;

    constructor(
        readonly settings: Settings,
        @IEventBus readonly eventBus: IEventBus) {
    }

    public start(): Promise<void> {
        this.settingsUpdatedEventSubscription = this.eventBus.subscribeToServer(SettingsUpdatedEvent, msg => {
            this.settings.init(msg.settings);
        });
        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.settingsUpdatedEventSubscription.dispose();
    }
}
