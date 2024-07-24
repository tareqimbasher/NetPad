import {Aurelia} from "aurelia";
import {Window} from "./window";
import {IWindowBootstrapper} from "@application";

export class SettingsWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        // nothing to register
    }
}
