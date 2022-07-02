import {
    AppStatusMessage,
    AppStatusMessagePublished,
    IEventBus,
    ISession,
    ISettingService,
    IShortcutManager
} from "@domain";
import {PLATFORM, Task} from "aurelia";

export class Statusbar {
    public appStatusMessage?: AppStatusMessage;

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

        this.eventBus.subscribeToServer(AppStatusMessagePublished, ev => {
            this.appStatusMessage = ev.message;

            if (clearMsgTask) {
                clearMsgTask.cancel();
            }

            if (this.appStatusMessage.persistant) {
                return;
            }

            clearMsgTask = PLATFORM.taskQueue.queueTask(() => {
                this.appStatusMessage = null;
                clearMsgTask = null;
            }, { delay: 5000 });
        });
    }
}
