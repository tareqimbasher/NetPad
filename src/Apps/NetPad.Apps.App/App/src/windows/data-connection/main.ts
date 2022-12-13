import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {IWindowBootstrapper} from "@application";
import {DataConnectionService, IDataConnectionService} from "@domain";

export class Bootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.transient(IDataConnectionService, DataConnectionService)
        );
    }
}
