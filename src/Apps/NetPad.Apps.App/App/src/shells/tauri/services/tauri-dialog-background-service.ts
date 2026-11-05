import {ILogger} from "aurelia";
import {html, WithDisposables} from "@common";
import {
    ChannelInfo,
    ConfirmOpenAsDuplicateCommand,
    ConfirmSaveCommand,
    IBackgroundService,
    IEventBus,
    IIpcGateway,
    RequestScriptSavePathCommand,
    YesNoCancel
} from "@application";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {answerBackend} from "@application/background-services/answer-backend";
import {save} from '@tauri-apps/plugin-dialog'

/**
 * This is utilized for the Tauri app
 * This enables opening specific dialog windows when running the tauri app.
 */
export class TauriDialogBackgroundService extends WithDisposables implements IBackgroundService {
    private readonly logger: ILogger;

    constructor(@IEventBus readonly eventBus: IEventBus,
                @IIpcGateway readonly ipcGateway: IIpcGateway,
                private readonly dialogUtil: DialogUtil,
                @ILogger logger: ILogger
    ) {
        super();
        this.logger = logger.scopeTo(nameof(TauriDialogBackgroundService));
    }

    public start(): Promise<void> {
        this.addDisposable(
            this.eventBus.subscribeToServer(ConfirmSaveCommand, async msg => {
                await this.confirmSave(msg);
            })
        );

        this.addDisposable(
            this.eventBus.subscribeToServer(RequestScriptSavePathCommand, async msg => {
                await this.requestScriptSavePath(msg);
            })
        );

        this.addDisposable(
            this.eventBus.subscribeToServer(ConfirmOpenAsDuplicateCommand, async msg => {
                await this.confirmOpenAsDuplicate(msg);
            })
        );

        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.dispose();
    }

    private async confirmSave(command: ConfirmSaveCommand) {
        await this.answer<YesNoCancel>(command.requestId, "Cancel", async () => {
            const response = await this.dialogUtil.ask({
                title: "Unsaved Changes",
                messageHtml: html`<b>${command.scriptName}</b> has unsaved changes. Do you want to save?`,
                buttons: [
                    {
                        text: "Cancel",
                    },
                    {
                        text: "No",
                    },
                    {
                        text: "Yes",
                        isPrimary: true
                    }
                ]
            });

            const answer = response.value;
            return answer === "Yes" ? "Yes" : answer === "No" ? "No" : "Cancel";
        });
    }

    private async requestScriptSavePath(command: RequestScriptSavePathCommand) {
        const path = await save({
            title: "Save Script",
            canCreateDirectories: true,
            defaultPath: command.defaultPath,
            filters: [
                {
                    name: "NetPad Script",
                    extensions: ["netpad"]
                }
            ]
        })

        await this.ipcGateway.send(new ChannelInfo("Respond"), command.requestId, path || null);
    }

    private async confirmOpenAsDuplicate(command: ConfirmOpenAsDuplicateCommand) {
        await this.answer(command.requestId, false, async () => {
            const response = await this.dialogUtil.ask({
                title: "Duplicate Script ID",
                message: command.message ?? "",
                buttons: [
                    {text: "Open as separate"},
                    {text: "Go to existing tab", isPrimary: true}
                ]
            });

            return response.value === "Open as separate";
        });
    }

    private answer<T>(requestId: string, unanswered: T, ask: () => Promise<T>) {
        return answerBackend(this.ipcGateway, this.logger, requestId, unanswered, ask);
    }
}
