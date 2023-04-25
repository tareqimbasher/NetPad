import * as appTasks from "./main.tasks";
import {IContainer, ILogger} from "aurelia";
import {IIpcGateway} from "@domain";
import {IAppLifecycleEvent} from "@application/windows/app-windows";

/**
 * Actions that run at specific points in the app's lifecycle
 */
export class AppLifeCycle {
    constructor(private readonly logger: ILogger) {
    }

    public async creating(container: IContainer): Promise<void> {
        this.logger.debug("App being created with options:", container.get(URLSearchParams).toString());

        await container.get(IIpcGateway).start();
    }

    public async hydrating(container: IContainer): Promise<void> {
        this.logger.debug("App hydrating...");
    }

    public async hydrated(container: IContainer): Promise<void> {
        this.logger.debug("App hydrated");
    }

    public async activating(container: IContainer): Promise<void> {
        this.logger.debug("App activating...");

        await appTasks.configureFetchClient(container);
        await appTasks.startBackgroundServices(container);
    }

    public async activated(container: IContainer): Promise<void> {
        this.logger.debug("App activated");

        const bc = new BroadcastChannel("windows");
        bc.postMessage(<IAppLifecycleEvent>{
            type: "app-activated",
            appName: container.get(URLSearchParams).get("win")
        });
        bc.close();
    }

    public async deactivating(container: IContainer): Promise<void> {
        this.logger.debug("App deactivating...");

        await appTasks.stopBackgroundServices(container);
        container.get(IIpcGateway).dispose();
    }

    public async deactivated(container: IContainer): Promise<void> {
        this.logger.debug("App deactivated");

        const bc = new BroadcastChannel("windows");
        bc.postMessage(<IAppLifecycleEvent>{
            type: "app-deactivated",
            appName: container.get(URLSearchParams).get("win")
        });
        bc.close();
    }
}
