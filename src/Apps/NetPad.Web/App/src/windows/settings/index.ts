import {ISettingService, Settings} from "@domain";

export class Index {
    public currentSettings: Readonly<Settings>;
    public settings: Settings;
    public selectedTab;
    public tabs = [
        {route: "general", text: "General"},
        {route: "editor", text: "Editor"},
        {route: "results", text: "Results"},
    ];

    constructor(currentSettings: Settings, @ISettingService readonly settingService: ISettingService) {
        document.title = "Settings";

        this.selectedTab = this.tabs[0];
        this.currentSettings = currentSettings;
        this.settings = this.currentSettings.clone();
    }

    public async save() {
        await this.settingService.update(this.settings);
        window.close();
    }

    public cancel() {
        window.close();
    }
}
