import {Constructable, EventAggregator, ILogger} from "aurelia";
import {ChannelInfo, IEventBus, IIpcGateway} from "@application";
import {IDisposable} from "@common";

/**
 * The main event message bus for the application.
 * Uses the base implementation of the Aurelia event aggregator.
 */
export class EventBus extends EventAggregator implements IEventBus {
    private readonly logger: ILogger;

    constructor(@IIpcGateway readonly ipcGateway: IIpcGateway, @ILogger logger: ILogger) {
        super();
        this.logger = logger.scopeTo(nameof(EventBus));
    }

    public subscribeToServer<TMessage>(
        channelTypeOrName: Constructable<TMessage> | string,
        callback: (message: TMessage, channel: ChannelInfo) => void): IDisposable {

        const channel = new ChannelInfo(channelTypeOrName as Constructable);

        const proxyCallback = (message: TMessage, proxiedChannel: ChannelInfo) => {
            try {
                callback(message, proxiedChannel);
            } catch (ex) {
                this.logger.error(`An unhandled error occurred while processing a server-pushed message callback on channel: ${channel.name}`, ex, callback);
            }
        };

        return this.ipcGateway.subscribe(channel, proxyCallback);
    }
}

