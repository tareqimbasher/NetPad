/**
 * Represents a main menu item as represented in the SPA app.
 */
export interface IAppMenuItem {
    id?: string,
    text?: string,
    icon?: string,
    helpText?: string,
    shortcut?: IAppShortcut,
    isDivider?: boolean;
    menuItems?: IAppMenuItem[],
}

/**
 * Represents a shortcut as represented in the SPA app.
 */
export interface IAppShortcut {
    name: string;
    isEnabled: boolean;
    keyCombo: string[];
}

export class AppMenuItemWalker {
    public static find(appMenuItems: IAppMenuItem[], predicate: (item: IAppMenuItem) => boolean): IAppMenuItem | undefined {
        let result: IAppMenuItem | undefined;

        this.walkMenuItemTree(appMenuItems, item => {
            if (predicate(item)) {
                result = item;
                return false;
            }

            return true;
        });

        return result;
    }

    public static walkMenuItemTree(appMenuItems: IAppMenuItem[], action: (item: IAppMenuItem) => boolean) {
        for (const item of appMenuItems) {
            if (!action(item)) return;

            if (item.menuItems && item.menuItems.length) {
                this.walkMenuItemTree(item.menuItems, action);
            }
        }
    }
}
