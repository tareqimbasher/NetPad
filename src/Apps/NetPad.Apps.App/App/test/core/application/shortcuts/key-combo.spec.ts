import {KeyCombo} from "@application/shortcuts/key-combo";
import {KeyCode} from "@common";

describe("KeyCombo.matches()", () => {
    function createComboInstances() {
        return [
            () => new KeyCombo(),
            () => new KeyCombo().withKey(KeyCode.KeyF),
            () => new KeyCombo().withMetaKey(),
            () => new KeyCombo().withAltKey(),
            () => new KeyCombo().withShiftKey(),
            () => new KeyCombo().withCtrlKey(),
            () => new KeyCombo().withKey(KeyCode.KeyF).withShiftKey(),
            () => new KeyCombo().withKey(KeyCode.KeyF).withAltKey().withShiftKey(),
            () => new KeyCombo().withKey(KeyCode.KeyF).withMetaKey().withShiftKey(),
            () => new KeyCombo().withKey(KeyCode.KeyF).withMetaKey().withAltKey().withShiftKey().withCtrlKey(),
        ].map(f => f());
    }

    test.each(createComboInstances())("Using KeyCombo params: '%s' should match another key combo with the same bindings", (combo) => {
        const matchAgainst = createComboInstances();
        const matches = matchAgainst.filter(x => combo.matches(x));

        expect(matches.length).toBe(1);
        expect(matches[0].toString()).toEqual(combo.toString());
    });

    test.each(createComboInstances())("Using key params: '%s' should match another key combo with the same bindings", (combo) => {
        const matchAgainst = createComboInstances();

        const matches = matchAgainst.filter(x => x.matches(
            combo.key,
            combo.ctrl,
            combo.alt,
            combo.shift,
            combo.meta
        ));

        expect(matches.length).toBe(1);
        expect(matches[0].toString()).toEqual(combo.toString());
    });

    test.each(createComboInstances())("Cloned '%s' should match original", (combo) => {
        const clone = combo.clone();

        expect(combo.matches(clone)).toBe(true);
    });
});

describe("KeyCombo.hasModifier", () => {
    test.each([
        new KeyCombo().withMetaKey(),
        new KeyCombo().withAltKey(),
        new KeyCombo().withShiftKey(),
        new KeyCombo().withCtrlKey(),
        new KeyCombo().withKey(KeyCode.KeyF).withShiftKey(),
        new KeyCombo().withKey(KeyCode.KeyF).withAltKey().withShiftKey(),
        new KeyCombo().withKey(KeyCode.KeyF).withMetaKey().withShiftKey(),
        new KeyCombo().withKey(KeyCode.KeyF).withMetaKey().withAltKey().withShiftKey().withCtrlKey(),
    ])("hasModifier should return true for '%s'", (combo) => {
        expect(combo.hasModifier).toBe(true);
    });

    test.each([
        new KeyCombo(),
        new KeyCombo().withKey(KeyCode.KeyF),
    ])("hasModifier should return false for '%s'", (combo) => {
        expect(combo.hasModifier).toBe(false);
    });
});
