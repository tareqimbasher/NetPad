import {IDisposable, ILogger} from "aurelia";
import {IEventBus} from "@application/events/ievent-bus";
import {Settings, SettingsUpdatedEvent} from "@application/api";
import {ICommandRegistry} from "@application/commands/icommand-registry";
import {Keybinding} from "./keybinding";
import {IKeybindingManager} from "./ikeybinding-manager";
import {resolveKeybindings} from "./builtin-keybindings";

export class KeybindingManager implements IKeybindingManager {
    private _keybindings: Keybinding[] = [];
    private readonly onChangedCallbacks = new Set<() => void>();
    private readonly logger: ILogger;

    constructor(
        private readonly settings: Settings,
        @IEventBus private readonly eventBus: IEventBus,
        @ICommandRegistry private readonly commandRegistry: ICommandRegistry,
        @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(KeybindingManager));
    }

    public get keybindings(): ReadonlyArray<Keybinding> {
        return this._keybindings;
    }

    public initialize() {
        this.logger.debug("Initializing");

        this.apply(this.settings);

        this.eventBus.subscribeToServer(SettingsUpdatedEvent, event => this.apply(event.settings));

        document.addEventListener("keydown", async (ev) => {
            const keybinding = this._keybindings.find(k => k.keyCombo.matches(ev));
            if (!keybinding || !this.commandRegistry.get(keybinding.commandId)) return;

            ev.preventDefault();

            await this.commandRegistry.execute(keybinding.commandId);
        });
    }

    public getKeybinding(commandId: string): Keybinding | undefined {
        return this._keybindings.find(k => k.commandId === commandId);
    }

    public keysFor(commandId: string): string | undefined {
        const keyCombo = this.getKeybinding(commandId)?.keyCombo;
        return keyCombo?.isBound ? keyCombo.asString() : undefined;
    }

    public describe(commandId: string): string {
        const title = this.commandRegistry.get(commandId)?.title ?? commandId;
        const keys = this.keysFor(commandId);
        return keys ? `${title} (${keys})` : title;
    }

    public onChanged(callback: () => void): IDisposable {
        this.onChangedCallbacks.add(callback);
        return {dispose: () => this.onChangedCallbacks.delete(callback)};
    }

    private apply(settings: Settings) {
        this._keybindings = resolveKeybindings(settings);

        for (const callback of this.onChangedCallbacks) {
            try {
                callback();
            } catch (err) {
                this.logger.error("A keybinding onChanged callback threw", err);
            }
        }
    }
}
