import {DI} from "aurelia";
import {PaletteMode} from "./palette-grammar";
import {PaletteGroup} from "./palette-item";

/**
 * Supplies rows for one palette mode. Windows register sources in DI, and several can feed one mode.
 */
export interface IPaletteSource {
    readonly mode: PaletteMode;
    /** Orders this source's groups against the other sources of the same mode (lower comes first). */
    readonly order: number;

    /**
     * This source's groups, in display order.
     *
     * A group is a labeled group of items. Two sources of a particular mode might offer the same group (same label),
     * in which case the command palette will merge them into a single group, and combine both group's items.
     */
    getGroups(): PaletteGroup[];
}

export const IPaletteSource = DI.createInterface<IPaletteSource>();
