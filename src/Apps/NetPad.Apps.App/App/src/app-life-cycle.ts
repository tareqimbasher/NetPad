import * as appActions from "./app-actions";
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
    constructor(@ILogger private readonly logger: ILogger,
                @IEventBus private readonly eventBus: IEventBus,
                @IContainer private readonly container: IContainer) {
        this.logger = this.logger.scopeTo(nameof(AppLifeCycle));
    }

    public async creating(): Promise<void> {
        this.logger.debug("App being created with options:", this.container.get(URLSearchParams).toString());
        this.eventBus.publish(new AppCreatingEvent());

        await this.container.get(IIpcGateway).start();
    }

    public async hydrating(): Promise<void> {
        this.logger.debug("App hydrating...");
    }

    public async hydrated(): Promise<void> {
        this.logger.debug("App hydrated");
        this.eventBus.publish(new AppCreatedEvent());
    }

    public async activating(): Promise<void> {
        this.logger.debug("App activating...");

        appActions.configureFetchClient(this.container);
        await appActions.startBackgroundServices(this.container);

        this.eventBus.publish(new AppActivatingEvent());
    }

    public async activated(): Promise<void> {
        this.logger.debug("App activated");

        const bc = new BroadcastChannel("windows");
        bc.postMessage(<IAppLifecycleEvent>{
            type: "app-activated",
            appName: this.container.get(URLSearchParams).get("win")
        });
        bc.close();

        this.eventBus.publish(new AppActivatedEvent());
    }

    public async deactivating(): Promise<void> {
        this.logger.debug("App deactivating...");
        this.eventBus.publish(new AppDeactivatingEvent());

        await appActions.stopBackgroundServices(this.container);

        const ipcGateways = this.container.getAll(IIpcGateway);
        for (const ipcGateway of ipcGateways) {
            await ipcGateway.stop();
            ipcGateway.dispose();
        }
    }

    public async deactivated(): Promise<void> {
        this.logger.debug("App deactivated");

        const bc = new BroadcastChannel("windows");
        bc.postMessage(<IAppLifecycleEvent>{
            type: "app-deactivated",
            appName: this.container.get(URLSearchParams).get("win")
        });
        bc.close();

        this.eventBus.publish(new AppDeactivatedEvent());
    }
}
