import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {
    IAssemblyService,
    IPackageService,
    IScriptService,
    IWindowBootstrapper,
    IWindowDestinations,
} from "@application";
import {AssemblyService} from "@application/assemblies/assembly-service";
import {PackageService} from "@application/packages/package-service";
import {ScriptService} from "@application/scripts/script-service";
import {ConfigStore} from "./config-store";

export class ScriptConfigWindowBootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAssemblyService, AssemblyService),
            Registration.singleton(IPackageService, PackageService),
            Registration.singleton(ConfigStore, ConfigStore),
            Registration.aliasTo(ConfigStore, IWindowDestinations),
        );
    }
}

