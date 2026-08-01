import {DI, IDisposable} from "aurelia";
import {Keybinding} from "./keybinding";

export interface IKeybindingManager {
    /** The keybindings in effect, kept in sync with the user's settings. */
    readonly keybindings: ReadonlyArray<Keybinding>;

    /**
     * Starts listening for keyboard events and dispatching the commands they are bound to. Every
     * window listens and only ever dispatches the commands registered in it.
     */
    initialize(): void;

    /**
     * The keybinding for a command, if it has one.
     */
    getKeybinding(commandId: string): Keybinding | undefined;

    /**
     * The key combination that runs a command, as it is shown to the user, or undefined when the
     * command is unbound.
     */
    keysFor(commandId: string): string | undefined;

    /**
     * A command's name together with the keys that run it, for a tooltip: "Settings (F12)".
     */
    describe(commandId: string): string;

    /**
     * Registers a callback invoked whenever the set of keybindings changes. Returns a {@link IDisposable}
     * that can be used to unsubscribe.
     */
    onChanged(callback: () => void): IDisposable;
}

export const IKeybindingManager = DI.createInterface<IKeybindingManager>();
