import {Aurelia, Registration} from "aurelia";
import {Index} from "./index";
import {EventBus, IEventBus, IScriptService, ISession, ScriptService, Session} from "@domain";

export function register(app: Aurelia): void {
    app
        .register(
            Registration.singleton(IEventBus, EventBus),
            Registration.singleton(ISession, Session),
            Registration.singleton(IScriptService, ScriptService),
        )
        .app(Index);
}
