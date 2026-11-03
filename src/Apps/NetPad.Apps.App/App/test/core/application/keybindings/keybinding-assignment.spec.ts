import {Settings} from "@application/api";
import {CommandIds} from "@application/commands/command-ids";
import {resolveKeybindings} from "@application/keybindings/builtin-keybindings";
import {KeyCombo} from "@application/keybindings/key-combo";
import {Keybinding} from "@application/keybindings/keybinding";
import {assignKeyCombo, holdersOf, resetKeyCombo} from "@application/keybindings/keybinding-assignment";

const commandIds = [CommandIds.saveScript, CommandIds.saveAllScripts, CommandIds.runScript, CommandIds.openScriptReferences];

function keybindingsFor(settings: Settings): Keybinding[] {
    const resolved = new Map(resolveKeybindings(settings).map(k => [k.commandId, k]));
    return commandIds.map(id => resolved.get(id) ?? new Keybinding(id));
}

const ctrlS = new KeyCombo({primary: true, key: "S"});

describe("keybinding assignment", () => {
    let settings: Settings;
    let keybindings: Keybinding[];
    const binding = (id: string) => keybindings.find(k => k.commandId === id)!;
    const override = (id: string) => settings.keyboardShortcuts.shortcuts.find(s => s.id === id);

    beforeEach(() => {
        settings = new Settings();
        keybindings = keybindingsFor(settings);
    });

    afterEach(() => {
        for (const k of keybindings.filter(k => k.keyCombo.isBound)) {
            expect(holdersOf(keybindings, k.keyCombo, k.commandId)).toEqual([]);
        }

        const stored = keybindingsFor(Settings.fromJS(settings.toJSON()));
        expect(stored.map(k => k.keyCombo.asString())).toEqual(keybindings.map(k => k.keyCombo.asString()));
    });

    test("replacing a shortcut that holds the combination by default stores it with no key", () => {
        assignKeyCombo(keybindings, binding(CommandIds.runScript), ctrlS, settings);

        expect(binding(CommandIds.runScript).keyCombo.matches(ctrlS)).toBe(true);
        expect(binding(CommandIds.saveScript).keyCombo.isBound).toBe(false);
        expect(override(CommandIds.saveScript)).toBeDefined();
        expect(override(CommandIds.saveScript)!.key).toBeUndefined();
    });

    test("replacing a shortcut whose own default is unassigned removes its override", () => {
        const f9 = new KeyCombo({key: "F9"});
        assignKeyCombo(keybindings, binding(CommandIds.openScriptReferences), f9, settings);
        expect(override(CommandIds.openScriptReferences)).toBeDefined();

        assignKeyCombo(keybindings, binding(CommandIds.runScript), f9, settings);

        expect(binding(CommandIds.openScriptReferences).keyCombo.isBound).toBe(false);
        expect(override(CommandIds.openScriptReferences)).toBeUndefined();
    });

    test("assigning a shortcut its own default removes its override", () => {
        assignKeyCombo(keybindings, binding(CommandIds.runScript), new KeyCombo({key: "F9"}), settings);
        assignKeyCombo(keybindings, binding(CommandIds.runScript), new KeyCombo({key: "F5"}), settings);

        expect(override(CommandIds.runScript)).toBeUndefined();
    });

    test("a reset whose default is taken asks, and changes nothing until it is confirmed", async () => {
        assignKeyCombo(keybindings, binding(CommandIds.saveScript), new KeyCombo({primary: true, key: "J"}), settings);
        assignKeyCombo(keybindings, binding(CommandIds.runScript), ctrlS, settings);

        const asked: string[] = [];
        await resetKeyCombo(keybindings, binding(CommandIds.saveScript), settings, async holder => {
            asked.push(holder.commandId);
            return false;
        });

        expect(asked).toEqual([CommandIds.runScript]);
        expect(binding(CommandIds.saveScript).keyCombo.asString()).toBe(new KeyCombo({primary: true, key: "J"}).asString());
        expect(binding(CommandIds.runScript).keyCombo.matches(ctrlS)).toBe(true);

        await resetKeyCombo(keybindings, binding(CommandIds.saveScript), settings, async () => true);

        expect(binding(CommandIds.saveScript).isDefault).toBe(true);
        expect(override(CommandIds.saveScript)).toBeUndefined();
        expect(binding(CommandIds.runScript).keyCombo.isBound).toBe(false);
    });

    test("a reset whose default is free does not ask", async () => {
        assignKeyCombo(keybindings, binding(CommandIds.runScript), new KeyCombo({key: "F9"}), settings);

        const confirm = jest.fn(async () => true);
        await resetKeyCombo(keybindings, binding(CommandIds.runScript), settings, confirm);

        expect(confirm).not.toHaveBeenCalled();
        expect(override(CommandIds.runScript)).toBeUndefined();
    });

    test("unassigning a shortcut whose default is a combination stores it with no key", () => {
        assignKeyCombo(keybindings, binding(CommandIds.saveAllScripts), new KeyCombo(), settings);

        expect(binding(CommandIds.saveAllScripts).keyCombo.isBound).toBe(false);
        expect(override(CommandIds.saveAllScripts)!.key).toBeUndefined();
    });

    test("unassigning a shortcut whose default is unassigned removes its override", () => {
        assignKeyCombo(keybindings, binding(CommandIds.openScriptReferences), new KeyCombo({key: "F9"}), settings);
        assignKeyCombo(keybindings, binding(CommandIds.openScriptReferences), new KeyCombo(), settings);

        expect(override(CommandIds.openScriptReferences)).toBeUndefined();
    });

    test("an unassigned combination is held by no one", () => {
        assignKeyCombo(keybindings, binding(CommandIds.saveAllScripts), new KeyCombo(), settings);

        expect(holdersOf(keybindings, new KeyCombo())).toEqual([]);
    });
});
