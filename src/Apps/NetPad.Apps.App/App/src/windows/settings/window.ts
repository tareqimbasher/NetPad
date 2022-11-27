import {ISettingService, Settings} from "@domain";

export class Window {
    public currentSettings: Readonly<Settings>;
    public settings: Settings;
    public selectedTab;
    public tabs = [
        {route: "general", text: "General"},
        {route: "editor", text: "Editor"},
        {route: "results", text: "Results"},
        {route: "keyboard-shortcuts", text: "Keyboard Shortcuts"},
        {route: "omnisharp", text: "OmniSharp"},
        {route: "about", text: "About"},
    ];

    constructor(
        private readonly startupOptions: URLSearchParams,
        currentSettings: Settings,
        @ISettingService readonly settingService: ISettingService) {

        document.title = "Settings";

        let tabIndex = this.tabs.findIndex(t => t.route === this.startupOptions.get("tab"));
        if (tabIndex < 0)
            tabIndex = 0;

        this.selectedTab = this.tabs[tabIndex];
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
