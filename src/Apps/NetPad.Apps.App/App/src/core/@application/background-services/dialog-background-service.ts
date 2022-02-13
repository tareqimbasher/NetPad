import {IBackgroundService} from "@common";
import {IDisposable} from "aurelia";
import {IEventBus, ConfirmSaveCommand, IIpcGateway, YesNoCancel} from "@domain";

export class DialogBackgroundService implements IBackgroundService {
    private disposables: IDisposable[] = [];

    constructor(@IEventBus readonly eventBus: IEventBus,
                @IIpcGateway readonly ipcGateway: IIpcGateway
    ) {
    }

    public start(): Promise<void> {
        this.disposables.push(
            this.eventBus.subscribeToServer(ConfirmSaveCommand, async msg => { await this.confirmSave(msg); })
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
}
