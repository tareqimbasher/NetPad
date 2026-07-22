import {IconName, Shortcut} from "@application";

export interface IMenuItem {
    id?: string,
    text?: string,
    icon?: IconName,
    helpText?: string,
    hoverText?: string,
    shortcut?: Shortcut,
    isDivider?: boolean;
    click?: () => Promise<void | unknown>,
    menuItems?: IMenuItem[],
    disabled?: boolean,
}
