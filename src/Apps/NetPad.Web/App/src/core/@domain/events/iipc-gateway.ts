import {DI, IDisposable} from "aurelia";

export interface IIpcGateway {
    subscribe(channelName: string, callback: (message: any, channel: string) => void): IDisposable;
}

export const IIpcGateway = DI.createInterface<IIpcGateway>();
