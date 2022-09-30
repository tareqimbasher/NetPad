import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {ILogger, PLATFORM} from "aurelia";
import {IIpcGateway} from "@domain";
import {SubscriptionToken} from "@common";

export class SignalRIpcGateway implements IIpcGateway {
    private readonly connection: HubConnection
    private readonly logger: ILogger;
    private readonly signalrHandlers: Map<string, (...args: unknown[]) => void>;
    private readonly callbacks: Map<string, ((message: unknown, channel: string) => void)[]>;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(SignalRIpcGateway));
        this.signalrHandlers = new Map<string, (...args: unknown[]) => void>();
        this.callbacks = new Map<string, ((message: unknown, channel: string) => void)[]>();

        this.connection = new HubConnectionBuilder()
            .withUrl("/ipc-hub")
            .build();

        this.connection.onclose(error => {
            this.logger.warn("SignalR IPC Gateway connection was closed. Will try to reconnect in 2 seconds", error);
            PLATFORM.setTimeout(() => this.connection.start(), 2000);
        });

        this.connection.start();
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
        let channelCallbacks = this.callbacks.get(channelName);

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
}
