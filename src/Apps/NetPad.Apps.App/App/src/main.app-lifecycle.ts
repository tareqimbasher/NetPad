import * as appTasks from "./main.tasks";
import {IContainer, ILogger} from "aurelia";
import {
    AppActivatedEvent,
    AppActivatingEvent,
    AppCreatedEvent,
    AppCreatingEvent,
    AppDeactivatedEvent,
    AppDeactivatingEvent,
    IEventBus,
    IIpcGateway
} from "@application";
import {IAppLifecycleEvent} from "@application/windows/app-windows";

/**
 * Actions that run at specific points in the app's lifecycle
 */
export class AppLifeCycle {
    constructor(private readonly logger: ILogger,
                private readonly eventBus: IEventBus) {
    }

    public async creating(container: IContainer): Promise<void> {
        this.logger.debug("App being created with options:", container.get(URLSearchParams).toString());
        this.eventBus.publish(new AppCreatingEvent());

        await container.get(IIpcGateway).start();
    }

    public async hydrating(container: IContainer): Promise<void> {
        this.logger.debug("App hydrating...");
    }

    public async hydrated(container: IContainer): Promise<void> {
        this.logger.debug("App hydrated");
        this.eventBus.publish(new AppCreatedEvent());
    }

    public async activating(container: IContainer): Promise<void> {
        this.logger.debug("App activating...");

        await appTasks.configureFetchClient(container);
        await appTasks.startBackgroundServices(container);

        this.eventBus.publish(new AppActivatingEvent());
    }

    public async activated(container: IContainer): Promise<void> {
        this.logger.debug("App activated");

        const bc = new BroadcastChannel("windows");
        bc.postMessage(<IAppLifecycleEvent>{
            type: "app-activated",
            appName: container.get(URLSearchParams).get("win")
        });
        bc.close();

        this.eventBus.publish(new AppActivatedEvent());
    }

    public async deactivating(container: IContainer): Promise<void> {
        this.logger.debug("App deactivating...");
        this.eventBus.publish(new AppDeactivatingEvent());

        await appTasks.stopBackgroundServices(container);

        const ipcGateways = container.getAll(IIpcGateway);
        for (const ipcGateway of ipcGateways) {
            await ipcGateway.stop();
            ipcGateway.dispose();
        }
    }

    public async deactivated(container: IContainer): Promise<void> {
        this.logger.debug("App deactivated");

        const bc = new BroadcastChannel("windows");
        bc.postMessage(<IAppLifecycleEvent>{
            type: "app-deactivated",
            appName: container.get(URLSearchParams).get("win")
        });
        bc.close();

        this.eventBus.publish(new AppDeactivatedEvent());
    }
}
