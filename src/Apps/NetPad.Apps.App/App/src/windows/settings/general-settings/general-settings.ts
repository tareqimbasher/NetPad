import {bindable} from "aurelia";
import {Settings} from "@application";

export class GeneralSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    constructor(currentSettings: Settings) {
        this.currentSettings = currentSettings;
    }
}
