import {Aurelia, Registration} from "aurelia";
import {
    ISession,
    Session,
    IScriptRepository,
    ScriptRepository,
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
            Registration.singleton(IScriptRepository, ScriptRepository),
            Registration.singleton(ISettingsManager, SettingsManager),
            Registration.singleton(IBackgroundService, SessionBackgroundService),
            Registration.singleton(IBackgroundService, ScriptBackgroundService),
        )
        .app(Index);
}
