import {Aurelia, Registration} from "aurelia";
import {
    IEventBus,
    EventBus,
    ISession,
    Session,
    IScriptManager,
    ScriptManager,
    ISettingsManager,
    SettingsManager,
} from "@domain";
import {Index} from "./index";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(IEventBus, EventBus),
            Registration.singleton(ISession, Session),
            Registration.singleton(IScriptManager, ScriptManager),
            Registration.singleton(ISettingsManager, SettingsManager),
        )
        .app(Index);
}
