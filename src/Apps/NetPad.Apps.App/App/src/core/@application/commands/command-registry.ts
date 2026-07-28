import {IContainer, ILogger} from "aurelia";
import {WindowParams} from "@application/windowing/window-params";
import {AppCommand, CommandContext} from "./command";
import {ICommandRegistry} from "./icommand-registry";
import {createBuiltinCommands} from "./builtin-commands";

export class CommandRegistry implements ICommandRegistry {
    private readonly _commands = new Map<string, AppCommand>();
    private readonly logger: ILogger;

    constructor(@IContainer private readonly container: IContainer, @ILogger logger: ILogger) {
        this.logger = logger.scopeTo(nameof(CommandRegistry));
        this.register(...createBuiltinCommands());
    }

    public get commands(): ReadonlyArray<AppCommand> {
        return [...this._commands.values()];
    }

    public register(...commands: AppCommand[]) {
        for (const command of commands) {
            if (!command.availableIn(WindowParams.shell)) continue;

            if (this._commands.has(command.id)) {
                this.logger.warn(`A command with id "${command.id}" is already registered. Replacing it.`);
            }

            this._commands.set(command.id, command);
        }
    }

    public get(commandId: string): AppCommand | undefined {
        return this._commands.get(commandId);
    }

    public isEnabled(commandId: string): boolean {
        const command = this._commands.get(commandId);
        return !!command && command.isEnabled(new CommandContext(this.container));
    }

    public async execute(commandId: string, arg?: unknown): Promise<void> {
        const command = this._commands.get(commandId);

        if (!command) {
            this.logger.warn(`No command is registered with id "${commandId}"`);
            return;
        }

        const context = new CommandContext(this.container, arg);

        if (!command.isEnabled(context)) {
            this.logger.debug(`Command "${commandId}" is disabled and was not executed`);
            return;
        }

        this.logger.debug(`Executing command "${commandId}"`);

        try {
            await command.execute(context);
        } catch (err) {
            this.logger.error(`Command "${commandId}" failed`, err);
        }
    }
}
