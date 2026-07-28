import {currentOs, LogicalKey, normalizeLogicalKey, OperatingSystem} from "@common";
import {IKeyboardShortcutConfiguration} from "@application";

export interface KeyComboParts {
    primary?: boolean;
    alt?: boolean;
    shift?: boolean;
    meta?: boolean;
    key?: LogicalKey;
}

/**
 * A combination of keyboard keys.
 *
 * The two system modifiers are named by role, not by key cap, so a combination means the same thing
 * on every platform:
 * - {@link primary} is Ctrl on Windows and Linux, Cmd on macOS. It is the modifier NetPad's defaults
 *   use, and it agrees with Electron's `CmdOrCtrl` accelerator and Monaco's `KeyMod.CtrlCmd`.
 * - {@link meta} is the other one: Meta/Super on Windows and Linux, Ctrl on macOS.
 */
export class KeyCombo {
    public primary = false;
    public alt = false;
    public shift = false;
    public meta = false;
    public key?: LogicalKey;

    constructor(parts?: KeyComboParts) {
        if (parts) {
            this.primary = parts.primary ?? false;
            this.alt = parts.alt ?? false;
            this.shift = parts.shift ?? false;
            this.meta = parts.meta ?? false;
            this.key = parts.key;
        }
    }

    public get hasModifier(): boolean {
        return this.primary || this.alt || this.shift || this.meta;
    }

    /**
     * Whether this combination can fire. A combination without a key is unassigned.
     */
    public get isBound(): boolean {
        return !!this.key;
    }

    public updateFrom(source: IKeyboardShortcutConfiguration | KeyComboParts): KeyCombo {
        this.primary = source.primary ?? false;
        this.alt = source.alt ?? false;
        this.shift = source.shift ?? false;
        this.meta = source.meta ?? false;
        this.key = source.key || undefined;
        return this;
    }

    public copyTo(config: IKeyboardShortcutConfiguration): KeyCombo {
        config.primary = this.primary;
        config.alt = this.alt;
        config.shift = this.shift;
        config.meta = this.meta;
        config.key = this.key;
        return this;
    }

    public clone(): KeyCombo {
        return new KeyCombo().updateFrom(this);
    }

    /**
     * Determines whether this combination is the one the keyboard event represents.
     */
    public matches(event: KeyboardEvent, os?: OperatingSystem): boolean;

    /**
     * Determines whether this combination is the same as another.
     */
    public matches(keyCombo: KeyCombo): boolean;

    public matches(eventOrCombo: KeyboardEvent | KeyCombo, os: OperatingSystem = currentOs): boolean {
        if (eventOrCombo instanceof KeyCombo) {
            return this.key === eventOrCombo.key
                && this.primary === eventOrCombo.primary
                && this.alt === eventOrCombo.alt
                && this.shift === eventOrCombo.shift
                && this.meta === eventOrCombo.meta;
        }

        const isMac = os === "macos";

        return !!this.key
            && this.key === normalizeLogicalKey(eventOrCombo.key)
            && this.primary === (isMac ? eventOrCombo.metaKey : eventOrCombo.ctrlKey)
            && this.alt === eventOrCombo.altKey
            && this.shift === eventOrCombo.shiftKey
            && this.meta === (isMac ? eventOrCombo.ctrlKey : eventOrCombo.metaKey);
    }

    /**
     * The parts of this combination as they are shown to the user. macOS gets its modifier glyphs in
     * the order Apple orders them, everywhere else the modifiers are spelled out.
     */
    public asArray(os: OperatingSystem = currentOs): string[] {
        const parts: string[] = [];

        if (os === "macos") {
            if (this.meta) parts.push("⌃");
            if (this.alt) parts.push("⌥");
            if (this.shift) parts.push("⇧");
            if (this.primary) parts.push("⌘");
        } else {
            if (this.primary) parts.push("Ctrl");
            if (this.shift) parts.push("Shift");
            if (this.alt) parts.push("Alt");
            if (this.meta) parts.push("Meta");
        }

        if (this.key) parts.push(this.key);

        return parts;
    }

    public asString(os: OperatingSystem = currentOs): string {
        return this.asArray(os).join(os === "macos" ? "" : " + ");
    }

    /**
     * This combination as an Electron/Tauri accelerator, or undefined when it is unassigned.
     */
    public asAccelerator(os: OperatingSystem = currentOs): string | undefined {
        if (!this.key) return undefined;

        const parts: string[] = [];
        if (this.primary) parts.push("CmdOrCtrl");
        if (this.shift) parts.push("Shift");
        if (this.alt) parts.push("Alt");
        if (this.meta) parts.push(os === "macos" ? "Control" : "Super");
        parts.push(this.key === "Space" ? "Space" : this.key);

        return parts.join("+");
    }

    public toString(): string {
        return this.asString();
    }

    /**
     * Reads the combination a user just pressed. Returns a combination with no key while only
     * modifiers are held, so a capture UI can show the combination forming.
     */
    public static fromKeyboardEvent(event: KeyboardEvent, os: OperatingSystem = currentOs): KeyCombo {
        const isMac = os === "macos";

        return new KeyCombo({
            primary: isMac ? event.metaKey : event.ctrlKey,
            alt: event.altKey,
            shift: event.shiftKey,
            meta: isMac ? event.ctrlKey : event.metaKey,
            key: normalizeLogicalKey(event.key),
        });
    }

    public static fromConfiguration(config: IKeyboardShortcutConfiguration): KeyCombo {
        return new KeyCombo().updateFrom(config);
    }
}
