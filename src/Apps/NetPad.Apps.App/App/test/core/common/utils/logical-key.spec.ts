import {isModifierKey, normalizeLogicalKey} from "@common/utils/logical-key";

describe("normalizeLogicalKey", () => {
    test.each([
        ["s", "S"],
        ["S", "S"],
        ["z", "Z"],
        ["5", "5"],
        ["=", "="],
        ["-", "-"],
        ["/", "/"],
        ["ü", "Ü"],
    ])("upper-cases the single character '%s'", (raw, expected) => {
        expect(normalizeLogicalKey(raw)).toBe(expected);
    });

    test.each(["F5", "Tab", "Enter", "Escape", "ArrowUp", "PageDown", "Delete"])(
        "leaves the named key '%s' as it is", (raw) => {
            expect(normalizeLogicalKey(raw)).toBe(raw);
        });

    test("spells out the space bar", () => {
        expect(normalizeLogicalKey(" ")).toBe("Space");
    });

    test.each(["Control", "Shift", "Alt", "Meta", "CapsLock", "AltGraph", "Dead", "Unidentified", "", undefined])(
        "returns nothing for '%s', which cannot end a combination", (raw) => {
            expect(normalizeLogicalKey(raw)).toBeUndefined();
        });
});

describe("isModifierKey", () => {
    test.each(["Control", "Shift", "Alt", "Meta", "AltGraph"])("'%s' is a modifier", (key) => {
        expect(isModifierKey(key)).toBe(true);
    });

    test.each(["A", "F5", "Tab", "Escape"])("'%s' is not a modifier", (key) => {
        expect(isModifierKey(key)).toBe(false);
    });
});
