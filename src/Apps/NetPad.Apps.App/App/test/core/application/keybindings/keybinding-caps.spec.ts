import {spellKeybindingCaps} from "@application/keybindings/keybinding-caps";
import {KeyCombo} from "@application/keybindings/key-combo";

describe("spellKeybindingCaps", () => {
    test("a single stroke is spelled the way NetPad spells its own", () => {
        expect(spellKeybindingCaps([["Ctrl", "Shift", "P"]])).toBe("Ctrl + Shift + P");
    });

    test("a chord separates its strokes", () => {
        expect(spellKeybindingCaps([["Ctrl", "K"], ["Ctrl", "F"]])).toBe("Ctrl + K, Ctrl + F");
    });

    test("nothing to spell is undefined", () => {
        expect(spellKeybindingCaps(undefined)).toBeUndefined();
        expect(spellKeybindingCaps([])).toBeUndefined();
    });
});

describe("KeyCombo.asCaps", () => {
    test("an app combination is a single stroke", () => {
        const combo = new KeyCombo({primary: true, shift: true, key: "P"});

        expect(combo.asCaps("other")).toEqual([["Ctrl", "Shift", "P"]]);
    });
});
