import {DI} from "aurelia";
import {PaletteMode} from "./palette-grammar";
import {PaletteGroup} from "./palette-item";

/**
 * Supplies rows for a single palette mode. A window offers sources for a mode by registering sources
 * for it in DI. Multiple sources can feed a single mode.
 */
export interface IPaletteSource {
    readonly mode: PaletteMode;
    /** Orders this source's groups against the other sources of the same mode (lower comes first). */
    readonly order: number;

    /**
     * The groups that this source provides. A group is a labeled group of items. Two sources
     * of a particular mode might offer the same group (same label), in which case the command palette
     * will merge them into a single group, and combine both group's items.
     *
     * Groups are shown in the order their source returns them.
     */
    getGroups(): PaletteGroup[];
}

export const IPaletteSource = DI.createInterface<IPaletteSource>();
