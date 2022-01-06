import {Aurelia, Registration} from "aurelia";
import {
    IIpcGateway,
    ElectronIpcGateway,
    IEventBus,
    EventBus,
    ISession,
    Session,
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
            Registration.singleton(IIpcGateway, ElectronIpcGateway),
            Registration.singleton(IEventBus, EventBus),
            Registration.singleton(IShortcutManager, ShortcutManager),
            Registration.singleton(ISession, Session),
            Registration.singleton(IScriptService, ScriptService),
            Registration.singleton(ISettingService, SettingService),
            Registration.singleton(IAppService, AppService),
        )
        .app(Index);
}
