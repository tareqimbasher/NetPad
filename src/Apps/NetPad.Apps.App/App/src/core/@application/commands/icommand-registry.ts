import {DI} from "aurelia";
import {AppCommand} from "./command";

/**
 * The central registry for all commands. Everything that can ask NetPad to do something (menus, toolbars,
 * keybindings, the command palette) goes through this registry.
 */
export interface ICommandRegistry {
    /** The commands the current window can run, in registration order. */
    readonly commands: ReadonlyArray<AppCommand>;

    /**
     * Every command this shell has, including the ones only other windows can run, in registration
     * order.
     */
    readonly allCommands: ReadonlyArray<AppCommand>;

    register(...commands: AppCommand[]): void;

    get(commandId: string): AppCommand | undefined;

    /** Whether the command exists and can run right now. */
    isEnabled(commandId: string): boolean;

    /**
     * Runs a command. Does nothing if the command does not exist in this shell or is disabled.
     * @param commandId The command id to execute.
     * @param arg A command-specific target. See the command's definition for what it accepts.
     */
    execute(commandId: string, arg?: unknown): Promise<void>;
}

export const ICommandRegistry = DI.createInterface<ICommandRegistry>();
