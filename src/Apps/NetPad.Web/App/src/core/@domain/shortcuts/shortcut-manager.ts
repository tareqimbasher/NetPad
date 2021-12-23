import {Shortcut} from "./shortcut";
import {ShortcutActionExecutionContext} from "./shortcut-action-execution-context";
import {IEventBus} from "@domain/events/event-bus";
import {DI} from "aurelia";
import {KeyCode} from "@common";

export interface IShortcutManager {
    initialize(): void;
    getRegisteredShortcuts(): Shortcut[];
    registerShortcut(shortcut: Shortcut): void;
    executeKeyCombination(
        key: KeyCode | undefined,
        ctrl?: boolean,
        alt?: boolean,
        shift?: boolean,
        meta?: boolean
    ): void;
}

export const IShortcutManager = DI.createInterface<IShortcutManager>();

export class ShortcutManager implements IShortcutManager{
    private registry: Shortcut[] = [];

    constructor(@IEventBus readonly eventBus: IEventBus) {
    }

    public initialize() {
        document.addEventListener("keydown", (ev) => {
            const shortcut = this.registry.find((s) => s.matches(ev));
            if (!shortcut) return;

            this.execute(shortcut, ev);
            ev.preventDefault();
        });
    }

    public getRegisteredShortcuts(): Shortcut[] {
        return [...this.registry];
    }

    public registerShortcut(shortcut: Shortcut) {
        const existing = this.registry.findIndex((s) => s.matches(shortcut));
        if (existing >= 0) {
            this.registry[existing] = shortcut;
        } else {
            this.registry.push(shortcut);
        }
    }

    public executeKeyCombination(
        key: KeyCode | undefined,
        ctrl?: boolean,
        alt?: boolean,
        shift?: boolean,
        meta?: boolean
    ): void {
        const shortcut = this.registry.find(s => s.matches(
            key,
            ctrl,
            alt,
            shift,
            meta
        ));

        console.log("shortcut", shortcut);

        if (shortcut) {
            this.execute(shortcut);
        }
    }

    private execute(shortcut: Shortcut, keyboardEvent: KeyboardEvent = null) {
        const context = new ShortcutActionExecutionContext(keyboardEvent, this.eventBus);
        shortcut.action(context);
    }
}
