import {KeyCode} from "@common";
import {IKeyboardShortcutConfiguration} from "@application";

/**
 * A combination of keyboard keys.
 */
export class KeyCombo {
    public meta = false;
    public alt = false;
    public ctrl = false;
    public shift = false;
    public key?: KeyCode;

    public get hasModifier(): boolean {
        return this.meta || this.alt || this.ctrl || this.shift;
    }

    /**
     * Sets whether META/Super key is required as part of this KeyCombo.
     */
    public withMetaKey(required = true): KeyCombo {
        this.meta = required;
        return this;
    }

    /**
     * Sets whether ALT key is required as part of this KeyCombo.
     */
    public withAltKey(required = true): KeyCombo {
        this.alt = required;
        return this;
    }

    /**
     * Sets whether CTRL key is required as part of this KeyCombo.
     */
    public withCtrlKey(required = true): KeyCombo {
        this.ctrl = required;
        return this;
    }

    /**
     * Sets whether SHIFT key is required as part of this KeyCombo.
     */
    public withShiftKey(required = true): KeyCombo {
        this.shift = required;
        return this;
    }

    /**
     * Sets whether CTRL key is required as part of this KeyCombo.
     */
    public withKey(key: KeyCode | undefined): KeyCombo {
        this.key = key;
        return this;
    }

    public updateFrom(config: IKeyboardShortcutConfiguration | KeyCombo) {
        this.withMetaKey(config.meta)
            .withAltKey(config.alt)
            .withCtrlKey(config.ctrl)
            .withShiftKey(config.shift)
            .withKey(config instanceof KeyCombo ? config.key : config.key as KeyCode);

        return this;
    }

    public copyTo(config: IKeyboardShortcutConfiguration) {
        config.meta = this.meta;
        config.alt = this.alt;
        config.ctrl = this.ctrl;
        config.shift = this.shift;
        config.key = this.key;

        return this;
    }

    /**
     * Creates a deep copy of this KeyCombo instance.
     */
    public clone(): KeyCombo {
        return new KeyCombo().updateFrom(this);
    }

    /**
     * Determines if this KeyCombo matches the specified key combination.
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
     * Determines if this KeyCombo matches they key combination in the specified keyboard event.
     * @param event The keyboard event.
     */
    public matches(event: KeyboardEvent): boolean;

    /**
     * Determines if this KeyCombo has the same key combination as the specified key combo.
     * @param keyCombo The KeyCombo to compare with.
     */
    public matches(keyCombo: KeyCombo): boolean;

    public matches(
        keyOrEventOrCombo: KeyCode | undefined | KeyboardEvent | KeyCombo,
        ctrl?: boolean,
        alt?: boolean,
        shift?: boolean,
        meta?: boolean
    ): boolean {
        let key: KeyCode | undefined;

        if (keyOrEventOrCombo instanceof KeyboardEvent) {
            key = keyOrEventOrCombo.code as KeyCode;
            ctrl = keyOrEventOrCombo.ctrlKey;
            alt = keyOrEventOrCombo.altKey;
            shift = keyOrEventOrCombo.shiftKey;
            meta = keyOrEventOrCombo.metaKey;
        } else if (keyOrEventOrCombo instanceof KeyCombo) {
            return (
                this.key === keyOrEventOrCombo.key &&
                this.ctrl === keyOrEventOrCombo.ctrl &&
                this.alt === keyOrEventOrCombo.alt &&
                this.shift === keyOrEventOrCombo.shift &&
                this.meta === keyOrEventOrCombo.meta
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
                this.ctrl === ctrl &&
                this.alt === alt &&
                this.shift === shift &&
                this.meta === meta
            );
        } else
            return false;
    }

    public get asArray(): string[] {
        const combo: string[] = [];
        if (this.meta) combo.push("Meta");
        if (this.alt) combo.push("Alt");
        if (this.ctrl) combo.push("Ctrl");
        if (this.shift) combo.push("Shift");
        if (this.key)
            combo.push(
                this.key
                    .replace("Key", "")
                    .replace("Digit", "")
                    .replace("Semicolon", ";")
                    .replace("Equal", "=")
                    .replace("Comma", ",")
                    .replace("Minus", "-")
                    .replace("Period", ".")
                    .replace("Slash", "/")
                    .replace("Backquote", "`")
                    .replace("BracketLeft", "[")
                    .replace("Backslash", "\\")
                    .replace("BracketRight", "]")
                    .replace("Quote", "'")
            );

        return combo;
    }

    public get asString(): string {
        return this.asArray.join(" + ");
    }

    public toString(): string {
        return this.asString;
    }

    public static fromKeyboardEvent(event: KeyboardEvent): KeyCombo {
        const combo = new KeyCombo();

        if (event.metaKey) combo.withMetaKey();
        if (event.altKey) combo.withAltKey();
        if (event.ctrlKey) combo.withCtrlKey();
        if (event.shiftKey) combo.withShiftKey();

        const key = event.key.toUpperCase();

        if (["ALT", "CONTROL", "SHIFT", "META"].indexOf(key) < 0) {
            const keyCode = KeyCode[event.code as keyof typeof KeyCode];
            if (!keyCode) throw new Error("Unknown keycode: " + event.code);
            combo.withKey(keyCode);
        }

        return combo;
    }

    public static fromKeyCombo(keyCombo: KeyCombo): KeyCombo {
        return keyCombo.clone();
    }
}
