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

    private keyComboCaptureContainer: HTMLDivElement;
    private isEditMode: boolean;
    private shortcutInEdit?: ShortcutRow;
    private pressedKeyCombo?: KeyCombo;
    private pressedKeyComboMatchingShortcut?: ShortcutRow;

    private orderBy: "description" | "keys" | undefined;
    private orderDir: "asc" | "desc" | undefined;

    /**
     * Keys a user may assign without holding a modifier.
     */
    private allowedStandaloneKeys = [
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
    ];

    constructor(
        currentSettings: Settings,
        @ICommandRegistry private readonly commandRegistry: ICommandRegistry,
        @ILogger logger: ILogger) {
        super(logger);
        this.currentSettings = currentSettings;
    }

    public get isKeyComboValid() {
        return !!this.pressedKeyCombo?.key
            && (this.pressedKeyCombo.hasModifier || this.allowedStandaloneKeys.includes(this.pressedKeyCombo.key))
            && !this.pressedKeyComboMatchingShortcut;
    }

    public get isKeyComboIncomplete() {
        return !!this.pressedKeyCombo && !this.pressedKeyCombo.key;
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

        const handler = (ev: KeyboardEvent) => {
            ev.stopPropagation();
            ev.preventDefault();

            if (ev.key === "Escape") {
                this.closeKeyComboCapture();
                return;
            }

            this.pressedKeyCombo = KeyCombo.fromKeyboardEvent(ev);
            this.pressedKeyComboMatchingShortcut = this.pressedKeyCombo.isBound
                ? this.shortcuts.find(s => s !== this.shortcutInEdit && s.keyCombo.matches(this.pressedKeyCombo!))
                : undefined;
        };

        this.keyComboCaptureContainer.addEventListener("keydown", handler);
        this.addDisposable(() => this.keyComboCaptureContainer.removeEventListener("keydown", handler));
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

    public editKeyCombo(shortcut: ShortcutRow) {
        this.shortcutInEdit = shortcut;
        this.isEditMode = true;
        setTimeout(() => {
            this.keyComboCaptureContainer.focus();
        }, 100);
    }

    public closeKeyComboCapture() {
        this.pressedKeyCombo = undefined;
        this.pressedKeyComboMatchingShortcut = undefined;
        this.isEditMode = false;
        this.shortcutInEdit = undefined;
    }

    public confirmKeyCombo() {
        if (!this.shortcutInEdit || !this.isKeyComboValid || !this.pressedKeyCombo) return;

        this.shortcutInEdit.keyCombo = this.pressedKeyCombo.clone();

        let config = this.settings.keyboardShortcuts.shortcuts
            .find(s => s.id === this.shortcutInEdit!.id);

        if (config) {
            this.pressedKeyCombo.copyTo(config);
        } else {
            config = new KeyboardShortcutConfiguration();
            config.id = this.shortcutInEdit.id;
            this.pressedKeyCombo.copyTo(config);

            this.settings.keyboardShortcuts.shortcuts.push(config);
        }

        this.closeKeyComboCapture();
    }

    public reset(shortcut: ShortcutRow) {
        shortcut.keyCombo = shortcut.defaultKeyCombo.clone();

        const iConfig = this.settings.keyboardShortcuts.shortcuts.findIndex(s => s.id === shortcut.id);
        if (iConfig < 0) return;

        this.settings.keyboardShortcuts.shortcuts.splice(iConfig, 1);
    }
}
