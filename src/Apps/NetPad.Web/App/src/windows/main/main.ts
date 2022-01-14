import {Aurelia, Registration} from "aurelia";
import {
    IScriptService,
    ScriptService,
    ISettingService,
    SettingService,
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
            Registration.singleton(ISettingService, SettingService),
            Registration.singleton(IAppService, AppService),
        )
        .app(Index);
}
