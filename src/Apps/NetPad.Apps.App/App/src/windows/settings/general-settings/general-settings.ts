import {bindable} from "aurelia";
import {AppIdentifier, IAppService, Settings} from "@domain";
import {Util} from "@common";

export class GeneralSettings {
    @bindable public settings: Settings;
    public appId: AppIdentifier;
    public currentSettings: Readonly<Settings>;

    constructor(currentSettings: Settings, @IAppService private readonly appService: IAppService) {
        this.currentSettings = currentSettings;
    }

    public binding() {
        this.appService.getIdentifier().then(id => {
            if (id.version.endsWith(".0"))
                id.version = id.version.substring(0, id.version.length - 2);

            this.appId = id;
        });
    }
}
