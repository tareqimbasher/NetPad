import {bindable, ILogger} from "aurelia";
import {
    AppCommand,
    assignKeyCombo,
    holdersOf,
    ICommandRegistry,
    KeyCombo,
    Keybinding,
    resetKeyCombo,
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
        const keybindings = this.keybindings();

        const result = await this.dialogUtil.open(KeyCaptureDialog, {
            current: shortcut.keybinding.keyCombo,
            findConflict: combo => {
                const holder = holdersOf(keybindings, combo, shortcut.keybinding.commandId)[0];
                return holder && this.nameOf(holder);
            },
        });

        if (result?.status !== "ok") return;

        assignKeyCombo(keybindings, shortcut.keybinding, result.value as KeyCombo, this.settings);
    }

    public async reset(shortcut: ShortcutRow) {
        await resetKeyCombo(this.keybindings(), shortcut.keybinding, this.settings, async holder => {
            const combo = shortcut.keybinding.defaultKeyCombo.asString();
            const holderName = this.nameOf(holder);
            const answer = await this.dialogUtil.ask({
                title: "Reset Shortcut",
                message: `${combo} is now used by ${holderName}. Reset ${shortcut.name} anyway and leave ${holderName} unassigned?`,
                buttons: [
                    {text: "Cancel"},
                    {text: "Reset", isPrimary: true},
                ],
            });

            return answer.value === "Reset";
        });
    }

    private keybindings(): Keybinding[] {
        return this.shortcuts.map(s => s.keybinding);
    }

    private nameOf(keybinding: Keybinding): string {
        return this.shortcuts.find(s => s.keybinding.commandId === keybinding.commandId)?.name ?? keybinding.commandId;
    }
}
