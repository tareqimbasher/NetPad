import {ipcRenderer, IpcRendererEvent} from "electron";
import {ILogger} from "aurelia";
import {ChannelInfo, IIpcGateway} from "@domain";
import {IDisposable, SubscriptionToken} from "@common";

/**
 * The main mode of communication with the Electron main process. All interaction with the
 * Electron main process should occur through this gateway.
 */
export class ElectronIpcGateway implements IIpcGateway {
    private readonly logger: ILogger;
    private connectedChannels = new Set<string>();

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(ElectronIpcGateway));
    }

    public start(): Promise<void> {
        return Promise.resolve();
    }

    public stop(): Promise<void> {
        for (const channel of this.connectedChannels) {
            ipcRenderer.removeAllListeners(channel);
        }

        return Promise.resolve();
    }

    public subscribe<TMessage>(channel: ChannelInfo, callback: (message: TMessage, channel: ChannelInfo) => void): IDisposable {
        const handler = (event: IpcRendererEvent, ...args: unknown[]) => {
            this.logger.debug(`Got Electron IPC message from Main process`, channel.name, event, ...args);

            const json = args.length > 0 ? args[0] as unknown : null;
            let value = !json ? null : typeof json === "string" ? JSON.parse(json) : json;

            if (channel.messageType && typeof value === "object") {
                value = Object.assign(new channel.messageType(), value);
            }

            callback(value, channel);
        };

        ipcRenderer.on(channel.name, handler);
        this.connectedChannels.add(channel.name);

        return new SubscriptionToken(() => {
            ipcRenderer.off(channel.name, handler);
            this.connectedChannels.delete(channel.name);
        });
    }

    public send<TResult>(channel: ChannelInfo, ...params: unknown[]): Promise<TResult> {
        return ipcRenderer.invoke(channel.name, ...params);
    }

    public dispose(): void {
        this.stop();
    }
}
