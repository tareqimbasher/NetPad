import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {IPackageService, IWindowBootstrapper, IWindowDestinations} from "@application";
import {PackageService} from "@application/packages/package-service";
import {SettingsStore} from "./settings-store";

export class SettingsWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IPackageService, PackageService),
            Registration.singleton(SettingsStore, SettingsStore),
            Registration.aliasTo(SettingsStore, IWindowDestinations),
        );
    }
}
