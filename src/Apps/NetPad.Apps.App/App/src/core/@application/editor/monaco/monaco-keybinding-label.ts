import {KeybindingCaps} from "@application/keybindings/keybinding-caps";

/**
 * Reads Monaco's own spelling of a key combination and converts it to a {@link KeybindingCaps}.
 * Monaco separates the strokes of a chord with a space and the keys within a stroke with `+`.
 *
 * macOS labels are modifier glyphs with no separator at all ("⌘K ⌘F"), so each stroke comes back
 * as a single cap.
 */
export function parseMonacoKeybindingLabel(label: string): KeybindingCaps {
    return label
        .split(" ")
        .filter(stroke => !!stroke)
        .map(stroke => stroke.split("+").filter(key => !!key));
}
