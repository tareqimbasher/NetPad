import {ipcRenderer, IpcRendererEvent} from "electron";
import {ILogger} from "aurelia";
import {IIpcGateway} from "@domain";
import {SubscriptionToken} from "@application";

export class ElectronIpcGateway implements IIpcGateway {
    private readonly logger: ILogger;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(ElectronIpcGateway));
    }

    public subscribe(channelName: string, callback: (message: any, channel: string) => void): SubscriptionToken {
        const handler = (event: IpcRendererEvent, ...args: any[]) => {
            this.logger.debug(`ElectronIpcGateway: Got server message`, event, ...args);
            const json = args.length > 0 ? args[0] : null;
            callback(!json ? null : JSON.parse(json), channelName);
        };

        ipcRenderer.on(channelName, handler);
        return new SubscriptionToken(() => ipcRenderer.off(channelName, handler));
    }
}
