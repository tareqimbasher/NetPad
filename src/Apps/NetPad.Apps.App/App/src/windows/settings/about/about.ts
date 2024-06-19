import {bindable} from "aurelia";
import {AppIdentifier, IAppService, Settings} from "@domain";

export class About {
    @bindable public settings: Settings;
    public appId: AppIdentifier;
    public currentSettings: Readonly<Settings>;

    constructor(currentSettings: Settings, @IAppService private readonly appService: IAppService) {
        this.currentSettings = currentSettings;
    }

    public binding() {
        this.appService.getIdentifier().then(id => {
            this.appId = id;
        });
    }
}
