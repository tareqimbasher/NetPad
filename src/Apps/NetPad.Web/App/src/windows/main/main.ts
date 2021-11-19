import {Aurelia, Registration} from "aurelia";
import {
    ISession,
    Session,
    IQueryManager,
    QueryManager,
    ISettingsManager,
    SettingsManager,
    ISessionManager, SessionManager
} from "@domain";
import {IBackgroundService, QueryBackgroundService, SessionBackgroundService} from "./background-services";
import {Index} from "./index";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(ISession, Session),
            Registration.singleton(ISessionManager, SessionManager),
            Registration.singleton(IQueryManager, QueryManager),
            Registration.singleton(ISettingsManager, SettingsManager),
            Registration.singleton(IBackgroundService, SessionBackgroundService),
            Registration.singleton(IBackgroundService, QueryBackgroundService),
        )
        .app(Index);
}
