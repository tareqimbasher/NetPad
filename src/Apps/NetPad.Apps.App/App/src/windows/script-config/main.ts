import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {
    AssemblyService,
    IAssemblyService,
    IPackageService,
    IScriptService,
    PackageService,
    ScriptService
} from "@domain";
import {IWindowBootstrapper} from "@application";

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

