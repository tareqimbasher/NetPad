import {bindable, ILogger} from "aurelia";
import {
    AppCommand,
    createDefaultKeybindings,
    ICommandRegistry,
    KeyboardShortcutConfiguration,
    KeyCombo,
    resolveKeybindings,
    Settings,
    ViewModelBase
} from "@application";
import {DialogUtil} from "@application/dialogs/dialog-util";
import {KeyCaptureDialog} from "./key-capture-dialog/key-capture-dialog";

class ShortcutRow {
    constructor(
        public readonly command: AppCommand,
        public readonly defaultKeyCombo: KeyCombo,
        public keyCombo: KeyCombo) {
    }

    public get id(): string {
        return this.command.id;
    }

    public get name(): string {
        return this.command.title;
    }

    public get keys(): string {
        return this.keyCombo.asString();
    }

    public get isDefault(): boolean {
        return this.keyCombo.matches(this.defaultKeyCombo);
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
        const defaults = new Map(createDefaultKeybindings().map(k => [k.commandId, k.keyCombo]));
        const configured = new Map(resolveKeybindings(this.settings).map(k => [k.commandId, k.keyCombo]));

        this.shortcuts = this.commandRegistry.allCommands
            .filter(command => command.keybindable)
            .map(command => new ShortcutRow(
                command,
                defaults.get(command.id) ?? new KeyCombo(),
                (configured.get(command.id) ?? new KeyCombo()).clone()));
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
            current: shortcut.keyCombo,
            findConflict: combo => this.shortcuts.find(s => s !== shortcut && s.keyCombo.matches(combo))?.name,
        });

        if (result?.status !== "ok") return;

        const keyCombo = result.value as KeyCombo;
        shortcut.keyCombo = keyCombo;

        let config = this.settings.keyboardShortcuts.shortcuts.find(s => s.id === shortcut.id);

        if (!config) {
            config = new KeyboardShortcutConfiguration();
            config.id = shortcut.id;
            this.settings.keyboardShortcuts.shortcuts.push(config);
        }

        keyCombo.copyTo(config);
    }

    public reset(shortcut: ShortcutRow) {
        shortcut.keyCombo = shortcut.defaultKeyCombo.clone();

        const iConfig = this.settings.keyboardShortcuts.shortcuts.findIndex(s => s.id === shortcut.id);
        if (iConfig < 0) return;

        this.settings.keyboardShortcuts.shortcuts.splice(iConfig, 1);
    }
}
