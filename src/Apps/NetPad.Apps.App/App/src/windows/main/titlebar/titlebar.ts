import {ISession, ISettingService, IWindowService} from "@domain";
import {IShortcutManager} from "@application";

export class Titlebar {
    public isWindowAlwaysOnTop = false;

    constructor(@ISession private readonly session: ISession,
                @IWindowService private readonly windowService: IWindowService,
                @ISettingService private readonly settingsService: ISettingService,
                @IShortcutManager private readonly shortcutManager: IShortcutManager
    ) {
    }

    public get title() {
        const activeScriptName = this.session.active?.script.name;

        return !!activeScriptName ? activeScriptName + " - NetPad" : "NetPad";
    }

    public async minimize() {
        await this.windowService.minimize();
    }

    public async maximize() {
        await this.windowService.maximize();
    }

    public close() {
        window.close();
    }

    public async openSettingsWindow() {
        await this.settingsService.openSettingsWindow(null);
    }

    public async toggleWindowAlwaysOnTop() {
        await this.windowService.toggleAlwaysOnTop();
        this.isWindowAlwaysOnTop = !this.isWindowAlwaysOnTop;
    }
}
