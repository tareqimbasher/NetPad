import {KeyCombo} from "./key-combo";

/**
 * Binds a key combination to a command.
 */
export class Keybinding {
    public readonly defaultKeyCombo: KeyCombo;
    public keyCombo: KeyCombo;

    constructor(public readonly commandId: string, defaultKeyCombo?: KeyCombo) {
        this.defaultKeyCombo = defaultKeyCombo?.clone() ?? new KeyCombo();
        this.keyCombo = this.defaultKeyCombo.clone();
    }

    public get isDefault(): boolean {
        return this.keyCombo.matches(this.defaultKeyCombo);
    }

    public reset(): Keybinding {
        this.keyCombo = this.defaultKeyCombo.clone();
        return this;
    }

    public toString(): string {
        return `${this.commandId} (${this.keyCombo.asString()})`;
    }
}
