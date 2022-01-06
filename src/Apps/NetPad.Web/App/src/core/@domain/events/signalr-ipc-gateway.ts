import {IIpcGateway, SubscriptionToken} from "@domain";
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {ILogger} from "aurelia";

export class SignalRIpcGateway implements IIpcGateway {
    private connection: HubConnection

    constructor(@ILogger readonly logger: ILogger) {
        this.connection = new HubConnectionBuilder()
            .withUrl("/ipchub")
            .build();

        this.connection.onclose(error => {
            this.logger.warn("SignalR IPC Gateway connection was closed. Will try to reconnect in 2 seconds", error);
            setTimeout(() => {
                this.connection.start();
            }, 2000);
        });

        this.connection.start();
    }

    public subscribe(channelName: string, callback: (message: any, channel: string) => void): SubscriptionToken {
        const handler = (...args: any[]) => {
            this.logger.debug(`SignalRIpcGateway: Got server message`, ...args);
            const arg = args.length > 0 ? args[0] : null;
            callback(arg, channelName);
        };

        this.connection.on(channelName, handler);
        return new SubscriptionToken(() => this.connection.off(channelName, handler));
    }
}
