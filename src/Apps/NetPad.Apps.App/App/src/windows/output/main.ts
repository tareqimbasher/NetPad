import {IWindowBootstrapper} from "@application";
import {Window} from "./window";
import {Aurelia} from "aurelia";

export class Bootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        // nothing to register
    }
}
