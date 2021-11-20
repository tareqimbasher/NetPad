import {Aurelia, Registration} from "aurelia";
import {
    ISession,
    Session,
    IScriptManager,
    ScriptManager,
    ISettingsManager,
    SettingsManager,
    ISessionManager, SessionManager
} from "@domain";
import {IBackgroundService, ScriptBackgroundService, SessionBackgroundService} from "./background-services";
import {Index} from "./index";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(ISession, Session),
            Registration.singleton(ISessionManager, SessionManager),
            Registration.singleton(IScriptManager, ScriptManager),
            Registration.singleton(ISettingsManager, SettingsManager),
            Registration.singleton(IBackgroundService, SessionBackgroundService),
            Registration.singleton(IBackgroundService, ScriptBackgroundService),
        )
        .app(Index);
}
