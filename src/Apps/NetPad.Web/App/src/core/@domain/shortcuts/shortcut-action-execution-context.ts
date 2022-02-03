import {IEventBus, ISession} from "@domain";
import {IContainer} from "aurelia";

export class ShortcutActionExecutionContext {
    public session: ISession;
    public eventBus: IEventBus;

    constructor(
        public readonly event: KeyboardEvent,
        @IContainer public readonly container: IContainer
    ) {
        this.session = container.get(ISession);
        this.eventBus = container.get(IEventBus);
    }
}
