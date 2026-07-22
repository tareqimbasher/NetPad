import {IContainer} from "aurelia";
import {ISettingsService, IWindowService, MonacoEnvironmentManager, Settings} from "@application";
import {IconName} from "@application/ui/np-icon/icons";
import {WindowBase} from "@application/windowing/window-base";
import {WindowParams} from "@application/windowing/window-params";

interface SettingsPage {
    route: string;
    text: string;
    icon: IconName;
}

export class Window extends WindowBase {
    public editableSettings: Settings;
    public selectedPage: SettingsPage;

    public pages: SettingsPage[] = [
        {route: "general", text: "General", icon: "settings"},
        {route: "editor", text: "Editor", icon: "code"},
        {route: "results", text: "Results", icon: "results"},
        {route: "style", text: "Custom CSS", icon: "custom-css"},
        {route: "keyboard-shortcuts", text: "Shortcuts", icon: "keyboard"},
        {route: "omnisharp", text: "OmniSharp", icon: "code-intelligence"},
        {route: "about", text: "About", icon: "info"},
    ];

    /**
     * Settings that the user changes with a single action but that are stored as more than one
     * value, so the unsaved-changes count matches what they did rather than how it is persisted.
     */
    private static readonly compositeSettings: Record<string, string> = {
        "appearance.themeFamily": "appearance.theme",
        "appearance.mode": "appearance.theme",
    };

    constructor(
        @ISettingsService private readonly settingsService: ISettingsService,
        @IWindowService private readonly windowService: IWindowService,
        @IContainer private readonly container: IContainer) {
        super();

        document.title = "Settings";

        let pageIndex = this.pages.findIndex(t => t.route === WindowParams.get("tab"));
        if (pageIndex < 0)
            pageIndex = 0;

        this.selectedPage = this.pages[pageIndex];
        this.editableSettings = this.settings.clone();
    }

    public async binding() {
        await MonacoEnvironmentManager.setupMonacoEnvironment(this.container);
    }

    public get canApply() {
        return JSON.stringify(this.settings) !== JSON.stringify(this.editableSettings);
    }

    public get unsavedChangeCount(): number {
        const changed = new Set<string>();
        Window.collectChanges(this.settings, this.editableSettings, "", changed);
        return changed.size;
    }

    public get unsavedChangeText(): string {
        const count = this.unsavedChangeCount;
        return `${count} unsaved ${count === 1 ? "change" : "changes"}`;
    }

    public async apply(): Promise<boolean> {
        if (!this.validate()) {
            return false;
        }

        try {
            await this.settingsService.update(this.editableSettings);
            return true;
        } catch (e) {
            this.logger.error("Error while saving settings", e);
            alert("A problem occurred. Could not save settings");
            return false;
        }
    }

    public async save() {
        if (!await this.apply()) {
            return;
        }

        await this.windowService.close();
    }

    public async close() {
        await this.windowService.close();
    }

    public async showAppDataFolder() {
        await this.settingsService.showSettingsFile();
    }

    /**
     * Walks both settings trees and records the path of every leaf that differs. A collection
     * counts as one leaf: "added two namespaces" is one thing the user did, not two.
     */
    private static collectChanges(saved: unknown, edited: unknown, path: string, changed: Set<string>) {
        const isWalkable = (value: unknown) =>
            typeof value === "object" && value !== null && !Array.isArray(value);

        if (isWalkable(saved) && isWalkable(edited)) {
            const keys = new Set([
                ...Object.keys(saved as object),
                ...Object.keys(edited as object),
            ]);

            for (const key of keys) {
                Window.collectChanges(
                    (saved as Record<string, unknown>)[key],
                    (edited as Record<string, unknown>)[key],
                    path ? `${path}.${key}` : key,
                    changed);
            }

            return;
        }

        if (JSON.stringify(saved) !== JSON.stringify(edited)) {
            changed.add(Window.compositeSettings[path] ?? path);
        }
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
