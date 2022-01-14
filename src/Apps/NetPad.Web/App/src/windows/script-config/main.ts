import {Aurelia, Registration} from "aurelia";
import {Index} from "./index";
import {IPackageService, IScriptService, PackageService, ScriptService} from "@domain";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IPackageService, PackageService),
        )
        .app(Index);
}
