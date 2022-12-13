import {Shortcut} from "@application";

export class ContextMenuOptions {
    public selector: string;
    public items: IContextMenuItem[];

    constructor(selector: string, items: IContextMenuItem[]) {
        this.selector = selector;
        this.items = items ?? [];
    }
}

/**
 * A single item in a context menu.
 */
export interface IContextMenuItem {
    /** Display text. */
    text?: string | ((target: Element) => string);
    /** Display icon. */
    icon?: string;
    /** Whether this menu item is meant to act as a divider. */
    isDivider?: boolean;
    /**
     * The action that should be executed when this menu item is selected.
     */
    onSelected?: (target: Element) => Promise<unknown | void>;
    /**
     * Associated shortcut. If assigned, menu item will show shortcut keystroke next to text.
     * If onSelected is assigned, this has no affect besides showing keystroke next to text.
     */
    shortcut?: Shortcut,
    /** A function to calculate when to show this menu item. */
    show?: (target: Element) => boolean;
}

export interface IContextMenuItemWithInternals extends IContextMenuItem {
    _show: boolean;
}
