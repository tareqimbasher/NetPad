import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {IBackgroundService, IDataConnectionService, IWindowBootstrapper, IWindowDestinations} from "@application";
import {DataConnectionService} from "@application/data-connections/data-connection-service";
import {WindowDestinationBackgroundService} from "@application/windowing/window-destination-background-service";
import {ConnectionWindowDestinations} from "./connection-window-destinations";

export class DataConnectionWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.transient(IDataConnectionService, DataConnectionService),
            Registration.singleton(IWindowDestinations, ConnectionWindowDestinations),
            Registration.singleton(IBackgroundService, WindowDestinationBackgroundService),
        );
    }
}
