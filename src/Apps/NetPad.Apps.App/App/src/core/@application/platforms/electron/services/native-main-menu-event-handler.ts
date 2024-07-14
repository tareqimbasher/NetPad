import {IContainer} from "aurelia";
import {WithDisposables} from "@common";
import {ChannelInfo, ClickMenuItemEvent, IBackgroundService, Shortcut} from "@application";
import {ElectronIpcGateway} from "./electron-ipc-gateway";
import {IMainMenuService} from "@application/main-menu/main-menu-service";
import {IMenuItem} from "@application/main-menu/imenu-item";

/**
 * Handles IPC events sent by the Electron main process related to the native main menu.
 */
export class NativeMainMenuEventHandler extends WithDisposables implements IBackgroundService {
    private readonly mainMenuService?: IMainMenuService;

    constructor(private readonly electronIpcGateway: ElectronIpcGateway, @IContainer container: IContainer) {
        super();

        try {
            this.mainMenuService = container.get(IMainMenuService);
        } catch {
            // ignore, no main menu
        }
    }

    public start(): Promise<void> {
        // Handle native menu click events
        if (this.mainMenuService) {
            this.addDisposable(this.electronIpcGateway.subscribe(new ChannelInfo(ClickMenuItemEvent), (event: ClickMenuItemEvent) => {
                this.mainMenuService?.clickMenuItem(event.menuItemId);
            }));
        }

        // Handle native menu bootstrap
        const bootstrapChannel = new ChannelInfo("main-menu-bootstrap");

        const sendBootstrapDataToMain = () => {
            // If no main menu service is registered for the app/window then don't send the event to main process
            if (!this.mainMenuService) {
                return;
            }

            try {
                this.electronIpcGateway.send(bootstrapChannel, {
                    menuItems: this.mainMenuService.items.map(i => this.mapToMenuItemDto(i))
                });
            } catch (err) {
                // ignore, Main process event handler might not be setup yet.
            }
        };

        this.addDisposable(this.electronIpcGateway.subscribe(bootstrapChannel, () => sendBootstrapDataToMain()));

        // Send right away to take care of any race-condition that might occur.
        sendBootstrapDataToMain();

        return Promise.resolve();
    }

    public stop(): void {
        this.dispose();
    }

    private mapToMenuItemDto(menuItem: IMenuItem): unknown {
        return {
            id: menuItem.id,
            text: menuItem.text,
            icon: menuItem.icon,
            helpText: menuItem.helpText,
            shortcut: menuItem.shortcut ? this.mapToShortcutDto(menuItem.shortcut) : undefined,
            isDivider: menuItem.isDivider,
            menuItems: menuItem.menuItems?.map(x => this.mapToMenuItemDto(x)),
        };
    }

    private mapToShortcutDto(shortcut: Shortcut) {
        return {
            name: shortcut.name,
            isEnabled: shortcut.isEnabled,
            keyCombo: shortcut.keyCombo.asArray
        };
    }
}
