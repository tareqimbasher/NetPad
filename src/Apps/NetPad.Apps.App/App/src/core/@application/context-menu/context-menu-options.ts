import {IconName} from "@application";

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
    icon?: IconName;
    /** Whether this menu item is meant to act as a divider. */
    isDivider?: boolean;
    /**
     * The action that should be executed when this menu item is selected.
     */
    onSelected?: (target: Element) => Promise<unknown | void>;
    /**
     * The command this item runs, and whose key combination it shows next to the text. When
     * onSelected is also assigned it wins, and the command only contributes the key combination.
     */
    commandId?: string;
    /**
     * The target to run the command against. Without one the command acts on whatever is active.
     */
    commandArg?: (target: Element) => unknown;
    /** A function to calculate when to show this menu item. */
    show?: (target: Element) => boolean;
}

export interface IContextMenuItemComputedOptions extends IContextMenuItem {
    visible: boolean;
    /** The item's key combination as it is shown to the user. Derived from the command. */
    keyLabel?: string;
}
