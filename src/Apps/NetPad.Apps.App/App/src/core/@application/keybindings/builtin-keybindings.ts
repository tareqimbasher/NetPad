import {Settings} from "@application/api";
import {CommandIds} from "@application/commands/command-ids";
import {KeyCombo, KeyComboParts} from "./key-combo";
import {Keybinding} from "./keybinding";

/**
 * The key combinations NetPad ships with. Commands that are not listed start unbound, a user can
 * still assign a combination to any keybindable command.
 */
const defaults: ReadonlyArray<[CommandIds, KeyComboParts]> = [
    [CommandIds.openCommandPalette, {key: "F1"}],
    [CommandIds.goToScript, {primary: true, key: "T"}],
    [CommandIds.switchToLastActiveScript, {primary: true, key: "Tab"}],
    [CommandIds.newScript, {primary: true, key: "N"}],
    [CommandIds.openFile, {primary: true, key: "O"}],
    [CommandIds.closeScript, {primary: true, key: "W"}],
    [CommandIds.saveScript, {primary: true, key: "S"}],
    [CommandIds.saveAllScripts, {primary: true, shift: true, key: "S"}],
    [CommandIds.runScript, {key: "F5"}],
    [CommandIds.stopScript, {shift: true, key: "F5"}],
    [CommandIds.openScriptProperties, {key: "F4"}],
    [CommandIds.openSettings, {primary: true, key: ","}],
    [CommandIds.toggleOutputPane, {primary: true, key: "R"}],
    [CommandIds.toggleExplorerPane, {alt: true, key: "E"}],
    [CommandIds.toggleNamespacesPane, {alt: true, key: "N"}],
    [CommandIds.reloadWindow, {primary: true, shift: true, key: "R"}],
    [CommandIds.zoomIn, {primary: true, key: "="}],
    [CommandIds.zoomOut, {primary: true, key: "-"}],
    [CommandIds.zoomReset, {primary: true, key: "0"}],
    [CommandIds.toggleVimMode, {alt: true, key: "V"}],
];

export function createDefaultKeybindings(): Keybinding[] {
    return defaults.map(([commandId, parts]) => new Keybinding(commandId, new KeyCombo(parts)));
}

/**
 * The keybindings in effect: the defaults, with the user's saved combinations applied over them.
 * A saved combination for a command that has no default adds a keybinding for it.
 */
export function resolveKeybindings(settings: Settings): Keybinding[] {
    const keybindings = createDefaultKeybindings();
    const byCommandId = new Map(keybindings.map(k => [k.commandId, k]));

    for (const config of settings.keyboardShortcuts.shortcuts) {
        let keybinding = byCommandId.get(config.id);

        if (!keybinding) {
            keybinding = new Keybinding(config.id);
            byCommandId.set(config.id, keybinding);
            keybindings.push(keybinding);
        }

        keybinding.keyCombo.updateFrom(config);
    }

    return keybindings;
}
