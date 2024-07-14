import {ipcRenderer, IpcRendererEvent} from "electron";
import {ILogger} from "aurelia";
import {ChannelInfo, IIpcGateway, IpcMessageBatch} from "@application";
import {IDisposable, SubscriptionToken} from "@common";

/**
 * An abstraction of the Electron IpcRenderer. Sends and receives messages from the Electron main process.
 */
export class ElectronIpcGateway implements IIpcGateway {
    private readonly logger: ILogger;
    private connectedChannels = new Set<string>();
    private callbacks: { channel: ChannelInfo, electronCallback: (event: Electron.IpcRendererEvent | undefined, ...args: unknown[]) => void }[] = [];

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(ElectronIpcGateway));

        this.subscribe<IpcMessageBatch>(new ChannelInfo(IpcMessageBatch), combinedMessage =>
        {
            for (const message of combinedMessage.messages) {
                const callbacks = this.callbacks.filter(x => x.channel.name === message.messageType);

                if (!callbacks.length) {
                    this.logger.debug(`Received a message on channel ${message.messageType} within a message batch but it did not have any handlers`);
                    continue;
                }

                for (const callback of callbacks) {
                    callback.electronCallback(undefined, message.message);
                }
            }
        });
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
        const handler = (event: IpcRendererEvent | undefined, ...args: unknown[]) => {
            this.logger.debug(`Got Electron IPC message from Main process`, channel.name, event, ...args);

            let firstArg = args.length > 0 ? args[0] as unknown : null;

            // IPC messages we get from .NET over Electron.NET get sent as an item in an array
            if (Array.isArray(firstArg)) {
                firstArg = firstArg[0];
            }

            let value = typeof firstArg === "string" ? JSON.parse(firstArg) : firstArg;

            if (channel.messageType && value && typeof value === "object") {
                value = Object.assign(new channel.messageType(), value);
            }

            callback(value, channel);
        };

        ipcRenderer.on(channel.name, handler);

        this.connectedChannels.add(channel.name);
        const cachedCallback = {channel: channel, electronCallback: handler};
        this.callbacks.push(cachedCallback);

        return new SubscriptionToken(() => {
            ipcRenderer.off(channel.name, handler);
            this.connectedChannels.delete(channel.name);
            this.callbacks.splice(this.callbacks.indexOf(cachedCallback), 1);
        });
    }

    public send<TResult>(channel: ChannelInfo, ...params: unknown[]): Promise<TResult> {
        return ipcRenderer.invoke(channel.name, ...params);
    }

    public dispose(): void {
        this.stop();
    }
}
