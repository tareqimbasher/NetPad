import {ISession, ISettingService, IShortcutManager} from "@domain";

export class Statusbar {
    constructor(@ISession readonly session: ISession,
                @ISettingService readonly settingsService: ISettingService,
                @IShortcutManager readonly shortcutManager: IShortcutManager) {
    }
}
