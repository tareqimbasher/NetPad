/**
 * A key combination broken into strokes of key caps. A single-stroke combination is one entry
 * (`[["Ctrl", "Shift", "P"]]`); a chord is one entry per stroke (`[["Ctrl", "K"], ["Ctrl", "F"]]`).
 */
export type KeybindingCaps = string[][];

/**
 * Spells a combination as a string: "Ctrl + Shift + P", and a chord as "Ctrl + K, Ctrl + F".
 */
export function spellKeybindingCaps(caps: KeybindingCaps | undefined): string | undefined {
    if (!caps?.length) return undefined;

    return caps.map(stroke => stroke.join(" + ")).join(", ");
}
