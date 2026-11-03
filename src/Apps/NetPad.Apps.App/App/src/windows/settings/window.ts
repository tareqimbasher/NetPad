import {IContainer} from "aurelia";
import {
    IEventBus,
    ISettingsService,
    IWindowService,
    MonacoEnvironmentManager,
    Settings,
    SettingsUpdatedEvent
} from "@application";
import {WindowBase} from "@application/windowing/window-base";
import {SettingsStore} from "./settings-store";
import {collectChanges, mergeSavedSettings} from "./settings-changes";

export class Window extends WindowBase {
    public editableSettings: Settings;

    /** The last saved settings this window knows about. Used to tell which values the user changed. */
    private base: Settings;

    constructor(
        private readonly settingsStore: SettingsStore,
        @ISettingsService private readonly settingsService: ISettingsService,
        @IWindowService private readonly windowService: IWindowService,
        @IEventBus private readonly eventBus: IEventBus,
        @IContainer private readonly container: IContainer) {
        super();

        document.title = "Settings";

        this.editableSettings = Window.deepCopy(this.settings);
        this.base = Window.deepCopy(this.settings);
    }

    public async binding() {
        this.addDisposable(this.eventBus.subscribeToServer(SettingsUpdatedEvent, msg => this.syncSaved(msg.settings)));

        await MonacoEnvironmentManager.setupMonacoEnvironment(this.container);
    }

    public get canApply() {
        return JSON.stringify(this.settings) !== JSON.stringify(this.editableSettings);
    }

    public get unsavedChangeCount(): number {
        const changed = new Set<string>();
        collectChanges(this.settings, this.editableSettings, "", changed);
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

    // Takes in a save made elsewhere while this window is open, so saving after doesn't write the old values back.
    private syncSaved(saved: Settings) {
        mergeSavedSettings(
            this.base,
            this.editableSettings as unknown as Record<string, unknown>,
            Window.deepCopy(saved) as unknown as Record<string, unknown>);

        this.base = Window.deepCopy(saved);
    }

    private static deepCopy(settings: Settings): Settings {
        return Settings.fromJS(JSON.parse(JSON.stringify(settings)));
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
