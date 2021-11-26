import {Aurelia, Registration} from "aurelia";
import {
    IEventBus,
    EventBus,
    ISession,
    Session,
    IScriptService,
    ScriptService,
    ISettingService,
    SettingService,
} from "@domain";
import {Index} from "./index";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(IEventBus, EventBus),
            Registration.singleton(ISession, Session),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(ISettingService, SettingService),
        )
        .app(Index);
}
