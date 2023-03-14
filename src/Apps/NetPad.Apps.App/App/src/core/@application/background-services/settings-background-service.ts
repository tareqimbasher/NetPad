import {IBackgroundService, IDisposable} from "@common";
import {IEventBus, Settings, SettingsUpdatedEvent} from "@domain";

/**
 * Used to sync the Settings singleton from changes upstream.
 */
export class SettingsBackgroundService implements IBackgroundService {
    private settingsUpdatedEventToken: IDisposable;

    constructor(
        readonly settings: Settings,
        @IEventBus readonly eventBus: IEventBus) {
    }

    public start(): Promise<void> {
        this.settingsUpdatedEventToken = this.eventBus.subscribeToServer(SettingsUpdatedEvent, msg => {
            this.settings.init(msg.settings);
        });
        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.settingsUpdatedEventToken.dispose();
    }
}
