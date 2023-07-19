import {IDisposable} from "@common";
import {ILogger} from "aurelia";

class AppWindow {
    constructor(public readonly name) {
    }
}

export interface IAppLifecycleEvent {
    type: "app-activated" | "app-deactivated";
    appName: string;
}

export class AppWindows implements IDisposable {
    public items: AppWindow[] = [];
    private readonly logger: ILogger;
    private broadcastChannel: BroadcastChannel;

    constructor(@ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(AppWindows));

        this.broadcastChannel = new BroadcastChannel("windows");
        this.broadcastChannel.onmessage = (msg) => {
            if (!msg.data) return;
            const event = msg.data as IAppLifecycleEvent;

            if (event.type === "app-activated") {
                this.logger.debug("App activated: ", event.appName);
                this.items.push(new AppWindow(event.appName));
            } else if (event.type === "app-deactivated") {
                this.logger.debug("App deactivated: ", event.appName);
                const ixApp = this.items.findIndex(x => x.name === event.appName);
                if (ixApp >= 0) this.items.splice(ixApp, 1);
            }
        }
    }

    public dispose(): void {
        this.broadcastChannel.close();
    }
}
