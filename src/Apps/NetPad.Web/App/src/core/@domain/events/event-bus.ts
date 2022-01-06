import {DI, EventAggregator, IDisposable, IEventAggregator} from "aurelia";
import {Constructable} from "@aurelia/kernel/src/interfaces";
import {IIpcGateway} from "@domain/events/iipc-gateway";

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
    constructor(@IIpcGateway readonly ipcGateway: IIpcGateway) {
        super();
    }

    public subscribeToServer<TMessage extends Constructable>(
        channel: string | Constructable,
        callback: (message: InstanceType<TMessage>, channel: string) => void): IDisposable {
        const channelName = typeof channel === 'string' ? channel : channel.name;

        return this.ipcGateway.subscribe(channelName, callback);
    }
}

