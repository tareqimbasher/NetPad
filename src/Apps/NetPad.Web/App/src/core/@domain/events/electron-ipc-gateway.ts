import {IIpcGateway, SubscriptionToken} from "@domain";
import {ipcRenderer, IpcRendererEvent} from "electron";
import {ILogger} from "aurelia";

export class ElectronIpcGateway implements IIpcGateway {
    constructor(@ILogger readonly logger: ILogger) {
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
