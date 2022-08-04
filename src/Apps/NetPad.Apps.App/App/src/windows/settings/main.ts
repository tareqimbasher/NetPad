import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {IWindowBootstrapper} from "@application";
import {AppService, IAppService} from "@domain";

export class Bootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(Registration.transient(IAppService, AppService));
    }
}
