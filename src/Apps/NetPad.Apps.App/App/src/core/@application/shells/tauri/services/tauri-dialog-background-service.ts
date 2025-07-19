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
import {save} from '@tauri-apps/plugin-dialog'
import {listen} from '@tauri-apps/api/event';

/**
 * This is utilized for the Tauri app
 * This enables opening specific dialog windows when running the tauri app.
 */
export class TauriDialogBackgroundService extends WithDisposables implements IBackgroundService {
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

        listen<string>('download-finished', (event) => {
            const fileSavePath = event.payload;

            if (fileSavePath) {
                alert("File successfully saved to:\n" + fileSavePath);
            } else {
                alert("File successfully saved to your Downloads folder.")
            }

        }).then(unlisten => this.addDisposable(unlisten));

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

        await this.ipcGateway.send(new ChannelInfo("Respond"), command.id, path || null);
    }
}
