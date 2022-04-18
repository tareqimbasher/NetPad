import {Shortcut} from "@domain";

export class ContextMenuOptions {
    public items: IContextMenuItem[];

    constructor(public selector: string) {
        this.items = [];
    }
}

export interface IContextMenuItem {
    text?: string;
    icon?: string;
    shortcut?: Shortcut,
    isDivider?: boolean;
    selected?: (target: Element) => Promise<any>;
    show?: (target: Element) => boolean;
}

export interface IContextMenuItemWithInternals extends IContextMenuItem {
    _show: boolean;
}
