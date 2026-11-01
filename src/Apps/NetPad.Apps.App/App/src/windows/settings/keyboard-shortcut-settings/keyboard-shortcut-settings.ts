import {bindable, ILogger} from "aurelia";
import {
    AppCommand,
    ICommandRegistry,
    KeyboardShortcutConfiguration,
    KeyCombo,
    Keybinding,
    resolveKeybindings,
    Settings,
    ViewModelBase
} from "@application";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {KeyCaptureDialog} from "./key-capture-dialog/key-capture-dialog";

class ShortcutRow {
    constructor(public readonly command: AppCommand, public readonly keybinding: Keybinding) {
    }

    public get name(): string {
        return this.command.title;
    }

    public get keys(): string {
        return this.keybinding.keyCombo.asString();
    }
}

export class KeyboardShortcutSettings extends ViewModelBase {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    public shortcuts: ShortcutRow[] = [];
    public filter = "";

    private orderBy: "description" | "keys" | undefined;
    private orderDir: "asc" | "desc" | undefined;

    constructor(
        currentSettings: Settings,
        @ICommandRegistry private readonly commandRegistry: ICommandRegistry,
        private readonly dialogUtil: DialogUtil,
        @ILogger logger: ILogger) {
        super(logger);
        this.currentSettings = currentSettings;
    }

    public get visibleShortcuts(): ShortcutRow[] {
        const ordered = this.orderedShortcuts;
        const filter = this.filter.trim().toLowerCase();

        if (!filter) return ordered;

        return ordered.filter(s =>
            s.name.toLowerCase().includes(filter)
            || s.keys.toLowerCase().includes(filter));
    }

    public get orderedShortcuts(): ShortcutRow[] {
        if (!this.orderBy) return this.shortcuts;

        const dir = this.orderDir === "asc" ? -1 : 1;

        return [...this.shortcuts].sort((a, b) => {
            if (this.orderBy === "description")
                return a.name < b.name ? dir : -dir;
            else
                return a.keys < b.keys ? dir : -dir;
        });
    }

    public attached() {
        this.buildShortcuts();
    }

    private buildShortcuts() {
        const keybindings = new Map(resolveKeybindings(this.settings).map(k => [k.commandId, k]));

        this.shortcuts = this.commandRegistry.allCommands
            .filter(command => command.keybindable)
            .map(command => new ShortcutRow(command, keybindings.get(command.id) ?? new Keybinding(command.id)));
    }

    public order(by: "description" | "keys") {
        if (this.orderBy === by && this.orderDir === "desc") {
            this.orderBy = undefined;
            return;
        }

        this.orderDir = this.orderBy !== by ? "asc" : this.orderDir === "asc" ? "desc" : "asc";
        this.orderBy = by;
    }

    public async editKeyCombo(shortcut: ShortcutRow) {
        const result = await this.dialogUtil.open(KeyCaptureDialog, {
            current: shortcut.keybinding.keyCombo,
            findConflict: combo => this.shortcuts.find(s => s !== shortcut && s.keybinding.keyCombo.matches(combo))?.name,
        });

        if (result?.status !== "ok") return;

        const keyCombo = result.value as KeyCombo;
        shortcut.keybinding.keyCombo = keyCombo;

        let config = this.settings.keyboardShortcuts.shortcuts.find(s => s.id === shortcut.command.id);

        if (!config) {
            config = new KeyboardShortcutConfiguration();
            config.id = shortcut.command.id;
            this.settings.keyboardShortcuts.shortcuts.push(config);
        }

        keyCombo.copyTo(config);
    }

    public reset(shortcut: ShortcutRow) {
        shortcut.keybinding.reset();

        const iConfig = this.settings.keyboardShortcuts.shortcuts.findIndex(s => s.id === shortcut.command.id);
        if (iConfig < 0) return;

        this.settings.keyboardShortcuts.shortcuts.splice(iConfig, 1);
    }
}
