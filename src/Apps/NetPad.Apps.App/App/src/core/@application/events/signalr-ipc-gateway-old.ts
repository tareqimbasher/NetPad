import {HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel as SignalRLogLevel} from "@microsoft/signalr";
import {ILogger, PLATFORM} from "aurelia";
import {ChannelInfo, IIpcGateway} from "@domain";
import {IDisposable, SubscriptionToken} from "@common";

/**
 * OBSOLETE keeping it around for a little bit before deleting as it included some minor mods from the true old SignalRIpcGateway.
 *
 * The OLD main mode of communication with the .NET backend app over SignalR. All interaction with the
 * backend's SignalR connection should occur through this gateway.
 */
export class SignalRIpcGatewayOld implements IIpcGateway {
    private readonly logger: ILogger;
    private readonly signalrHandlers: Map<string, (...args: unknown[]) => void>;
    private readonly callbacks: Map<string, ((message: unknown, channel: string) => void)[]>;
    private connection: HubConnection
    private disposed = false;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(SignalRIpcGatewayOld));
        this.signalrHandlers = new Map<string, (...args: unknown[]) => void>();
        this.callbacks = new Map<string, ((message: unknown, channel: string) => void)[]>();
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

        await this.startConnection();
    }

    public async stop(): Promise<void> {
        this.removeAllCallbacks();
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
        // TODO Use ChannelInfo keys instead of strings
        // This wrapper was added to accomodate the new IIpcGateway interface change. This class uses strings
        // for keys, the rest of the event messaging system use a "ChannelInfo" object as the channel key.
        const wrappedCallback = (message: TMessage, channelName: string) => callback(message as TMessage, channel);

        this.addCallback(channel.name, wrappedCallback);

        return new SubscriptionToken(() => {
            this.removeCallback(channel.name, wrappedCallback);
        });
    }

    public async send<TResult>(channel: ChannelInfo, ...params: unknown[]): Promise<TResult> {
        return await this.connection.invoke(channel.name, ...params);
    }

    private addCallback<TMessage>(channelName: string, callback: (message: TMessage, channel: string) => void) {
        this.ensureOrRegisterNewSignalrHandler(channelName);

        let channelCallbacks = this.callbacks.get(channelName);

        if (!channelCallbacks) {
            channelCallbacks = [];
            this.callbacks.set(channelName, channelCallbacks);
        }

        channelCallbacks.push(callback as (message: unknown, channel: string) => void);
    }

    private removeCallback<TMessage>(channelName: string, callback: (message: TMessage, channel: string) => void) {
        const channelCallbacks = this.callbacks.get(channelName);

        if (!!channelCallbacks && channelCallbacks.length > 0) {
            const ix = channelCallbacks.indexOf(callback as (message: unknown, channel: string) => void);
            if (ix >= 0) {
                channelCallbacks.splice(ix, 1);
            }
        }

        if (!channelCallbacks || channelCallbacks.length === 0) {
            this.callbacks.delete(channelName);
            this.removeSignalrHandlerIfApplicable(channelName);
        }
    }

    private removeAllCallbacks() {
        for (const [channelName, callbacks] of this.callbacks) {
            while (callbacks.length > 0) {
                this.removeCallback(channelName, callbacks[0]);
            }
        }
    }

    private ensureOrRegisterNewSignalrHandler(channelName: string): void {
        if (this.signalrHandlers.has(channelName)) return;

        const signalrHandler = (...args: unknown[]) => {
            this.logger.debug(`Received push message of type ${channelName}`, ...args);

            const channelCallbacks = this.callbacks.get(channelName);
            if (!channelCallbacks) {
                this.logger.warn(`Received server push message of type ${channelName} that does not have any callbacks`, ...args)
                return;
            }

            const arg = args.length > 0 ? args[0] : null;

            for (const callback of channelCallbacks) {
                try {
                    callback(arg, channelName);
                } catch (ex) {
                    this.logger.error(`An unhandled error occurred while processing an IPC message callback on channel: ${channelName}`, ex, callback);
                }
            }
        };

        this.connection.on(channelName, signalrHandler);
        this.signalrHandlers.set(channelName, signalrHandler);
    }

    private removeSignalrHandlerIfApplicable(channelName: string) {
        const channelCallbacks = this.callbacks.get(channelName);

        if (!!channelCallbacks && channelCallbacks.length > 0) {
            return;
        }

        this.connection.off(channelName);
        this.signalrHandlers.delete(channelName);
    }

    public dispose(): void {
        if (this.disposed) return;
        this.disposed = true;

        // TODO Create an interface IAsyncDisposable and use it here
        this.stop();
    }
}
