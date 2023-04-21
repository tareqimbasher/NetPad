import {IMenuItem} from "./imenu-item";
import {IShortcutManager} from "@application";
import {Workbench} from "../../workbench";


export class MainMenu {
    public isExpanded = false;

    constructor(
        private readonly element: HTMLElement,
        private readonly workbench: Workbench,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
    ) {
    }

    public attached() {
        const topLevelMenuItems =
            Array.from<HTMLElement>(this.element.querySelectorAll(".top-level-menu-item"))
                .map(mi => {
                    return {
                        element: mi,
                        isOpen: false,
                        labelElement: mi.querySelector(".menu-item-label") as HTMLElement,
                    };
                });

        for (const topLevelMenuItem of topLevelMenuItems) {
            topLevelMenuItem.element.addEventListener("shown.bs.dropdown", () => {
                topLevelMenuItem.isOpen = true;
            });

            topLevelMenuItem.element.addEventListener("hidden.bs.dropdown", () => {
                topLevelMenuItem.isOpen = false;
            });

            topLevelMenuItem.labelElement.addEventListener("mouseenter", () => {
                if (!topLevelMenuItem.isOpen && topLevelMenuItems.some(x => x.isOpen))
                    topLevelMenuItem.labelElement.click();
            });
        }
    }

    public async menuItemClicked(item: IMenuItem) {
        if (item.click) {
            await item.click();
        } else if (item.shortcut) {
            this.shortcutManager.executeShortcut(item.shortcut);
        }
    }
}
