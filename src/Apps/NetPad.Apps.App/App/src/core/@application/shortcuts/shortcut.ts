import {Constructable} from "@aurelia/kernel/src/interfaces";
import {KeyCode} from "@common";
import {ShortcutActionExecutionContext} from "./shortcut-action-execution-context";

/**
 * A shortcut that executes an action.
 */
export class Shortcut {
    public ctrlKey = false;
    public altKey = false;
    public shiftKey = false;
    public metaKey = false;
    public key?: KeyCode;
    public keyExpression?: (keyCode: KeyCode) => boolean;
    public action?: (context: ShortcutActionExecutionContext) => void;
    public event?: Constructable | (() => unknown);
    public isConfigurable = false;
    public isEnabled = false;

    constructor(public name: string) {
    }

    public withKey(key: KeyCode): Shortcut {
        this.key = key;
        return this;
    }

    public withKeyExpression(expression: (keyCode: KeyCode) => boolean): Shortcut {
        this.keyExpression = expression;
        return this;
    }

    public withCtrlKey(mustBePressed = true): Shortcut {
        this.ctrlKey = mustBePressed;
        return this;
    }

    public withAltKey(mustBePressed = true): Shortcut {
        this.altKey = mustBePressed;
        return this;
    }

    public withShiftKey(mustBePressed = true): Shortcut {
        this.shiftKey = mustBePressed;
        return this;
    }

    public withMetaKey(mustBePressed = true): Shortcut {
        this.metaKey = mustBePressed;
        return this;
    }

    public hasAction(action: (context: ShortcutActionExecutionContext) => void): Shortcut {
        this.action = action;
        return this;
    }

    public firesEvent(eventGetter: () => unknown);
    public firesEvent<TEventType extends Constructable>(eventType: TEventType);

    public firesEvent<TEventType extends Constructable>(eventTypeOrGetter: TEventType | (() => unknown)) {
        this.event = eventTypeOrGetter;
        return this;
    }

    public configurable(isConfigurable = true): Shortcut {
        this.isConfigurable = isConfigurable;
        return this;
    }

    public enabled(isEnabled = true): Shortcut {
        this.isEnabled = isEnabled;
        return this;
    }

    /**
     * Determines if this shortcut matches the specified key combination.
     * @param key Key code.
     * @param ctrl Whether the ctrl key is pressed.
     * @param alt Whether the alt key is pressed.
     * @param shift Whether the shift key is pressed.
     * @param meta Whether the meta key is pressed.
     */
    public matches(
        key: KeyCode | undefined,
        ctrl: boolean,
        alt: boolean,
        shift: boolean,
        meta: boolean
    ): boolean;

    /**
     * Determines if this shortcut matches they key combination in the specified keyboard event.
     * @param event The keyboard event.
     */
    public matches(event: KeyboardEvent): boolean;

    /**
     * Determines if this shortcut has the same key combination as the specified shortcut.
     * @param shortcut The shortcut to compare with.
     */
    public matches(shortcut: Shortcut): boolean;

    public matches(
        keyOrEventOrShortcut: KeyCode | undefined | KeyboardEvent | Shortcut,
        ctrl?: boolean,
        alt?: boolean,
        shift?: boolean,
        meta?: boolean
    ): boolean {
        let key: KeyCode | undefined;

        if (keyOrEventOrShortcut instanceof KeyboardEvent) {
            key = keyOrEventOrShortcut.code as KeyCode;
            ctrl = keyOrEventOrShortcut.ctrlKey;
            alt = keyOrEventOrShortcut.altKey;
            shift = keyOrEventOrShortcut.shiftKey;
            meta = keyOrEventOrShortcut.metaKey;
        } else if (keyOrEventOrShortcut instanceof Shortcut) {
            return (
                this.key === keyOrEventOrShortcut.key &&
                this.keyExpression?.toString() === keyOrEventOrShortcut.keyExpression?.toString() &&
                this.ctrlKey === keyOrEventOrShortcut.ctrlKey &&
                this.altKey === keyOrEventOrShortcut.altKey &&
                this.shiftKey === keyOrEventOrShortcut.shiftKey &&
                this.metaKey === keyOrEventOrShortcut.metaKey
            );
        }

        return this.matchesKeyCombo(key, ctrl ?? false, alt ?? false, shift ?? false, meta ?? false);
    }

    public matchesKeyCombo(
        key: KeyCode | undefined | null,
        ctrl: boolean,
        alt: boolean,
        shift: boolean,
        meta: boolean
    ): boolean {
        if (!key) return false;

        if (this.key) {
            return (
                this.key === key &&
                this.ctrlKey === ctrl &&
                this.altKey === alt &&
                this.shiftKey === shift &&
                this.metaKey === meta
            );
        } else if (this.keyExpression) {
            return this.keyExpression(key);
        } else return false;
    }

    public get keyComboString(): string {
        const combo: string[] = [];
        if (this.metaKey) combo.push("Meta");
        if (this.altKey) combo.push("Alt");
        if (this.ctrlKey) combo.push("Ctrl");
        if (this.shiftKey) combo.push("Shift");
        if (this.key) combo.push(this.key.replace("Key", ""));
        if (this.keyExpression) combo.push("Custom Expression");

        return combo.join(" + ").trim();
    }

    public toString(): string {
        return `${this.name} (${this.keyComboString})`;
    }
}
