import {DI, IDisposable} from "aurelia";

export interface IIpcGateway {
    subscribe(channelName: string, callback: (message: any, channel: string) => void): IDisposable;
    send<TResult>(channelName: string, ...params: any[]): Promise<TResult>;
}

export const IIpcGateway = DI.createInterface<IIpcGateway>();
