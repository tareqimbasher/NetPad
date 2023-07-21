import {ISettingService, Settings} from "@domain";
import {WindowBase} from "@application/windows/window-base";

export class Window extends WindowBase {
    public editableSettings: Settings;
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
        @ISettingService readonly settingService: ISettingService) {
        super();

        document.title = "Settings";

        let tabIndex = this.tabs.findIndex(t => t.route === this.startupOptions.get("tab"));
        if (tabIndex < 0)
            tabIndex = 0;

        this.selectedTab = this.tabs[tabIndex];
        this.editableSettings = this.settings.clone();
    }

    public async save() {
        if (!this.validate()) {
            return;
        }

        await this.settingService.update(this.editableSettings);
        window.close();
    }

    public cancel() {
        window.close();
    }

    private validate(): boolean {
        let userValue: unknown = this.editableSettings.results.maxSerializationDepth;
        if ((userValue !== 0 && !userValue) || isNaN(Number(userValue))) {
            alert("Results > Serialization > Max Depth is required.");
            return false;
        }

        userValue = this.editableSettings.results.maxCollectionSerializeLength;
        if ((userValue !== 0 && !userValue) || isNaN(Number(userValue))) {
            alert("Results > Serialization > Max Collection Length is required.");
            return false;
        }

        return true;
    }
}
