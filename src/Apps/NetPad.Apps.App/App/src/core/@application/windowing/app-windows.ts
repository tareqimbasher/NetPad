import {IDisposable} from "@common";
import {ILogger} from "aurelia";

class AppWindow {
    constructor(public readonly name: string) {
    }
}

export interface IAppWindowEvent {
    windowName: string;
    type: "activated" | "deactivated";
}

export class AppWindows implements IDisposable {
    public items: AppWindow[] = [];
    private readonly logger: ILogger;
    private broadcastChannel: BroadcastChannel;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(AppWindows));

        this.broadcastChannel = new BroadcastChannel("windows");
        this.broadcastChannel.onmessage = (msg) => {
            if (!msg.data) {
                return;
            }

            const event = msg.data as IAppWindowEvent;

            this.logger.debug(`AppWindow event from window: '${event.windowName}'. Event: '${event.type}'`);

            if (event.type === "activated") {
                this.items.push(new AppWindow(event.windowName));
            } else if (event.type === "deactivated") {
                const ixApp = this.items.findIndex(x => x.name === event.windowName);
                if (ixApp >= 0) {
                    this.items.splice(ixApp, 1);
                }
            } else {
                this.logger.error(`App window event is not handled: ${event.type}`);
            }
        }
    }

    public dispose(): void {
        this.broadcastChannel.close();
    }
}
