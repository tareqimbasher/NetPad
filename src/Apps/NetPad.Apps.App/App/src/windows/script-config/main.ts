import {Aurelia, Registration} from "aurelia";
import {Window} from "./window";
import {
    AppService,
    AssemblyService,
    IAppService,
    IAssemblyService,
    IPackageService,
    IScriptService,
    PackageService,
    ScriptService
} from "@domain";
import {IWindowBootstrap} from "@application";

export class Bootstrapper implements IWindowBootstrap {
    getEntry = () => Window;

    registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IAppService, AppService),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAssemblyService, AssemblyService),
            Registration.singleton(IPackageService, PackageService),
        )
    }
}

