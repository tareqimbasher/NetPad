import {PLATFORM} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import {Settings} from "@application";
import {IMainMenuService} from "@application/main-menu/imain-menu-service";
import {IMenuItem} from "@application/main-menu/imenu-item";

interface ITopLevelMenuItem {
    element: HTMLElement;
    label: HTMLElement;
    isOpen: boolean;
}

export class MainMenu {
    private openCounter = 0;
    private topLevelMenuItems: ITopLevelMenuItem[];
    private visible: boolean;

    public get isOpen() {
        return this.openCounter > 0;
    }

    public get items() {
        return this.mainMenuService.items;
    }

    constructor(
        private readonly element: HTMLElement,
        @IMainMenuService private readonly mainMenuService: IMainMenuService,
        private readonly settings: Settings) {
        this.visible = settings.appearance.titlebar.mainMenuVisibility === "AlwaysVisible";
    }

    public attached() {
        this.topLevelMenuItems = Array.from<HTMLElement>(this.element.querySelectorAll(".top-level-menu-item"))
            .map(mi => {
                return {
                    element: mi,
                    isOpen: false,
                    label: mi.querySelector(".menu-item-label") as HTMLElement,
                };
            });


        for (const topLevelMenuItem of this.topLevelMenuItems) {
            topLevelMenuItem.element.addEventListener("shown.bs.dropdown", ev => {
                this.menuVisibilityChanged("open", topLevelMenuItem);
            });

            topLevelMenuItem.element.addEventListener("hidden.bs.dropdown", ev => {
                this.menuVisibilityChanged("close", topLevelMenuItem);
            });

            topLevelMenuItem.label.addEventListener("mouseenter", () => {
                if (!topLevelMenuItem.isOpen && this.topLevelMenuItems.some(x => x.isOpen))
                    topLevelMenuItem.label.click();
            });
        }
    }

    private openMenu() {
        this.visible = true;
        PLATFORM.taskQueue.queueTask(async () => {
            await PLATFORM.domWriteQueue.yield();
            this.topLevelMenuItems[0].label.click();
        });
    }

    @watch<MainMenu>(vm => vm.settings.appearance.titlebar.mainMenuVisibility)
    private menuVisibilityChanged(change: "open" | "close", topLevelMenuItem: ITopLevelMenuItem) {
        if (change === "open") {
            this.visible = true;
            this.openCounter++;
            topLevelMenuItem.isOpen = true;
            topLevelMenuItem.element.classList.add("open");
        } else if (change === "close") {
            this.openCounter--;
            topLevelMenuItem.isOpen = false;
            topLevelMenuItem.element.classList.remove("open");
        }

        this.visible = this.openCounter > 0 || this.settings.appearance.titlebar.mainMenuVisibility === "AlwaysVisible";
    }

    private async clickMenuItem(item: IMenuItem) {
        await this.mainMenuService.clickMenuItem(item);
    }
}
