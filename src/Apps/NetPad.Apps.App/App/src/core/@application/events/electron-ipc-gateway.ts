import {ipcRenderer, IpcRendererEvent} from "electron";
import {ILogger} from "aurelia";
import {IIpcGateway} from "@domain";
import {SubscriptionToken} from "@common";

/**
 * @deprecated This is still functional, but we are using SignalR IPC communication
 * for both Web and Electron versions of the app. This class will be deleted.
 */
export class ElectronIpcGateway implements IIpcGateway {
    private readonly logger: ILogger;
    private connectedChannels = new Set<string>();

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(ElectronIpcGateway));
    }

    public start(): Promise<void> {
        return Promise.resolve(undefined);
    }

    public subscribe(channelName: string, callback: (message: unknown, channel: string) => void): SubscriptionToken {
        const handler = (event: IpcRendererEvent, ...args: unknown[]) => {
            this.logger.debug(`ElectronIpcGateway: Got server message`, event, ...args);
            const json = args.length > 0 ? args[0] as string : null;
            callback(!json ? null : JSON.parse(json), channelName);
        };

        ipcRenderer.on(channelName, handler);
        this.connectedChannels.add(channelName);

        return new SubscriptionToken(() => {
            ipcRenderer.off(channelName, handler);
            this.connectedChannels.delete(channelName);
        });
    }

    public send<TResult>(channelName: string, ...params: unknown[]): Promise<TResult> {
        throw new Error("Platform not supported");
    }

    public dispose(): void {
        for (const channel of this.connectedChannels) {
            ipcRenderer.removeAllListeners(channel);
        }
    }
}
