import {IconName} from "@application/ui";
import {KeybindingCaps} from "@application/keybindings/keybinding-caps";

/**
 * One selectable row in the command palette.
 *
 * An item can carry a leading mark: an {@link icon}, a {@link badge}, or a {@link prefixCap}. At most
 * one of them is set. An item with none keeps an empty slot so titles stay aligned down the list.
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
    /** A key cap ahead of the title, for the rows that teach the grammar. */
    prefixCap?: string;
    /** Marks the row with the unsaved-changes dot. */
    dirty?: boolean;
    /** Mono text at the end of the row: a script's path, or where a prefix leads. */
    detail?: string;
    /** Key caps at the end of the row: the key combo shortcut for this item, one group per chord stroke. */
    keys?: KeybindingCaps;
    /** Leaves the palette showing after the row runs, for rows that change what the palette is searching. */
    keepOpen?: boolean;

    run(): unknown | Promise<unknown>;
}

/**
 * A labeled group of items. Two sources of a particular mode might offer the same group (same label),
 * in which case the command palette will merge them into a single group, and combine both group's items.
 *
 * Groups are shown in the order their source returns them.
 */
export interface PaletteGroup {
    label?: string;
    items: PaletteItem[];
}
