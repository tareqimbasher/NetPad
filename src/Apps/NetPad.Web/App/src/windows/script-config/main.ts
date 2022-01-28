import {Aurelia, Registration} from "aurelia";
import {Index} from "./index";
import {
    AppService, AssemblyService,
    IAppService,
    IAssemblyService,
    IPackageService,
    IScriptService,
    PackageService,
    ScriptService
} from "@domain";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(IAppService, AppService),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAssemblyService, AssemblyService),
            Registration.singleton(IPackageService, PackageService),
        )
        .app(Index);
}
