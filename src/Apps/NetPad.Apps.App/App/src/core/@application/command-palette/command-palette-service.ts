import {ICommandPalette} from "./icommand-palette";

/**
 * Holds the command palette's state.
 */
export class CommandPaletteService implements ICommandPalette {
    public isOpen = false;
    public prefix = "";

    /**
     * Counts how many times the palette is opened. Used to observe "opening" the palette when it is already
     * open as a way to react to requests to re-open the palette in a different mode.
     */
    public openCount = 0;

    public open(prefix = "") {
        this.prefix = prefix;
        this.isOpen = true;
        this.openCount++;
    }

    public close() {
        this.isOpen = false;
    }
}
