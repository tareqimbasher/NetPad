import {bindable, ILogger} from "aurelia";
import {
    ContextMenuOptions,
    IContextMenuItem,
    IContextMenuItemWithInternals,
    IShortcutManager,
    ViewModelBase
} from "@application";
import {AppMutationObserver} from "@common";

export class ContextMenu extends ViewModelBase {
    @bindable options: ContextMenuOptions;
    private contextClickTargets: Element[];
    private activeClickTarget?: Element;

    constructor(
        private readonly element: HTMLElement,
        private readonly mutationObserver: AppMutationObserver,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @ILogger logger: ILogger) {
        super(logger);
        this.contextClickTargets = [];
    }

    public attached() {
        if (!this.options || !this.options.selector)
            return;

        this.trackContextClickTargets();

        const mouseClickHandler = ev => this.handleClickEvent(ev);
        document.addEventListener("mousedown", mouseClickHandler);
        this.disposables.push(() => document.removeEventListener("mousedown", mouseClickHandler));

        const windowBlurHandler = () => this.hideContextMenu();
        window.addEventListener("blur", windowBlurHandler);
        this.disposables.push(() => window.removeEventListener("blur", windowBlurHandler));
    }

    private handleClickEvent(event: MouseEvent) {
        const el = event.target as HTMLElement;
        const clickTarget = this.contextClickTargets.find(t => t.contains(el));

        const showContextMenu =
            event.buttons === 2
            && el
            && clickTarget;

        if (showContextMenu) {
            this.activeClickTarget = clickTarget;

            // Evaluate which items should show
            for (const item of this.options.items) {
                (item as IContextMenuItemWithInternals)._show = !item.show || item.show(clickTarget);
            }

            this.showContextMenu(event.clientX, event.clientY);
        } else {
            this.hideContextMenu();
        }
    }

    private async selectMenuItem(item: IContextMenuItem) {
        if (item.onSelected) {
            await item.onSelected(this.activeClickTarget);
        } else if (item.shortcut) {
            this.shortcutManager.executeShortcut(item.shortcut);
        }
    }

    private showContextMenu(x: number, y: number) {
        if ((x >= 0 && y >= 0) != true) {
            return
        }

        const windowWidth = Math.floor(window.innerWidth);
        const menuWidth = this.element.clientWidth;
        const menuRightX = x + menuWidth;

        // If context menu will be right of the right edge of window, show context menu on left of mouse
        if (menuRightX > windowWidth) this.element.style.left = x - menuWidth + "px";
        else this.element.style.left = x + "px";

        const windowHeight = Math.floor(window.innerHeight);
        const menuHeight = this.element.clientHeight;
        const menuBottomY = y + menuHeight;

        // If context menu will be below the bottom edge of window, show context menu on top of mouse
        if (menuBottomY > windowHeight) this.element.style.top = y - menuHeight + "px";
        else this.element.style.top = y + "px";

        this.element.classList.add("visible");
    }

    private hideContextMenu() {
        this.element.classList.remove("visible");
    }

    private isCurrentlyShowing() {
        return this.element.classList.contains("visible");
    }

    private trackContextClickTargets() {
        this.contextClickTargets = Array.from(document.querySelectorAll(this.options.selector));

        const mutationHandler = (mutations: MutationRecord[], observer: MutationObserver) => {
            for (const mutation of mutations) {
                for (const addedNode of Array.from(mutation.addedNodes).map(n => n as Element)) {
                    if (addedNode && addedNode.matches && addedNode.matches(this.options.selector))
                        this.contextClickTargets.push(addedNode);
                }

                for (const removedNode of Array.from(mutation.removedNodes).map(n => n as Element)) {
                    if (removedNode && removedNode.matches && removedNode.matches(this.options.selector)) {
                        const ix = this.contextClickTargets.indexOf(removedNode);
                        if (ix >= 0)
                            this.contextClickTargets.splice(ix, 1);
                    }
                }
            }
        };

        const mutationObserverSubscriptionToken = this.mutationObserver.subscribe(mutationHandler);
        this.disposables.push(() => mutationObserverSubscriptionToken.dispose());
    }
}
