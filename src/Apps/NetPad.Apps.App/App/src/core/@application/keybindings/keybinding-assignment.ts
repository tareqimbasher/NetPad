import {KeyboardShortcutConfiguration, Settings} from "@application/api";
import {KeyCombo} from "./key-combo";
import {Keybinding} from "./keybinding";

/**
 * The keybindings, other than the one for `exceptCommandId`, that hold the combination. An unassigned
 * combination has none.
 */
export function holdersOf(keybindings: readonly Keybinding[], combo: KeyCombo, exceptCommandId?: string): Keybinding[] {
    if (!combo.isBound) return [];

    return keybindings.filter(k => k.commandId !== exceptCommandId && k.keyCombo.matches(combo));
}

/**
 * Gives a keybinding a combination and writes the change to the settings. Any other keybinding that
 * holds the combination becomes unassigned, so a combination never runs two commands.
 */
export function assignKeyCombo(
    keybindings: readonly Keybinding[],
    keybinding: Keybinding,
    combo: KeyCombo,
    settings: Settings
) {
    const holders = holdersOf(keybindings, combo, keybinding.commandId);

    for (const holder of holders) {
        holder.keyCombo = new KeyCombo();
    }

    keybinding.keyCombo = combo.clone();

    for (const changed of [keybinding, ...holders]) {
        storeOverride(changed, settings);
    }
}

/**
 * Gives a keybinding back its default combination. When another keybinding holds it, `confirmTakeover`
 * is asked first, and nothing changes unless it agrees.
 */
export async function resetKeyCombo(
    keybindings: readonly Keybinding[],
    keybinding: Keybinding,
    settings: Settings,
    confirmTakeover: (holder: Keybinding) => Promise<boolean>
) {
    const holder = holdersOf(keybindings, keybinding.defaultKeyCombo, keybinding.commandId)[0];

    if (holder && !await confirmTakeover(holder)) return;

    assignKeyCombo(keybindings, keybinding, keybinding.defaultKeyCombo, settings);
}

/**
 * Writes a keybinding to the settings in the form `resolveKeybindings` reads back: no entry when it has
 * its default, and an entry without a key when it is unassigned.
 */
function storeOverride(keybinding: Keybinding, settings: Settings) {
    const overrides = settings.keyboardShortcuts.shortcuts;
    const index = overrides.findIndex(s => s.id === keybinding.commandId);

    if (keybinding.isDefault) {
        if (index >= 0) overrides.splice(index, 1);
        return;
    }

    let config = overrides[index];

    if (!config) {
        config = new KeyboardShortcutConfiguration();
        config.id = keybinding.commandId;
        overrides.push(config);
    }

    keybinding.keyCombo.copyTo(config);
}
