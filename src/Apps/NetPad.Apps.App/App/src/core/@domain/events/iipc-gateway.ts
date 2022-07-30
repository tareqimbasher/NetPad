import {DI, IDisposable} from "aurelia";

export interface IIpcGateway {
    subscribe(channelName: string, callback: (message: unknown, channel: string) => void): IDisposable;
    send<TResult>(channelName: string, ...params: unknown[]): Promise<TResult>;
}

export const IIpcGateway = DI.createInterface<IIpcGateway>();
