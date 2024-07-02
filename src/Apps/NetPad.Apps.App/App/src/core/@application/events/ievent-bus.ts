import {DI, IEventAggregator} from "aurelia";
import {Constructable} from "@aurelia/kernel/src/interfaces";
import {IDisposable} from "@common";
import {ChannelInfo} from "./channel-info";

/**
 * An event message bus.
 */
export interface IEventBus extends IEventAggregator {
    /**
     * Subscribes to a message sent by a remote party. ie. the sender is not the current application.
     * @param channelTypeOrName The channel to subscribe to.
     * @param callback The callback to execute when a message is received.
     */
    subscribeToServer<TMessage>(
        channelTypeOrName: Constructable<TMessage> | string,
        callback: (message: TMessage, channel: ChannelInfo) => void): IDisposable;
}

export const IEventBus = DI.createInterface<IEventBus>();
