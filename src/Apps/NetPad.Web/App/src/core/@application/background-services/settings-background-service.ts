import {IBackgroundService} from "@common";
import {IEventBus, Settings, SettingsUpdated} from "@domain";
import {IDisposable} from "aurelia";

export class SettingsBackgroundService implements IBackgroundService {
    private settingsUpdatedEventToken: IDisposable;

    constructor(readonly settings: Settings, @IEventBus readonly eventBus: IEventBus) {
    }

    start(): Promise<void> {
        this.settingsUpdatedEventToken = this.eventBus.subscribeToServer(SettingsUpdated, msg => {
            console.warn("Got settings updated message", msg);
            this.settings.init(msg.settings);
        })
        return Promise.resolve(undefined);
    }

    stop(): void {
        this.settingsUpdatedEventToken.dispose();
    }
}
