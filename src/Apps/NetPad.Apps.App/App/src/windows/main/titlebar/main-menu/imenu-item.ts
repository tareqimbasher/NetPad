import {Shortcut} from "@application";

export interface IMenuItem {
    text?: string,
    icon?: string,
    helpText?: string,
    shortcut?: Shortcut,
    isDivider?: boolean;
    click?: () => Promise<void | unknown>,
    menuItems?: IMenuItem[],
}
