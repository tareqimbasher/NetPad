import {
    AppStatusMessage,
    AppStatusMessagePublishedEvent,
    IEventBus,
    ISession,
    ISettingsService,
    IShortcutManager,
    ScriptEnvironment,
    Settings,
} from "@application";
import {PLATFORM} from "aurelia";
import {AppDependenciesCheckDialog} from "@application/app/app-dependencies-check-dialog/app-dependencies-check-dialog";
import {QuickTipsDialog} from "@application/app/quick-tips-dialog/quick-tips-dialog";
import {Workbench} from "../workbench";
import {DialogUtil} from "@application/dialogs/dialog-util";

export class Statusbar {
    public appStatusMessage: IAppStatusMessage | null;
    public lastPersistentPriorityMessage: IAppStatusMessage | null;

    constructor(private readonly workbench: Workbench,
                private readonly settings: Settings,
                @ISession private readonly session: ISession,
                @ISettingsService private readonly settingsService: ISettingsService,
                @IShortcutManager private readonly shortcutManager: IShortcutManager,
                private readonly dialogUtil: DialogUtil,
                @IEventBus private readonly eventBus: IEventBus) {
    }

    public get activeEnvironment(): ScriptEnvironment | null | undefined {
        return this.session.active;
    }

    public binding() {
        this.listenToAppStatusMessages();
    }

    private async showAppDepsCheckDialog() {
        await this.dialogUtil.toggle(AppDependenciesCheckDialog);
    }

    private async showQuickTipsDialog() {
        await this.dialogUtil.toggle(QuickTipsDialog);
    }

    private listenToAppStatusMessages() {
        let clearMsgTask: number | null = null;

        this.eventBus.subscribeToServer(AppStatusMessagePublishedEvent, ev => {
            this.appStatusMessage = ev.message;
            if (this.appStatusMessage.scriptId) {
                this.appStatusMessage.scriptName = this.session.getScriptName(this.appStatusMessage.scriptId);
            }

            if (clearMsgTask !== null) {
                PLATFORM.clearTimeout(clearMsgTask);
            }

            if (this.appStatusMessage.persistant) {
                this.lastPersistentPriorityMessage = this.appStatusMessage;
                return;
            }

            clearMsgTask = PLATFORM.setTimeout(() => {
                clearMsgTask = null;

                this.appStatusMessage = this.lastPersistentPriorityMessage
                    ? this.lastPersistentPriorityMessage
                    : null;
            }, 30000);
        });
    }
}

interface IAppStatusMessage extends AppStatusMessage {
    scriptName?: string;
}
