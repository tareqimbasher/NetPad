import {DI, EventAggregator, IEventAggregator, IDisposable} from "aurelia";
import {ipcRenderer, IpcRendererEvent} from "electron";
import {Constructable} from "@aurelia/kernel/src/interfaces";

export interface IEventBus extends IEventAggregator {
    /**
     * Subscribes to a message sent by a remote party.
     * @param channel The channel to subscribe to.
     * @param callback The callback to execute when a message is received.
     */
    subscribeToServer<TMessage extends Constructable>(
        channel: string | TMessage,
        callback: (message: InstanceType<TMessage>, channel: string) => void): IDisposable;
}

export const IEventBus = DI.createInterface<IEventBus>();

export class EventBus extends EventAggregator implements IEventBus {
    public subscribeToServer<TMessage extends Constructable>(
        channel: string | Constructable,
        callback: (message: InstanceType<TMessage>, channel: string) => void): IDisposable {
        const channelName = typeof channel === 'string' ? channel : channel.name;

        const handler = (event: IpcRendererEvent, ...args: any[]) => {
            const json = args.length > 0 ? args[0] : null;
            callback(!json ? null : JSON.parse(json), channelName);
        };

        ipcRenderer.on(channelName, handler);
        return new SubscriptionToken(() => ipcRenderer.off(channelName, handler));
    }
}

class SubscriptionToken implements IDisposable {
    constructor(private readonly action: () => void) {
    }

    public dispose(): void {
        this.action();
    }
}
