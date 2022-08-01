import {AppStatusMessage, AppStatusMessagePublishedEvent, IEventBus, ISession, ISettingService,} from "@domain";
import {PLATFORM, Task} from "aurelia";
import {IShortcutManager} from "@application";

export class Statusbar {
    public appStatusMessage?: IAppStatusMessage;

    constructor(@ISession private readonly session: ISession,
                @ISettingService private readonly settingsService: ISettingService,
                @IShortcutManager private readonly shortcutManager: IShortcutManager,
                @IEventBus private readonly eventBus: IEventBus) {
    }

    public binding() {
        this.listenToAppStatusMessages();
    }

    private listenToAppStatusMessages() {
        let clearMsgTask: Task<void>;

        this.eventBus.subscribeToServer(AppStatusMessagePublishedEvent, ev => {
            this.appStatusMessage = ev.message;
            if (this.appStatusMessage.scriptId) {
                this.appStatusMessage.scriptName = this.session.getScriptName(this.appStatusMessage.scriptId);
            }

            if (clearMsgTask) {
                clearMsgTask.cancel();
            }

            if (this.appStatusMessage.persistant) {
                return;
            }

            clearMsgTask = PLATFORM.taskQueue.queueTask(() => {
                this.appStatusMessage = null;
                clearMsgTask = null;
            }, { delay: 10000 });
        });
    }
}

interface IAppStatusMessage extends AppStatusMessage {
    scriptName?: string;
}
