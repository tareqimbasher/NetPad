import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {IPackageService, IWindowBootstrapper} from "@application";
import {PackageService} from "@application/packages/package-service";

export class SettingsWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPackageService, PackageService),
        );
    }
}
