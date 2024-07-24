import {Constructable, IContainer, ILogger} from "aurelia";
import {IEventBus, Settings, SettingsUpdatedEvent} from "@application";
import {Shortcut} from "./shortcut";
import {ShortcutActionExecutionContext} from "./shortcut-action-execution-context";
import {BuiltinShortcuts} from "./builtin-shortcuts";
import {IShortcutManager} from "./ishortcut-manager";

export class ShortcutManager implements IShortcutManager {
    private readonly registry: Shortcut[] = [];
    private readonly logger: ILogger;

    constructor(
        private readonly settings: Settings,
        @IEventBus private readonly eventBus: IEventBus,
        @IContainer private readonly container: IContainer,
        @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(ShortcutManager));
    }

    public initialize() {
        this.logger.debug("Initializing");

        const builtInShortcuts = [...BuiltinShortcuts];

        const addOrUpdateShortcuts = (settings: Settings) => {
            const configs = settings.keyboardShortcuts.shortcuts;

            for (const builtinShortcut of builtInShortcuts) {
                let shortcut = this.getShortcut(builtinShortcut.id);

                if (!shortcut) {
                    this.registerShortcut(builtinShortcut);
                    shortcut = builtinShortcut;
                }

                const config = configs.find(x => x.id === shortcut!.id);

                if (config) {
                    shortcut.keyCombo.updateFrom(config);
                } else {
                    shortcut.resetKeyCombo();
                }
            }
        };

        this.eventBus.subscribeToServer(SettingsUpdatedEvent, event => addOrUpdateShortcuts(event.settings));

        addOrUpdateShortcuts(this.settings);

        // Listen and process keyboard events
        document.addEventListener("keydown", async (ev) => {
            const shortcut = this.registry.find((s) => s.isEnabled && s.keyCombo.matches(ev));
            if (!shortcut) return;

            ev.preventDefault();

            await this.executeShortcut(shortcut);
        });
    }

    public registerShortcut(shortcut: Shortcut) {
        this.logger.debug(`Registering shortcut "${shortcut.name}" (${shortcut.keyCombo.asString})`);

        const existing = this.registry.findIndex((s) => s.keyCombo.matches(shortcut.keyCombo));

        if (existing >= 0) {
            this.registry[existing] = shortcut;
        } else {
            this.registry.push(shortcut);
        }
    }

    public getShortcut(id: string): Shortcut | undefined {
        return this.registry.find(s => s.id === id);
    }

    public getShortcutByName(name: string): Shortcut | undefined {
        return this.registry.find(s => s.name === name);
    }

    public unregisterShortcut(shortcut: Shortcut) {
        this.logger.debug(`Unregistering shortcut "${shortcut.name}" (${shortcut.keyCombo.asString})`);

        const ix = this.registry.indexOf(shortcut);

        if (ix >= 0) {
            this.registry.splice(ix, 1);
        }
    }

    public async executeShortcut(shortcut: Shortcut) {
        this.logger.debug(`Executing shortcut "${shortcut.name}" (${shortcut.keyCombo.asString})`);

        if (shortcut.action) {
            const context = new ShortcutActionExecutionContext(this.container);
            shortcut.action(context);
        }

        if (shortcut.event) {
            let event: InstanceType<Constructable>;

            if (Object.hasOwnProperty.bind(shortcut.event)("prototype")) {
                event = new (shortcut.event as Constructable)();
            } else {
                const eventOrPromise = (shortcut.event as () => (unknown | Promise<unknown>))();
                event = await Promise.resolve(eventOrPromise) as InstanceType<Constructable>;
            }

            this.eventBus.publish(event);
        }
    }
}
