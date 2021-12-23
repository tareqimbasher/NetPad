import {IEventBus} from "@domain";

export class ShortcutActionExecutionContext {
    constructor(
        public readonly event: KeyboardEvent | undefined | null,
        @IEventBus public readonly eventBus: IEventBus
    ) {}
}
