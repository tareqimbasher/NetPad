import {Constructable, EventAggregator, ILogger} from "aurelia";
import {IEventBus, IIpcGateway} from "@domain";
import {IDisposable} from "@common";

export class EventBus extends EventAggregator implements IEventBus {
    private readonly logger: ILogger;

    constructor(@IIpcGateway readonly ipcGateway: IIpcGateway, @ILogger logger: ILogger) {
        super();
        this.logger = logger.scopeTo(nameof(EventBus));
    }

    public subscribeToServer<TMessage extends Constructable>(
        channel: string | Constructable,
        callback: (message: InstanceType<TMessage>, channel: string) => void): IDisposable {
        const channelName = typeof channel === 'string' ? channel : channel.name;

        const proxyCallback = (message: InstanceType<TMessage>, channel: string) => {
            try {
                callback(message, channel);
            } catch (ex) {
                this.logger.error(`An unhandled error occurred while processing a server-pushed message callback on channel: ${channel}`, ex, callback);
            }
        };

        return this.ipcGateway.subscribe(channelName, proxyCallback);
    }
}

