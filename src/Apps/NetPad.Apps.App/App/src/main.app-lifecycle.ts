import * as appTasks from "./main.tasks";
import {IContainer, ILogger} from "aurelia";
import {IIpcGateway} from "@domain";

/**
 * Actions that run at specific points in the app's lifecycle
 */
export class AppLifeCycle {
    constructor(private readonly logger: ILogger) {
    }

    public async creating(container: IContainer): Promise<void> {
        this.logger.debug("App being created...");

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
    }

    public async deactivating(container: IContainer): Promise<void> {
        this.logger.debug("App deactivating...");

        await appTasks.stopBackgroundServices(container);
        container.get(IIpcGateway).dispose();
    }

    public async deactivated(container: IContainer): Promise<void> {
        this.logger.debug("App deactivated");
    }
}
