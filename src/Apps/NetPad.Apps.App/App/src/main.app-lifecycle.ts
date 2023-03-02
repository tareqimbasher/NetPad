import * as appTasks from "./main.tasks";
import {IContainer, ILogger} from "aurelia";
import {IIpcGateway} from "@domain";

export class AppLifeCycle {
    constructor(private readonly logger: ILogger) {
    }

    public async beforeCreate(container: IContainer): Promise<void> {
        this.logger.debug("Creating app...");

        await container.get(IIpcGateway).start();
    }

    public async hydrating(container: IContainer): Promise<void> {
        this.logger.debug("Hydrating app...");
    }

    public async hydrated(container: IContainer): Promise<void> {
        this.logger.debug("App hydrated");
    }

    public async beforeActivate(container: IContainer): Promise<void> {
        this.logger.debug("App activating...");

        await appTasks.configureFetchClient(container);
        await appTasks.startBackgroundServices(container);
    }

    public async afterActivate(container: IContainer): Promise<void> {
        this.logger.debug("App activated");
    }

    public async beforeDeactivate(container: IContainer): Promise<void> {
        this.logger.debug("App deactivating...");

        await appTasks.stopBackgroundServices(container);
        container.get(IIpcGateway).dispose();
    }

    public async afterDeactivate(container: IContainer): Promise<void> {
        this.logger.debug("App deactivated");
    }
}
