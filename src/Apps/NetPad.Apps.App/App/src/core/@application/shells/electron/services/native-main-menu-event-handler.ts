import {WithDisposables} from "@common";
import {ChannelInfo, IBackgroundService, Shortcut} from "@application";
import {ElectronIpcGateway} from "./electron-ipc-gateway";
import {IMainMenuService} from "@application/main-menu/imain-menu-service";
import {IMenuItem} from "@application/main-menu/imenu-item";
import {ClickMenuItemCommand} from "@application/main-menu/click-menu-item-command";
import {electronConstants} from "@application/shells/electron/electron-shared";

/**
 * Handles IPC events sent by the Electron main process related to the native main menu.
 */
export class NativeMainMenuEventHandler extends WithDisposables implements IBackgroundService {
    constructor(private readonly electronIpcGateway: ElectronIpcGateway, @IMainMenuService private readonly mainMenuService: IMainMenuService) {
        super();
    }

    public start(): Promise<void> {
        // Handle native menu click events
        this.addDisposable(this.electronIpcGateway.subscribe(new ChannelInfo(ClickMenuItemCommand), (event: ClickMenuItemCommand) => {
            this.mainMenuService?.clickMenuItem(event.menuItemId);
        }));

        // Handle native menu bootstrap
        const bootstrapChannel = new ChannelInfo(electronConstants.ipcEventNames.mainMenuBootstrap);

        const sendBootstrapDataToMain = () => {
            try {
                this.electronIpcGateway.send(bootstrapChannel, {
                    menuItems: this.mainMenuService!.items.map(i => this.mapToMenuItemDto(i))
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
