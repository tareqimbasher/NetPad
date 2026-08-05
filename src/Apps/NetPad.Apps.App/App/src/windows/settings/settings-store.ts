import {IWindowDestinations} from "@application/windowing/iwindow-destinations";
import {WindowParams} from "@application/windowing/window-params";
import {IconName} from "@application/ui/np-icon/icons";

export interface SettingsPage {
    route: string;
    text: string;
    icon: IconName;
}

export class SettingsStore implements IWindowDestinations {
    public readonly identityParams: string[] = [];

    public selectedPage: SettingsPage;

    public readonly pages: SettingsPage[] = [
        {route: "general", text: "General", icon: "settings"},
        {route: "editor", text: "Editor", icon: "code"},
        {route: "results", text: "Results", icon: "results"},
        {route: "style", text: "Custom CSS", icon: "custom-css"},
        {route: "keyboard-shortcuts", text: "Shortcuts", icon: "keyboard"},
        {route: "omnisharp", text: "OmniSharp", icon: "code-intelligence"},
        {route: "about", text: "About", icon: "info"},
    ];

    constructor() {
        this.selectedPage = this.pageAt(WindowParams.get("tab")) ?? this.pages[0];
    }

    public goTo(route: string) {
        const page = this.pageAt(route);
        if (page) {
            this.selectedPage = page;
        }
    }

    private pageAt(route: string | null): SettingsPage | undefined {
        return this.pages.find(p => p.route === route);
    }
}
