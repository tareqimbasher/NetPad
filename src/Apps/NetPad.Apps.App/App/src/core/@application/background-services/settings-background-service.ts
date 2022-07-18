import {IBackgroundService} from "@common";
import {IEventBus, Settings, SettingsUpdatedEvent} from "@domain";
import {IDisposable} from "aurelia";

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
