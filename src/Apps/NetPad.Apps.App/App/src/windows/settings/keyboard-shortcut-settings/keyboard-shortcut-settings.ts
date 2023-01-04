import {bindable} from "aurelia";
import {Settings} from "@domain";
import {KeyCode} from "@common";
import {BuiltinShortcuts, Shortcut} from "@application";

export class KeyboardShortcutSettings {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;
    public builtinShortcuts: Shortcut[] = [];

    //public keyCombo?: string;

    constructor(currentSettings: Settings) {
        // This is a monaco editor shortcut that we want to show here. It is not an app shortcut.
        this.builtinShortcuts.push(new Shortcut("Command Palette")
            .withKey(KeyCode.F1)
            .configurable(false)
            .enabled());

        this.builtinShortcuts.push(...BuiltinShortcuts);
        this.currentSettings = currentSettings;
    }

    // keyComboPressed(event: KeyboardEvent) {
    //     const code = event.code;
    //     if (["Meta", "Alt", "Control", "Shift"].find(s => code.startsWith(s))) return;
    //
    //     let combo = [];
    //     if (event.metaKey) combo.push("Meta");
    //     if (event.altKey) combo.push("Alt");
    //     if (event.ctrlKey) combo.push("Ctrl");
    //     if (event.shiftKey) combo.push("Shift");
    //     if (event.code) combo.push(event.code.replace("Key", ""));
    //
    //     this.keyCombo = combo.join(" + ").trim();
    //     return false;
    // }
}
