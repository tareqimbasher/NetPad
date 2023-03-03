import {HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel as SignalRLogLevel} from "@microsoft/signalr";
import {ILogger, PLATFORM} from "aurelia";
import {IIpcGateway} from "@domain";
import {SubscriptionToken} from "@common";

export class SignalRIpcGateway implements IIpcGateway {
    private readonly logger: ILogger;
    private readonly signalrHandlers: Map<string, (...args: unknown[]) => void>;
    private readonly callbacks: Map<string, ((message: unknown, channel: string) => void)[]>;
    private connection: HubConnection
    private disposed = false;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(SignalRIpcGateway));
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
            .configureLogging(SignalRLogLevel.Information)
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

    public subscribe(channelName: string, callback: (message: unknown, channel: string) => void): SubscriptionToken {
        this.addCallback(channelName, callback);

        return new SubscriptionToken(() => {
            this.removeCallback(channelName, callback);
        });
    }

    public async send<TResult>(channelName: string, ...params: unknown[]): Promise<TResult> {
        return await this.connection.invoke(channelName, ...params);
    }

    private addCallback(channelName: string, callback: (message: unknown, channel: string) => void) {
        this.ensureOrRegisterNewSignalrHandler(channelName);

        let channelCallbacks = this.callbacks.get(channelName);

        if (!channelCallbacks) {
            channelCallbacks = [];
            this.callbacks.set(channelName, channelCallbacks);
        }

        channelCallbacks.push(callback);
    }

    private removeCallback(channelName: string, callback: (message: unknown, channel: string) => void) {
        const channelCallbacks = this.callbacks.get(channelName);

        if (!!channelCallbacks && channelCallbacks.length > 0) {
            const ix = channelCallbacks.indexOf(callback);
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
                callback(arg, channelName);
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

        this.removeAllCallbacks();

        // TODO Stop using IDisposable from aurelia package and copy it to be part of app code
        //  also create an interface IAsyncDisposable and use it here
        this.connection.stop();
    }
}
