import {ISession, ISettingService} from "@domain";

export class Statusbar {
    constructor(@ISession readonly session: ISession,
                @ISettingService readonly settingsService: ISettingService) {
    }
}
