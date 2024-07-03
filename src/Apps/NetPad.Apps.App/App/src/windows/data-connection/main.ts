import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {IDataConnectionService, IWindowBootstrapper} from "@application";
import {DataConnectionService} from "@application/data-connections/data-connection-service";

export class Bootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.transient(IDataConnectionService, DataConnectionService)
        );
    }
}
