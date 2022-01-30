import {Aurelia, Registration} from "aurelia";
import {AppService, IAppService, IScriptService, IShortcutManager, ScriptService, ShortcutManager,} from "@domain";
import {Index} from "./index";
import {IWindowBootstrap} from "@application";

export class Bootstrapper implements IWindowBootstrap {
    getEntry = () => Index;

    registerServices(app: Aurelia): void {
        app.register(
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAppService, AppService),
        );
    }
}
