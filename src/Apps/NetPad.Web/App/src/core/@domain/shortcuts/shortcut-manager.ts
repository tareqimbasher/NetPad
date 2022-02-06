import {DI, IContainer} from "aurelia";
import {Shortcut} from "./shortcut";
import {ShortcutActionExecutionContext} from "./shortcut-action-execution-context";
import {IEventBus} from "@domain";

export interface IShortcutManager {
    initialize(): void;
    registerShortcut(shortcut: Shortcut): void;
    getShortcutByName(name: string): Shortcut | undefined;
}

export const IShortcutManager = DI.createInterface<IShortcutManager>();

export class ShortcutManager implements IShortcutManager{
    private registry: Shortcut[] = [];

    constructor(@IEventBus readonly eventBus: IEventBus, @IContainer readonly container: IContainer) {
    }

    public getShortcutByName(name: string): Shortcut | undefined {
        return this.registry.find(s => s.name === name);
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
            const shortcut = this.registry.find((s) => s.isEnabled && s.matches(ev));
            if (!shortcut) return;

            if (shortcut.action) {
                const context = new ShortcutActionExecutionContext(ev, this.container);
                shortcut.action(context);
            }

            if (shortcut.eventType) {
                this.eventBus.publish(new shortcut.eventType());
            }

            ev.preventDefault();
        });
    }
}
