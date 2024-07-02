import {DI} from "aurelia";
import {IDisposable} from "@common";
import {ChannelInfo} from "@application";

/**
 * Provides an interface to interact with an external process.
 */
export interface IIpcGateway extends IDisposable {
    start(): Promise<void>;

    stop(): Promise<void>;

    subscribe<TMessage>(channel: ChannelInfo, callback: (message: TMessage, channel: ChannelInfo) => void): IDisposable;

    send<TResult>(channel: ChannelInfo, ...params: unknown[]): Promise<TResult>;
}

export const IIpcGateway = DI.createInterface<IIpcGateway>();
