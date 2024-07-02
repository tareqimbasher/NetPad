import {bindable} from "aurelia";
import {Settings} from "@application";

export class ResultsOptionsSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    constructor(currentSettings: Settings) {
        this.currentSettings = currentSettings;
    }
}
