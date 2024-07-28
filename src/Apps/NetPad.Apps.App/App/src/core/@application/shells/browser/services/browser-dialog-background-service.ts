import {WithDisposables} from "@common";
import {
    ChannelInfo,
    ConfirmSaveCommand,
    IBackgroundService,
    IEventBus,
    IIpcGateway,
    RequestNewScriptNameCommand,
    YesNoCancel
} from "@application";

/**
 * This is utilized for the Browser app, not the Electron app.
 * This enables opening specific dialog windows when running the browser app.
 */
export class BrowserDialogBackgroundService extends WithDisposables implements IBackgroundService {
    constructor(@IEventBus readonly eventBus: IEventBus,
                @IIpcGateway readonly ipcGateway: IIpcGateway
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
            this.eventBus.subscribeToServer(RequestNewScriptNameCommand, async msg => {
                await this.requestNewScriptName(msg);
            })
        );

        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.dispose();
    }

    private async confirmSave(command: ConfirmSaveCommand) {
        const ync: YesNoCancel = confirm(command.message) ? "Yes" : "No";

        await this.ipcGateway.send(new ChannelInfo("Respond"), command.id, ync);
    }

    private async requestNewScriptName(command: RequestNewScriptNameCommand) {
        const newName = prompt("Name:", command.currentScriptName);

        await this.ipcGateway.send(new ChannelInfo("Respond"), command.id, newName || null);
    }
}
