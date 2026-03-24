import {Shortcut} from "@application/shortcuts/shortcut";
import {KeyCode} from "@common";

describe("Shortcut", () => {
    test("should set id and name from constructor", () => {
        const shortcut = new Shortcut("test-id", "Test Shortcut");

        expect(shortcut.id).toBe("test-id");
        expect(shortcut.name).toBe("Test Shortcut");
    });

    test("should default to not configurable and not enabled", () => {
        const shortcut = new Shortcut("id", "name");

        expect(shortcut.isConfigurable).toBe(false);
        expect(shortcut.isEnabled).toBe(false);
    });

    describe("fluent builders", () => {
        test("withKey should set key on keyCombo", () => {
            const shortcut = new Shortcut("id", "name").withKey(KeyCode.KeyF);

            expect(shortcut.keyCombo.key).toBe(KeyCode.KeyF);
        });

        test("withCtrlKey should set ctrl on keyCombo", () => {
            const shortcut = new Shortcut("id", "name").withCtrlKey();

            expect(shortcut.keyCombo.ctrl).toBe(true);
        });

        test("withAltKey should set alt on keyCombo", () => {
            const shortcut = new Shortcut("id", "name").withAltKey();

            expect(shortcut.keyCombo.alt).toBe(true);
        });

        test("withShiftKey should set shift on keyCombo", () => {
            const shortcut = new Shortcut("id", "name").withShiftKey();

            expect(shortcut.keyCombo.shift).toBe(true);
        });

        test("withMetaKey should set meta on keyCombo", () => {
            const shortcut = new Shortcut("id", "name").withMetaKey();

            expect(shortcut.keyCombo.meta).toBe(true);
        });

        test("builders should be chainable", () => {
            const shortcut = new Shortcut("id", "name")
                .withCtrlKey()
                .withShiftKey()
                .withKey(KeyCode.KeyS)
                .enabled()
                .configurable();

            expect(shortcut.keyCombo.ctrl).toBe(true);
            expect(shortcut.keyCombo.shift).toBe(true);
            expect(shortcut.keyCombo.key).toBe(KeyCode.KeyS);
            expect(shortcut.isEnabled).toBe(true);
            expect(shortcut.isConfigurable).toBe(true);
        });
    });

    describe("enabled / configurable", () => {
        test("enabled() should set isEnabled to true by default", () => {
            const shortcut = new Shortcut("id", "name").enabled();
            expect(shortcut.isEnabled).toBe(true);
        });

        test("enabled(false) should set isEnabled to false", () => {
            const shortcut = new Shortcut("id", "name").enabled().enabled(false);
            expect(shortcut.isEnabled).toBe(false);
        });

        test("configurable() should set isConfigurable to true by default", () => {
            const shortcut = new Shortcut("id", "name").configurable();
            expect(shortcut.isConfigurable).toBe(true);
        });

        test("configurable(false) should set isConfigurable to false", () => {
            const shortcut = new Shortcut("id", "name").configurable().configurable(false);
            expect(shortcut.isConfigurable).toBe(false);
        });
    });

    describe("hasAction", () => {
        test("should set the action", () => {
            const action = jest.fn();
            const shortcut = new Shortcut("id", "name").hasAction(action);

            expect(shortcut.action).toBe(action);
        });
    });

    describe("captureDefaultKeyCombo / isDefaultKeyCombo / resetKeyCombo", () => {
        test("should be default key combo before any changes", () => {
            const shortcut = new Shortcut("id", "name")
                .withCtrlKey()
                .withKey(KeyCode.KeyS)
                .captureDefaultKeyCombo();

            expect(shortcut.isDefaultKeyCombo).toBe(true);
        });

        test("should not be default after changing key combo", () => {
            const shortcut = new Shortcut("id", "name")
                .withCtrlKey()
                .withKey(KeyCode.KeyS)
                .captureDefaultKeyCombo();

            shortcut.keyCombo.withAltKey();

            expect(shortcut.isDefaultKeyCombo).toBe(false);
        });

        test("resetKeyCombo should restore to captured default", () => {
            const shortcut = new Shortcut("id", "name")
                .withCtrlKey()
                .withKey(KeyCode.KeyS)
                .captureDefaultKeyCombo();

            shortcut.keyCombo.withAltKey();
            expect(shortcut.isDefaultKeyCombo).toBe(false);

            shortcut.resetKeyCombo();
            expect(shortcut.isDefaultKeyCombo).toBe(true);
            expect(shortcut.keyCombo.alt).toBe(false);
        });

        test("captureDefaultKeyCombo should clone, not reference", () => {
            const shortcut = new Shortcut("id", "name")
                .withCtrlKey()
                .withKey(KeyCode.KeyS)
                .captureDefaultKeyCombo();

            shortcut.keyCombo.withShiftKey();

            // Default should not have shift since it was cloned before the change
            expect(shortcut.defaultKeyCombo.shift).toBe(false);
        });
    });

    describe("toString", () => {
        test("should include name and key combo", () => {
            const shortcut = new Shortcut("id", "Save")
                .withCtrlKey()
                .withKey(KeyCode.KeyS);

            const str = shortcut.toString();

            expect(str).toContain("Save");
            expect(str).toContain("(");
            expect(str).toContain(")");
        });
    });
});
