import {DI} from "aurelia";
import {IDisposable} from "@common";

export interface IIpcGateway extends IDisposable {
    start(): Promise<void>;

    subscribe<TMessage>(channelName: string, callback: (message: TMessage, channel: string) => void): IDisposable;

    send<TResult>(channelName: string, ...params: unknown[]): Promise<TResult>;
}

export const IIpcGateway = DI.createInterface<IIpcGateway>();
