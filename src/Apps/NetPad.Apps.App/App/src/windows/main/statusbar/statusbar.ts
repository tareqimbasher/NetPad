import {
    INotificationService,
    ISession,
    ISettingsService,
    IShortcutManager,
    ScriptEnvironment,
    Settings,
    severityIconClass,
    severityTextClass,
} from "@application";
import {AppDependenciesCheckDialog} from "@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog";
import {QuickTipsDialog} from "@application/app/quick-tips-dialog/quick-tips-dialog";
import {Workbench} from "../workbench";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {Util} from "@common";

export class Statusbar {
    public readonly iconClass = severityIconClass;
    public readonly textClass = severityTextClass;

    constructor(private readonly workbench: Workbench,
                private readonly settings: Settings,
                @ISession private readonly session: ISession,
                @ISettingsService private readonly settingsService: ISettingsService,
                @IShortcutManager private readonly shortcutManager: IShortcutManager,
                @INotificationService readonly notificationService: INotificationService,
                private readonly dialogUtil: DialogUtil) {
    }

    public get activeEnvironment(): ScriptEnvironment | null | undefined {
        return this.session.active;
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
