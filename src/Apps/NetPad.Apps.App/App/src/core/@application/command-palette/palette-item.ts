import {IconName} from "@application/ui";
import {KeybindingCaps} from "@application/keybindings/keybinding-caps";

/**
 * One selectable row in the command palette. At most one of {@link icon}, {@link badge} and
 * {@link prefixCap} is set.
 */
export interface PaletteItem {
    /** Unique within the palette's current contents. */
    id: string;
    /** The text shown, and the text the query is matched against. */
    title: string;
    /** An icon shown ahead of the title. */
    icon?: IconName;
    /** Mono text shown ahead of the title. */
    badge?: string;
    /** A key cap ahead of the title, for the rows that teach the prefix characters. */
    prefixCap?: string;
    /** Marks the row with the unsaved-changes dot. */
    dirty?: boolean;
    /** Mono text at the end of the row: a script's path, or where a prefix leads. */
    detail?: string;
    /** The item's shortcut, as key caps at the end of the row. */
    keys?: KeybindingCaps;
    /** Leaves the palette showing after the row runs, for rows that change what the palette is searching. */
    keepOpen?: boolean;

    run(): unknown | Promise<unknown>;
}

/**
 * A labeled group of items. Groups with the same label from different sources of a mode are merged.
 */
export interface PaletteGroup {
    label?: string;
    items: PaletteItem[];
}
