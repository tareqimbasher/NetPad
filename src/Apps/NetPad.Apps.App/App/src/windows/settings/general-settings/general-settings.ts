import {bindable} from "aurelia";
import {AppIdentifier, IAppService, Settings} from "@domain";
import {Util} from "@common";

export class GeneralSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    constructor(currentSettings: Settings) {
        this.currentSettings = currentSettings;
    }
}
