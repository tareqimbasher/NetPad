import {createDefaultKeybindings, resolveKeybindings} from "@application/keybindings/builtin-keybindings";
import {CommandIds} from "@application/commands/command-ids";
import {KeyboardShortcutConfiguration, Settings} from "@application/api";

function settingsWith(...shortcuts: Partial<KeyboardShortcutConfiguration>[]): Settings {
    return {
        keyboardShortcuts: {
            shortcuts: shortcuts.map(s => Object.assign(new KeyboardShortcutConfiguration(), s))
        }
    } as Settings;
}

describe("createDefaultKeybindings", () => {
    test("binds Save to the primary modifier and S", () => {
        const save = createDefaultKeybindings().find(k => k.commandId === CommandIds.saveScript);

        expect(save?.keyCombo.primary).toBe(true);
        expect(save?.keyCombo.key).toBe("S");
        expect(save?.isDefault).toBe(true);
    });

    test("binds Stop to Shift+F5", () => {
        const stop = createDefaultKeybindings().find(k => k.commandId === CommandIds.stopScript);

        expect(stop?.keyCombo.asString("other")).toBe("Shift + F5");
    });

    test("no two commands ship with the same combination", () => {
        const combos = createDefaultKeybindings().map(k => k.keyCombo.asString("other"));

        expect(new Set(combos).size).toBe(combos.length);
    });
});

describe("resolveKeybindings", () => {
    test("a saved combination replaces the default", () => {
        const keybindings = resolveKeybindings(settingsWith(
            {id: CommandIds.saveScript, primary: true, alt: true, key: "S"}
        ));

        const save = keybindings.find(k => k.commandId === CommandIds.saveScript)!;

        expect(save.keyCombo.asString("other")).toBe("Ctrl + Alt + S");
        expect(save.isDefault).toBe(false);
    });

    test("a saved combination for a command with no default adds a keybinding", () => {
        const keybindings = resolveKeybindings(settingsWith(
            {id: CommandIds.about, primary: true, shift: true, key: "F1"}
        ));

        const about = keybindings.find(k => k.commandId === CommandIds.about)!;

        expect(about.keyCombo.asString("other")).toBe("Ctrl + Shift + F1");
        expect(about.isDefault).toBe(false);
        expect(about.reset().keyCombo.isBound).toBe(false);
    });

    test("commands with no saved combination keep their default", () => {
        const keybindings = resolveKeybindings(settingsWith(
            {id: CommandIds.saveScript, primary: true, alt: true, key: "S"}
        ));

        expect(keybindings.filter(k => !k.isDefault)).toHaveLength(1);
    });
});
