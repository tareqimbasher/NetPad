import {
    CommandIds,
    IKeybindingManager,
    INotificationService,
    ISession,
    ISettingsService,
    ScriptEnvironment,
    Settings,
    severityIcon,
    severityTextClass,
} from "@application";
import {AppDependenciesCheckDialog} from "@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog";
import {QuickTipsDialog} from "@application/app/quick-tips-dialog/quick-tips-dialog";
import {Workbench} from "../workbench";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {Util} from "@common";

export class Statusbar {
    public readonly icon = severityIcon;
    public readonly textClass = severityTextClass;

    constructor(private readonly workbench: Workbench,
                private readonly settings: Settings,
                @ISession private readonly session: ISession,
                @ISettingsService private readonly settingsService: ISettingsService,
                @IKeybindingManager private readonly keybindingManager: IKeybindingManager,
                @INotificationService readonly notificationService: INotificationService,
                private readonly dialogUtil: DialogUtil) {
    }

    public get settingsTooltip(): string {
        return this.keybindingManager.describe(CommandIds.openSettings);
    }

    public get activeEnvironment(): ScriptEnvironment | null | undefined {
        return this.session.active;
    }

    /** The state-dot class for the active script. */
    public get activeStateDot(): string {
        switch (this.session.active?.status) {
            case "Running":
                return "running";
            case "Stopping":
                return "stopping";
            case "Error":
                return "error";
            case "Ready":
                return this.session.active?.runDurationMilliseconds != null ? "success" : "";
            default:
                return "";
        }
    }

    /** Scripts running somewhere other than the active tab, so their state stays visible. */
    public get backgroundRunning(): ReadonlyArray<ScriptEnvironment> {
        return this.session.environments.filter(
            env => env.status === "Running" && env.script.id !== this.session.active?.script.id);
    }

    public get backgroundRunningLabel(): string {
        const running = this.backgroundRunning;
        return running.length === 1 ? running[0].script.name : `${running.length} scripts`;
    }

    public get backgroundRunningTooltip(): string {
        const running = this.backgroundRunning;
        return running.length === 1
            ? `${running[0].script.name} is running in the background`
            : `Running in the background:\n- ${running.map(env => env.script.name).join("\n- ")}`;
    }

    public get runDuration(): string | null {
        const env = this.session.active;
        if (!env || env.runDurationMilliseconds === undefined || env.runDurationMilliseconds === null) {
            return null;
        }
        return Util.formatDurationMs(env.runDurationMilliseconds);
    }

    private async showAppDepsCheckDialog() {
        await this.dialogUtil.toggle(AppDependenciesCheckDialog);
    }

    private async showQuickTipsDialog() {
        await this.dialogUtil.toggle(QuickTipsDialog);
    }

    public dismissStatusBarMessage(event?: Event) {
        event?.preventDefault();
        this.notificationService.dismissStatusBarMessage();
    }
}
