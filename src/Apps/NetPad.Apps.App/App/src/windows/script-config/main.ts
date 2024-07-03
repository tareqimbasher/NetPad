import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {IAssemblyService, IPackageService, IScriptService, IWindowBootstrapper,} from "@application";
import {AssemblyService} from "@application/assemblies/assembly-service";
import {PackageService} from "@application/packages/package-service";
import {ScriptService} from "@application/scripts/script-service";

export class Bootstrapper implements IWindowBootstrapper {
    public getEntry = () => Window;

    public registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAssemblyService, AssemblyService),
            Registration.singleton(IPackageService, PackageService),
        );
    }
}

