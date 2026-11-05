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
import {collectChanges} from "./settings-changes";
import {SettingsDraft} from "./settings-draft";
import {DialogUtil} from "@application/dialogs/dialog-util";

export class Window extends WindowBase {
    public editableSettings: Settings;
    private readonly draft: SettingsDraft;

    constructor(
        private readonly settingsStore: SettingsStore,
        private readonly dialogUtil: DialogUtil,
        @ISettingsService private readonly settingsService: ISettingsService,
        @IWindowService private readonly windowService: IWindowService,
        @IEventBus private readonly eventBus: IEventBus,
        @IContainer private readonly container: IContainer) {
        super();

        document.title = "Settings";

        this.draft = new SettingsDraft(this.settings as Settings);
        this.editableSettings = this.draft.edited;
    }

    public async binding() {
        this.addDisposable(this.eventBus.subscribeToServer(SettingsUpdatedEvent, msg => this.draft.syncSaved(msg.settings)));

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
        try {
            await this.settingsService.update(this.draft.beginSave());
            return true;
        } catch (e) {
            this.draft.saveFailed();
            this.logger.error("Error while saving settings", e);
            await this.dialogUtil.alert({
                title: "Couldn't Save Settings",
                message: "Your changes are still here. Try again, or check the log for the cause.",
            });
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
}
