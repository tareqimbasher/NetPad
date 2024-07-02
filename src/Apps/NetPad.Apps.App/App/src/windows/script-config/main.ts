import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {
    AssemblyService,
    IAssemblyService,
    IPackageService,
    IScriptService,
    IWindowBootstrapper,
    PackageService,
    ScriptService
} from "@application";

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

