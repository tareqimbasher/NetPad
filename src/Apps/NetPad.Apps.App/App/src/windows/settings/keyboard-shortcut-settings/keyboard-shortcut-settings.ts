import {bindable, ILogger} from "aurelia";
import {
    BuiltinShortcuts,
    KeyboardShortcutConfiguration,
    KeyCombo,
    Settings,
    Shortcut,
    ViewModelBase
} from "@application";
import {KeyCode} from "@common";

export class KeyboardShortcutSettings extends ViewModelBase {
    @bindable public settings: Settings;
    public currentSettings: Readonly<Settings>;

    public shortcuts: Shortcut[] = [];

    private keyComboCaptureContainer: HTMLDivElement;
    private isEditMode: boolean;
    private shortcutInEdit?: Shortcut;
    private pressedKeyCombo?: KeyCombo;
    private pressedKeyComboMatchingShortcut?: Shortcut;

    private orderBy: "description" | "keys" | undefined;
    private orderDir: "asc" | "desc" | undefined;

    /**
     * KeyCodes that user is allowed to assign without a modifier key (ALT, CTRL...etc)
     */
    private allowedStandaloneKeyCodes = [
        KeyCode.F1,
        KeyCode.F2,
        KeyCode.F3,
        KeyCode.F4,
        KeyCode.F5,
        KeyCode.F6,
        KeyCode.F7,
        KeyCode.F8,
        KeyCode.F9,
        KeyCode.F10,
        KeyCode.F11,
        KeyCode.F12,
    ];

    constructor(currentSettings: Settings, @ILogger logger: ILogger) {
        super(logger);
        this.currentSettings = currentSettings;
    }

    public get isKeyComboValid() {
        return !!this.pressedKeyCombo?.key
            && (this.pressedKeyCombo.hasModifier || this.allowedStandaloneKeyCodes.indexOf(this.pressedKeyCombo.key) >= 0)
            && !this.shortcuts.some(s => s.keyCombo.matches(this.pressedKeyCombo!));
    }

    public get orderedShortcuts(): Shortcut[] {
        if (!this.orderBy) return this.shortcuts;

        const dir = this.orderDir === "asc" ? -1 : 1;

        return [...this.shortcuts].sort((a, b) => {
            if (this.orderBy === "description")
                return a.name < b.name ? dir : -dir;
            else
                return a.keyCombo < b.keyCombo ? dir : -dir;
        });
    }

    public attached() {
        const configShortcuts = BuiltinShortcuts
            .filter(s => s.isConfigurable)
            .map(builtInShortcut => {
                const shortcut = new Shortcut(builtInShortcut.id, builtInShortcut.name)
                    .configurable();

                shortcut.keyCombo.updateFrom(builtInShortcut.keyCombo);
                shortcut.captureDefaultKeyCombo();

                const config = this.settings.keyboardShortcuts.shortcuts
                    .find(s => s.id === builtInShortcut.id);

                if (config)
                    shortcut.keyCombo.updateFrom(config);

                return shortcut;
            });

        this.shortcuts = configShortcuts;


        const handler = (ev: KeyboardEvent) => {
            ev.stopPropagation();
            ev.preventDefault();

            this.pressedKeyCombo = KeyCombo.fromKeyboardEvent(ev);
            this.pressedKeyComboMatchingShortcut = this.shortcuts.find(s => s.keyCombo.matches(this.pressedKeyCombo!));
        };

        this.keyComboCaptureContainer.addEventListener("keydown", handler);
        this.addDisposable(() => this.keyComboCaptureContainer.removeEventListener("keydown", handler));
    }

    private order(by: "description" | "keys") {
        if (this.orderBy === by && this.orderDir === "desc") {
            this.orderBy = undefined;
            return;
        }

        this.orderDir = this.orderBy !== by ? "asc" : this.orderDir === "asc" ? "desc" : "asc";
        this.orderBy = by;
    }

    private editKeyCombo(shortcut: Shortcut) {
        this.shortcutInEdit = shortcut;
        this.isEditMode = true;
        setTimeout(() => {
            this.keyComboCaptureContainer.focus();
        }, 100);
    }

    private closeKeyComboCapture() {
        this.pressedKeyCombo = undefined;
        this.pressedKeyComboMatchingShortcut = undefined;
        this.isEditMode = false;
        this.shortcutInEdit = undefined;
    }

    private confirmKeyCombo() {
        if (!this.shortcutInEdit || !this.isKeyComboValid || !this.pressedKeyCombo) return;

        this.shortcutInEdit.keyCombo.updateFrom(this.pressedKeyCombo);

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

    private reset(shortcut: Shortcut) {
        shortcut.resetKeyCombo();

        const iConfig = this.settings.keyboardShortcuts.shortcuts.findIndex(s => s.id === shortcut.id);
        if (iConfig < 0) return;

        this.settings.keyboardShortcuts.shortcuts.splice(iConfig, 1);
    }

    // private monacoBuiltinKeybindings() {
    //     for (const defaultKeybinding of EditorUtil.getKeybindingService()._getResolver()._defaultKeybindings) {
    //         this.builtinShortcuts.push({
    //             name: defaultKeybinding.command,
    //             keyComboString: defaultKeybinding.chords.join(" ")
    //         });
    //     }
    //
    //     console.warn(EditorUtil.getKeybindingService()._getResolver()._defaultKeybindings);
    // }
}
