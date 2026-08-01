import {IContainer} from "aurelia";
import {IconName} from "@application/ui";
import {IEventBus} from "@application/events/ievent-bus";
import {ISession} from "@application/sessions/isession";
import {ShellType} from "@application/windowing/shell-type";
import {WindowId} from "@application/windowing/window-id";

/**
 * The group a command is filed under wherever commands are listed.
 */
export type CommandCategory = "File" | "Edit" | "View" | "Scripts" | "Settings" | "Tools" | "Help";

/**
 * What a command is handed when it runs. `arg` is the command-specific target a caller can supply
 * (ex: a script id, a tab id). Commands that accept one should document what they expect.
 */
export class CommandContext {
    public readonly session: ISession;
    public readonly eventBus: IEventBus;

    constructor(public readonly container: IContainer, public readonly arg?: unknown) {
        this.session = container.get(ISession);
        this.eventBus = container.get(IEventBus);
    }

    public argAs<T>(): T | undefined {
        return this.arg as T | undefined;
    }
}

export interface CommandDefinition {
    id: string;
    title: string;
    category: CommandCategory;
    icon?: IconName;
    /** Longer text explaining what the command does. */
    description?: string;
    /** The editor action this command triggers. */
    monacoCommandId?: string;
    /** The shells this command exists in. Undefined means all of them. */
    shells?: ShellType[];
    /** The windows this command exists in. Undefined means all of them. */
    windows?: WindowId[];
    /** Whether a key combination can be assigned to this command. Defaults to true. */
    keybindable?: boolean;
    /** Whether the command can run right now. Commands without this defined are always available. */
    isEnabled?: (context: CommandContext) => boolean;
    execute: (context: CommandContext) => unknown | Promise<unknown>;
}

/**
 * An action the app can be asked to do.
 */
export class AppCommand {
    public readonly id: string;
    public readonly title: string;
    public readonly category: CommandCategory;
    public readonly icon?: IconName;
    public readonly description?: string;
    public readonly monacoCommandId?: string;
    public readonly shells?: ShellType[];
    public readonly windows?: WindowId[];
    public readonly keybindable: boolean;

    private readonly _isEnabled?: (context: CommandContext) => boolean;
    private readonly _execute: (context: CommandContext) => unknown | Promise<unknown>;

    constructor(definition: CommandDefinition) {
        this.id = definition.id;
        this.title = definition.title;
        this.category = definition.category;
        this.icon = definition.icon;
        this.description = definition.description;
        this.monacoCommandId = definition.monacoCommandId;
        this.shells = definition.shells;
        this.windows = definition.windows;
        this.keybindable = definition.keybindable ?? true;
        this._isEnabled = definition.isEnabled;
        this._execute = definition.execute;
    }

    public availableInShell(shell: ShellType): boolean {
        return !this.shells || this.shells.includes(shell);
    }

    public availableInWindow(window: WindowId): boolean {
        return !this.windows || this.windows.includes(window);
    }

    public isEnabled(context: CommandContext): boolean {
        return !this._isEnabled || this._isEnabled(context);
    }

    public async execute(context: CommandContext): Promise<void> {
        await this._execute(context);
    }

    public toString(): string {
        return `${this.category}: ${this.title}`;
    }
}
