import {IDisposable} from "@common";
import {IEventBus, Settings, SettingsUpdatedEvent} from "@domain";
import {ClickMenuItemEvent, IBackgroundService} from "@application";
import {IMainMenuService} from "@application/main-menu/main-menu-service";

export class MainMenuBackgroundService implements IBackgroundService {
    private clickMenuItemEventSubscription: IDisposable;

    constructor(@IEventBus private readonly eventBus: IEventBus,
                @IMainMenuService private readonly mainMenuService: IMainMenuService) {
    }

    public start(): Promise<void> {
        this.clickMenuItemEventSubscription = this.eventBus.subscribe(ClickMenuItemEvent, async msg => {
            await this.mainMenuService.clickMenuItem(msg.menuItemId);
        });

        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.clickMenuItemEventSubscription.dispose();
    }
}
