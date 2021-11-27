import {IEventBus} from "@domain";

export class ShortcutActionExecutionContext {
    constructor(
        public readonly event: KeyboardEvent,
        @IEventBus public readonly eventBus: IEventBus
    ) {}
}
