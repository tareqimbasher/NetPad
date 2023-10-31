import {
    AppStatusMessage,
    AppStatusMessagePublishedEvent,
    IEventBus,
    ISession,
    ISettingService,
    ScriptEnvironment,
    Settings,
} from "@domain";
import {PLATFORM} from "aurelia";
import {IShortcutManager} from "@application";
import {
    AppDependenciesCheckDialog
} from "@application/dialogs/app-dependencies-check-dialog/app-dependencies-check-dialog";
import {QuickTipsDialog} from "@application/dialogs/quick-tips-dialog/quick-tips-dialog";
import {Workbench} from "../workbench";
import {DialogUtil} from "@application/dialogs/dialog-util";

export class Statusbar {
    public appStatusMessage: IAppStatusMessage | null;
    public lastPersistantPriorityMessage: IAppStatusMessage | null;

    constructor(private readonly workbench: Workbench,
                private readonly settings: Settings,
                @ISession private readonly session: ISession,
                @ISettingService private readonly settingsService: ISettingService,
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
                this.lastPersistantPriorityMessage = this.appStatusMessage;
                return;
            }

            clearMsgTask = PLATFORM.setTimeout(() => {
                clearMsgTask = null;

                this.appStatusMessage = this.lastPersistantPriorityMessage
                    ? this.lastPersistantPriorityMessage
                    : null;
            }, 30000);
        });
    }
}

interface IAppStatusMessage extends AppStatusMessage {
    scriptName?: string;
}
