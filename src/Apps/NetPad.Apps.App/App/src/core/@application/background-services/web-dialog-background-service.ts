import {IBackgroundService} from "@common";
import {IDisposable} from "aurelia";
import {ConfirmSaveCommand, IEventBus, IIpcGateway, RequestNewScriptNameCommand, YesNoCancel} from "@domain";

/**
 * This is utilized for the Web app, not the Electron app.
 * This enables opening specific dialog windows when running the web app.
 */
export class WebDialogBackgroundService implements IBackgroundService {
    private disposables: IDisposable[] = [];

    constructor(@IEventBus readonly eventBus: IEventBus,
                @IIpcGateway readonly ipcGateway: IIpcGateway
    ) {
    }

    public start(): Promise<void> {
        this.disposables.push(
            this.eventBus.subscribeToServer(ConfirmSaveCommand, async msg => {
                await this.confirmSave(msg);
            })
        );

        this.disposables.push(
            this.eventBus.subscribeToServer(RequestNewScriptNameCommand, async msg => {
                await this.requestNewScriptName(msg);
            })
        );

        return Promise.resolve(undefined);
    }

    public stop(): void {
        this.disposables.forEach(d => d.dispose());
    }

    private async confirmSave(command: ConfirmSaveCommand) {
        const ync: YesNoCancel = confirm(command.message) ? "Yes" : "No";

        await this.ipcGateway.send("Respond", command.id, ync);
    }

    private async requestNewScriptName(command: RequestNewScriptNameCommand) {
        const newName = prompt("Name:", command.currentScriptName);

        await this.ipcGateway.send("Respond", command.id, newName || null);
    }
}
