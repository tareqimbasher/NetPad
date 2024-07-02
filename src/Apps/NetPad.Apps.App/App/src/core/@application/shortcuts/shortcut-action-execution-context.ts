import {IEventBus, ISession} from "@application";
import {IContainer} from "aurelia";

export class ShortcutActionExecutionContext {
    public session: ISession;
    public eventBus: IEventBus;

    constructor(@IContainer public readonly container: IContainer) {
        this.session = container.get(ISession);
        this.eventBus = container.get(IEventBus);
    }
}
