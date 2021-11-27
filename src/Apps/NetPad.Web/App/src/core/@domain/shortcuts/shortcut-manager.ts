import {Shortcut} from "./shortcut";
import {ShortcutActionExecutionContext} from "./shortcut-action-execution-context";
import {IEventBus} from "@domain/events/event-bus";
import {DI} from "aurelia";

export interface IShortcutManager {
    initialize(): void;
    registerShortcut(shortcut: Shortcut): void;
}

export const IShortcutManager = DI.createInterface<IShortcutManager>();

export class ShortcutManager implements IShortcutManager{
    private registry: Shortcut[] = [];

    constructor(@IEventBus readonly eventBus: IEventBus) {
    }

    public registerShortcut(shortcut: Shortcut) {
        const existing = this.registry.findIndex((s) => s.matches(shortcut));
        if (existing >= 0) {
            this.registry[existing] = shortcut;
        } else {
            this.registry.push(shortcut);
        }
    }

    public initialize() {
        document.addEventListener("keydown", (ev) => {
            const shortcut = this.registry.find((s) => s.matches(ev));
            if (!shortcut) return;

            const context = new ShortcutActionExecutionContext(ev, this.eventBus);

            shortcut.action(context);
            ev.preventDefault();
        });
    }
}
