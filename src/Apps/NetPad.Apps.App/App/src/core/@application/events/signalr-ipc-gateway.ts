import {ILogger, PLATFORM} from "aurelia";
import {HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel as SignalRLogLevel} from "@microsoft/signalr";
import {ChannelInfo, IIpcGateway, IpcMessageBatch} from "@application";
import {IDisposable, SubscriptionToken} from "@common";

/**
 * The main mode of communication with the .NET backend app over SignalR. All interaction with the
 * backend's SignalR connection should occur through this gateway.
 */
export class SignalRIpcGateway implements IIpcGateway {
    private connection: HubConnection
    private disposed = false;
    private callbacks: { channel: ChannelInfo, signalrCallback: (...args: unknown[]) => void }[] = [];

    constructor(@ILogger private readonly logger: ILogger) {
        this.logger = logger.scopeTo(nameof(SignalRIpcGateway));
    }

    public async start(): Promise<void> {
        if (this.disposed) {
            throw new Error("Gateway is disposed");
        }

        if (this.connection) {
            throw new Error("Gateway has already started");
        }

        this.connection = new HubConnectionBuilder()
            .withUrl("/ipc-hub")
            .configureLogging({
                log: (logLevel: SignalRLogLevel, message: string) => {
                    if (logLevel === SignalRLogLevel.Warning && message.startsWith("No client method with the name"))
                        logLevel = SignalRLogLevel.Debug;

                    switch (logLevel) {
                        case SignalRLogLevel.Critical:
                        case SignalRLogLevel.Error:
                            this.logger.error(message);
                            break;
                        case SignalRLogLevel.Warning:
                            this.logger.warn(message);
                            break;
                        case SignalRLogLevel.Information:
                            this.logger.info(message);
                            break;
                        default:
                            this.logger.debug(message);
                            break;
                    }
                }
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    // If disposed or if we've been reconnecting for more than 20 seconds so far, stop reconnecting.
                    if (this.disposed || retryContext.elapsedMilliseconds > 20000) {
                        return null;
                    }

                    return retryContext.previousRetryCount < 3
                        ? 500
                        : 5000;
                }
            })
            .build();

        this.connection.onreconnecting(error => {
            this.logger.debug("Reconnecting", error);
        });

        this.connection.onreconnected(error => {
            this.logger.debug("Reconnected", error);
        });

        this.connection.onclose(error => {
            if (this.disposed) {
                this.logger.info("Connection was closed. Gateway is disposed, will not try to reconnect");
                return;
            }

            this.logger.warn("Connection was closed. Will try to reconnect in 2 seconds. Error: ", error);

            PLATFORM.setTimeout(() => {
                if (this.connection.state === HubConnectionState.Disconnected)
                    this.startConnection();
            }, 2000);
        });

        this.subscribe<IpcMessageBatch>(new ChannelInfo(IpcMessageBatch), combinedMessage =>
        {
            for (const message of combinedMessage.messages) {
                const callbacks = this.callbacks.filter(x => x.channel.name === message.messageType);

                if (!callbacks.length) {
                    this.logger.debug(`Received a message on channel ${message.messageType} within a message batch but it did not have any handlers`);
                    continue;
                }

                for (const callback of callbacks) {
                    callback.signalrCallback(message.message);
                }
            }
        });

        await this.startConnection();
    }

    public async stop(): Promise<void> {
        await this.connection.stop();
    }

    private async startConnection(): Promise<void> {
        if (this.disposed || this.connection.state === HubConnectionState.Connected)
            return;

        try {
            this.logger.debug("Starting connection...");
            await this.connection.start();

            if (this.connection.state === HubConnectionState.Disconnected)
                throw new Error("Connection did not start");

            this.logger.debug("Connected");
        } catch (ex) {
            this.logger.error("Failed to start connection. Will retry in 2 seconds...", ex);
            setTimeout(() => this.startConnection(), 2000);
        }
    }

    public subscribe<TMessage>(channel: ChannelInfo, callback: (message: TMessage, channel: ChannelInfo) => void): IDisposable {
        const signalrHandler = (...args: unknown[]) => {
            this.logger.debug(`Received push message of type ${channel.name}`, ...args);

            const arg = args.length > 0 ? args[0] as unknown : null;

            try {
                // TODO make TMessage nullable
                callback(arg as TMessage, channel);
            } catch (ex) {
                this.logger.error(`An unhandled error occurred while processing an IPC message callback on channel: ${channel.name}`, ex, callback);
            }
        };

        this.connection.on(channel.name, signalrHandler);

        const cachedCallback = {channel: channel, signalrCallback: signalrHandler};
        this.callbacks.push(cachedCallback);

        return new SubscriptionToken(() => {
            this.connection.off(channel.name, signalrHandler);
            this.callbacks.splice(this.callbacks.indexOf(cachedCallback), 1);
        });
    }

    public async send<TResult>(channel: ChannelInfo, ...params: unknown[]): Promise<TResult> {
        return await this.connection.invoke(channel.name, ...params);
    }

    public dispose(): void {
        if (this.disposed) return;
        this.disposed = true;

        // TODO Create an interface IAsyncDisposable and use it here
        this.stop();
    }
}
