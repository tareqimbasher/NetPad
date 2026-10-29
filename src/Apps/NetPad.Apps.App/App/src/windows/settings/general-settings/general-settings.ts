import {bindable} from "aurelia";
import {Settings} from "@application";
import {ITranslationService} from "@application/i18n/itranslation-service";

export class GeneralSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;
    public readonly translation: ITranslationService;

    constructor(
        currentSettings: Settings,
        @ITranslationService translation: ITranslationService
    ) {
        this.currentSettings = currentSettings;
        this.translation = translation;
    }
}
