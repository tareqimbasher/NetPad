import {EventAggregator, IDisposable} from "aurelia";
import {Constructable} from "@aurelia/kernel/src/interfaces";
import {IEventBus, IIpcGateway} from "@domain";

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

