import {ILogger} from "aurelia";
import {Util, WithDisposables} from "@common";
import {ChannelInfo, IBackgroundService, Shortcut} from "@application";
import {IMainMenuService} from "@application/main-menu/imain-menu-service";
import {IMenuItem} from "@application/main-menu/imenu-item";
import {ClickMenuItemCommand} from "@application/main-menu/click-menu-item-command";
import {ElectronIpcGateway} from "./electron-ipc-gateway";
import {electronConstants} from "../electron-shared";

/**
 * Handles IPC events sent by the Electron main process related to the native main menu.
 */
export class NativeMainMenuEventHandler extends WithDisposables implements IBackgroundService {
    private readonly logger: ILogger;
    private readonly bootstrapChannel = new ChannelInfo(electronConstants.ipcEventNames.mainMenuBootstrap);

    private readonly sendBootstrapDataToMain = Util.debounceAsync(this, async () => {
        try {
            await this.electronIpcGateway.send(this.bootstrapChannel, {
                menuItems: this.mainMenuService!.items.map(i => this.mapToMenuItemDto(i))
            });
        } catch (err) {
            // ignore, Main process event handler might not be setup yet.
        }
    }, 150, true);

    constructor(
        private readonly electronIpcGateway: ElectronIpcGateway,
        @IMainMenuService private readonly mainMenuService: IMainMenuService,
        @ILogger logger: ILogger
    ) {
        super();
        this.logger = logger.scopeTo(nameof(NativeMainMenuEventHandler));
    }

    public async start(): Promise<void> {
        // Handle native menu click events
        this.addDisposable(this.electronIpcGateway.subscribe(new ChannelInfo(ClickMenuItemCommand), (event: ClickMenuItemCommand) => {
            this.mainMenuService?.clickMenuItem(event.menuItemId);
        }));

        // Handle native menu bootstrap
        this.addDisposable(this.electronIpcGateway.subscribe(this.bootstrapChannel, () => this.sendBootstrapDataToMain()));

        this.addDisposable(
            this.mainMenuService.onChanged(() => this.sendBootstrapDataToMain())
        );

        try {
            await this.mainMenuService.initialized;
        } catch (err) {
            this.logger.error("Main menu initial hydration failed; bootstrapping native menu anyway:", err);
        }

        // Send right away to take care of any race-condition that might occur.
        this.sendBootstrapDataToMain();
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
            disabled: menuItem.disabled,
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
