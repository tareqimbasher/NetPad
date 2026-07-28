import {KeyCombo, KeyComboParts} from "@application/keybindings/key-combo";

function press(init: KeyboardEventInit): KeyboardEvent {
    return new KeyboardEvent("keydown", init);
}

describe("KeyCombo.matches(event)", () => {
    test("matches the key the layout produces, not the key's position", () => {
        const combo = new KeyCombo({primary: true, key: "Z"});

        // On QWERTZ the key that types "z" sits where a US layout has "y".
        expect(combo.matches(press({key: "z", code: "KeyY", ctrlKey: true}))).toBe(true);

        // and the key at the US "z" position types "y" there, so it must not match.
        expect(combo.matches(press({key: "y", code: "KeyZ", ctrlKey: true}))).toBe(false);
    });

    test("is case-insensitive about the key", () => {
        const combo = new KeyCombo({primary: true, shift: true, key: "S"});

        expect(combo.matches(press({key: "S", ctrlKey: true, shiftKey: true}))).toBe(true);
        expect(combo.matches(press({key: "s", ctrlKey: true, shiftKey: true}))).toBe(true);
    });

    test("requires every modifier to agree", () => {
        const combo = new KeyCombo({alt: true, key: "E"});

        expect(combo.matches(press({key: "e", altKey: true}))).toBe(true);
        expect(combo.matches(press({key: "e"}))).toBe(false);
        expect(combo.matches(press({key: "e", altKey: true, shiftKey: true}))).toBe(false);
    });

    test("an unassigned combination never matches", () => {
        expect(new KeyCombo({primary: true}).matches(press({key: "s", ctrlKey: true}))).toBe(false);
    });
});

describe("the primary modifier resolves per platform", () => {
    const save = new KeyCombo({primary: true, key: "S"});

    test("is Ctrl on Windows and Linux", () => {
        expect(save.matches(press({key: "s", ctrlKey: true}), "other")).toBe(true);
        expect(save.matches(press({key: "s", metaKey: true}), "other")).toBe(false);
    });

    test("is Cmd on macOS", () => {
        expect(save.matches(press({key: "s", metaKey: true}), "macos")).toBe(true);
        expect(save.matches(press({key: "s", ctrlKey: true}), "macos")).toBe(false);
    });

    test("the other system modifier is Meta off macOS and Ctrl on it", () => {
        const combo = new KeyCombo({meta: true, key: "S"});

        expect(combo.matches(press({key: "s", metaKey: true}), "other")).toBe(true);
        expect(combo.matches(press({key: "s", ctrlKey: true}), "macos")).toBe(true);
        expect(combo.matches(press({key: "s", metaKey: true}), "macos")).toBe(false);
    });

    test("a combination captured on one platform matches the same physical keys there", () => {
        const captured = KeyCombo.fromKeyboardEvent(press({key: "s", metaKey: true}), "macos");

        expect(captured.primary).toBe(true);
        expect(captured.meta).toBe(false);
        expect(captured.matches(press({key: "s", metaKey: true}), "macos")).toBe(true);
    });
});

describe("KeyCombo.fromKeyboardEvent", () => {
    test("reports a combination with no key while only modifiers are held", () => {
        const combo = KeyCombo.fromKeyboardEvent(press({key: "Control", ctrlKey: true}), "other");

        expect(combo.primary).toBe(true);
        expect(combo.isBound).toBe(false);
    });
});

describe("display and accelerators", () => {
    test.each<[KeyComboParts, string]>([
        [{primary: true, key: "S"}, "Ctrl + S"],
        [{primary: true, shift: true, key: "S"}, "Ctrl + Shift + S"],
        [{alt: true, key: "E"}, "Alt + E"],
        [{key: "F5"}, "F5"],
        [{primary: true, key: "="}, "Ctrl + ="],
    ])("%s reads as '%s' off macOS", (parts, expected) => {
        expect(new KeyCombo(parts).asString("other")).toBe(expected);
    });

    test("macOS gets glyphs, in Apple's order", () => {
        expect(new KeyCombo({primary: true, shift: true, alt: true, meta: true, key: "S"}).asString("macos"))
            .toBe("⌃⌥⇧⌘S");
    });

    test("accelerators name the primary modifier the way the native menus do", () => {
        expect(new KeyCombo({primary: true, shift: true, key: "S"}).asAccelerator("other"))
            .toBe("CmdOrCtrl+Shift+S");
        expect(new KeyCombo({meta: true, key: "S"}).asAccelerator("other")).toBe("Super+S");
        expect(new KeyCombo({meta: true, key: "S"}).asAccelerator("macos")).toBe("Control+S");
    });

    test("an unassigned combination has no accelerator", () => {
        expect(new KeyCombo({primary: true}).asAccelerator("other")).toBeUndefined();
    });
});

describe("KeyCombo.matches(keyCombo)", () => {
    test("two combinations with the same parts match", () => {
        expect(new KeyCombo({primary: true, key: "S"}).matches(new KeyCombo({primary: true, key: "S"}))).toBe(true);
        expect(new KeyCombo({primary: true, key: "S"}).matches(new KeyCombo({primary: true, key: "T"}))).toBe(false);
    });

    test("a clone matches its original", () => {
        const combo = new KeyCombo({primary: true, shift: true, alt: true, meta: true, key: "S"});
        expect(combo.matches(combo.clone())).toBe(true);
    });
});
