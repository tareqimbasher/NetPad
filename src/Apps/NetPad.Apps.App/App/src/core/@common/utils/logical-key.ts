/**
 * A keyboard key identified by what it produces on the user's layout, the normalized form of
 * `KeyboardEvent.key`. Single characters are upper-cased ("s" and "S" are one key).
 */
export type LogicalKey = string;

const modifierKeyNames = new Set([
    "Alt",
    "AltGraph",
    "CapsLock",
    "Control",
    "Fn",
    "FnLock",
    "Hyper",
    "Meta",
    "NumLock",
    "ScrollLock",
    "Shift",
    "Super",
    "Symbol",
    "SymbolLock",
]);

/**
 * Whether the key is a modifier and so cannot be the key a combination ends on.
 */
export function isModifierKey(key: string): boolean {
    return modifierKeyNames.has(key);
}

/**
 * Normalizes a raw `KeyboardEvent.key` into a {@link LogicalKey}. Returns undefined for keys that
 * cannot terminate a combination: modifiers, and the "Dead" key some layouts emit for accents.
 */
export function normalizeLogicalKey(key: string | undefined): LogicalKey | undefined {
    if (!key || key === "Dead" || key === "Unidentified" || isModifierKey(key)) {
        return undefined;
    }

    if (key === " ") {
        return "Space";
    }

    return key.length === 1 ? key.toUpperCase() : key;
}
