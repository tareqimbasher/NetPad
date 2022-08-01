import {Constructable, DI, IContainer, ILogger} from "aurelia";
import {Shortcut} from "./shortcut";
import {ShortcutActionExecutionContext} from "./shortcut-action-execution-context";
import {IEventBus} from "@domain";

export interface IShortcutManager {
    /**
     * Initializes the ShortcutManager and starts listening for keyboard events.
     */
    initialize(): void;

    /**
     * Adds a shortcut to the shortcut registry.
     * @param shortcut The shortcut to register.
     */
    registerShortcut(shortcut: Shortcut): void;

    /**
     * Removes a shortcut from the shortcut registry.
     * @param shortcut The shortcut to unregister.
     */
    unregisterShortcut(shortcut: Shortcut): void;

    /**
     * Finds a shortcut by its name, if one exists.
     * @param name The name of the shortcut to get.
     */
    getShortcutByName(name: string): Shortcut | undefined;

    /**
     * Executes a shortcut.
     * @param shortcut
     */
    executeShortcut(shortcut: Shortcut): void;
}

export const IShortcutManager = DI.createInterface<IShortcutManager>();

export class ShortcutManager implements IShortcutManager {
    private readonly registry: Shortcut[] = [];
    private readonly logger: ILogger;

    constructor(
        @IEventBus private readonly eventBus: IEventBus,
        @IContainer private readonly container: IContainer,
        @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(ShortcutManager));
    }

    public getShortcutByName(name: string): Shortcut | undefined {
        return this.registry.find(s => s.name === name);
    }

    public registerShortcut(shortcut: Shortcut) {
        this.logger.debug(`Registering shortcut "${shortcut.name}"`);

        const existing = this.registry.findIndex((s) => s.matches(shortcut));

        if (existing >= 0) {
            this.registry[existing] = shortcut;
        } else {
            this.registry.push(shortcut);
        }
    }

    public unregisterShortcut(shortcut: Shortcut) {
        this.logger.debug(`Unregistering shortcut "${shortcut.name}"`);

        const ix = this.registry.indexOf(shortcut);

        if (ix >= 0) {
            this.registry.splice(ix, 1);
        }
    }

    public initialize() {
        this.logger.debug("Initializing");

        document.addEventListener("keydown", (ev) => {
            const shortcut = this.registry.find((s) => s.isEnabled && s.matches(ev));
            if (!shortcut) return;

            this.executeShortcut(shortcut);

            ev.preventDefault();
        });
    }

    public executeShortcut(shortcut: Shortcut) {
        this.logger.debug(`Executing shortcut "${shortcut.name}"`);

        if (shortcut.action) {
            const context = new ShortcutActionExecutionContext(this.container);
            shortcut.action(context);
        }

        if (shortcut.event) {
            const event = shortcut.event.hasOwnProperty("prototype")
                ? new (shortcut.event as Constructable)()
                : (shortcut.event as () => unknown)();

            this.eventBus.publish(event);
        }
    }
}
