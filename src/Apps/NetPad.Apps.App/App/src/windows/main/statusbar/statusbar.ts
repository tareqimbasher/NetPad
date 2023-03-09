import {
    AppStatusMessage,
    AppStatusMessagePublishedEvent,
    IEventBus,
    ISession,
    ISettingService,
    ScriptEnvironment,
} from "@domain";
import {IDialogService, PLATFORM} from "aurelia";
import {IShortcutManager} from "@application";
import {DialogBase} from "@application/dialogs/dialog-base";
import {
    AppDependenciesCheckDialog
} from "@application/dialogs/app-dependencies-check-dialog/app-dependencies-check-dialog";
import {QuickTipsDialog} from "@application/dialogs/quick-tips-dialog/quick-tips-dialog";

export class Statusbar {
    public appStatusMessage: IAppStatusMessage | null;
    public lastPersistantPriorityMessage: IAppStatusMessage | null;

    constructor(@ISession private readonly session: ISession,
                @ISettingService private readonly settingsService: ISettingService,
                @IShortcutManager private readonly shortcutManager: IShortcutManager,
                @IDialogService private readonly dialogService: IDialogService,
                @IEventBus private readonly eventBus: IEventBus) {
    }

    public get activeEnvironment(): ScriptEnvironment | null | undefined {
        return this.session.active;
    }

    public binding() {
        this.listenToAppStatusMessages();
    }

    public async showAppDepsCheckDialog() {
        await DialogBase.toggle(this.dialogService, AppDependenciesCheckDialog);
    }

    public async showQuickTipsDialog() {
        await DialogBase.toggle(this.dialogService, QuickTipsDialog);
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
