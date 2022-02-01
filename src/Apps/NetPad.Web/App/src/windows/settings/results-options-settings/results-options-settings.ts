import {bindable} from "aurelia";
import {Settings} from "@domain";

export class ResultsOptionsSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    constructor(currentSettings: Settings) {
        this.currentSettings = currentSettings;
    }
}
