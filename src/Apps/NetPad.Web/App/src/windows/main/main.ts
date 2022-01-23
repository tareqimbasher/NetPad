import {Aurelia, Registration} from "aurelia";
import {
    IScriptService,
    ScriptService,
    IShortcutManager,
    ShortcutManager,
    IAppService,
    AppService,
} from "@domain";
import {Index} from "./index";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(IAppService, AppService),
        )
        .app(Index);
}
