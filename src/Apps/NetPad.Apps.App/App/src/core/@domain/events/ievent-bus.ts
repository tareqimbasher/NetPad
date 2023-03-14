import {DI, IEventAggregator} from "aurelia";
import {Constructable} from "@aurelia/kernel/src/interfaces";
import {IDisposable} from "@common";

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
