import {DI} from "aurelia";

/**
 * A command box that is used to reach actions quickly.
 */
export interface ICommandPalette {
    readonly isOpen: boolean;

    /**
     * Shows the palette. Optionally, opens with a mode prefix already typed.
     */
    open(prefix?: string): void;

    close(): void;
}

export const ICommandPalette = DI.createInterface<ICommandPalette>();
