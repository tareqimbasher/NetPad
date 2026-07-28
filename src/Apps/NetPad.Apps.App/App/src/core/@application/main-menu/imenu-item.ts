import {IconName} from "@application";

export interface IMenuItem {
    id?: string,
    text?: string,
    icon?: IconName,
    hoverText?: string,
    /**
     * The command this item runs. Its key hint and enabled state follow the command.
     */
    commandId?: string,
    isDivider?: boolean;
    /**
     * What this item does when clicked, for items that are data rather than commands (ex: a recent file path).
     */
    click?: () => Promise<void | unknown>,
    menuItems?: IMenuItem[],
    disabled?: boolean,
    /**
     * The item's key combination as it is shown to the user (derived from the command, not authored).
     */
    keyLabel?: string,
    /**
     * The item's key combination as a native-menu accelerator (derived from the command, not authored).
     */
    accelerator?: string,
}
