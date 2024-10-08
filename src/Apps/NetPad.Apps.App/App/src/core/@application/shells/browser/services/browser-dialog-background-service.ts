import {WithDisposables} from "@common";
import {
    ChannelInfo,
    ConfirmSaveCommand,
    IBackgroundService,
    IEventBus,
    IIpcGateway,
    RequestScriptSavePathCommand,
    YesNoCancel
} from "@application";
import {DialogUtil} from "@application/dialogs/dialog-util";

/**
 * This is utilized for the Browser app, not the Electron app.
 * This enables opening specific dialog windows when running the browser app.
 */
export class BrowserDialogBackgroundService extends WithDisposables implements IBackgroundService {
    constructor(@IEventBus readonly eventBus: IEventBus,
                @IIpcGateway readonly ipcGateway: IIpcGateway,
                private readonly dialogUtil: DialogUtil
    ) {
        super();
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

        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.dispose();
    }

    private async confirmSave(command: ConfirmSaveCommand) {
        const response = await this.dialogUtil.ask({
            title: "Unsaved Changes",
            message: command.message,
            buttons: [
                {
                    text: "Yes",
                    isPrimary: true
                },
                {
                    text: "No",
                },
                {
                    text: "Cancel",
                }
            ]
        });

        const answer = response.value;
        const ync: YesNoCancel = answer === "Yes" ? "Yes" : answer === "No" ? "No" : "Cancel";

        await this.ipcGateway.send(new ChannelInfo("Respond"), command.id, ync);
    }

    private async requestScriptSavePath(command: RequestScriptSavePathCommand) {
        const newName = prompt("Script name:", command.scriptName);

        await this.ipcGateway.send(new ChannelInfo("Respond"), command.id, newName || null);
    }
}
