import {bindable, ILogger, PLATFORM} from "aurelia";
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
    private contextClickTargets: Set<Element>;
    private activeClickTarget?: Element;
    private isVisible = false;

    constructor(
        private readonly element: HTMLElement,
        private readonly mutationObserver: AppMutationObserver,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @ILogger logger: ILogger) {
        super(logger);
        this.contextClickTargets = new Set<Element>();
    }

    public attached() {
        if (!this.options || !this.options.selector)
            return;

        const mouseClickHandler = ev => this.handleClickEvent(ev);
        document.addEventListener("mousedown", mouseClickHandler);
        this.disposables.push(() => document.removeEventListener("mousedown", mouseClickHandler));

        const windowBlurHandler = () => this.hideContextMenu();
        window.addEventListener("blur", windowBlurHandler);
        this.disposables.push(() => window.removeEventListener("blur", windowBlurHandler));

        PLATFORM.taskQueue.queueTask(() => this.trackContextClickTargets());
    }

    private handleClickEvent(event: MouseEvent) {
        const el = event.target as HTMLElement;
        const clickTarget = this.contextClickTargets.has(el)
            ? el
            : Array.from(this.contextClickTargets).find(t => t.contains(el));

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
        if (item.onSelected && this.activeClickTarget) {
            await item.onSelected(this.activeClickTarget);
        } else if (item.shortcut) {
            this.shortcutManager.executeShortcut(item.shortcut);
        }
    }

    private showContextMenu(x: number, y: number) {
        if (!(x >= 0 && y >= 0)) {
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

        this.isVisible = true;
    }

    private hideContextMenu() {
        this.isVisible = false;
    }

    private trackContextClickTargets() {
        const found = document.querySelectorAll(this.options.selector);
        this.contextClickTargets = new Set<Element>(Array.from(found));
        this.logger.debug(`Found and added ${found.length} elements with selector '${this.options.selector}' to click targets on initialization`);

        const mutationHandler = (mutations: MutationRecord[], observer: MutationObserver) => {
            for (const mutation of mutations) {
                if (mutation.type !== "childList") continue;

                // Check mutation target if it should be added/removed to click targets
                if (mutation.addedNodes.length > 0 && mutation.removedNodes.length === 0) {
                    const target = mutation.target as Element;
                    this.addClickTargets(target);
                } else if (mutation.removedNodes.length > 0 && mutation.addedNodes.length === 0) {
                    const target = mutation.target as Element;
                    this.removeClickTargets(target);
                }

                // Check added nodes if they should be added to click targets
                for (const addedNode of Array.from(mutation.addedNodes).map(n => n as Element)) {
                    this.addClickTargets(addedNode);
                }

                // Check removed nodes if they should be removed from click targets
                for (const removedNode of Array.from(mutation.removedNodes).map(n => n as Element)) {
                    this.removeClickTargets(removedNode);
                }
            }
        };

        const mutationObserverSubscriptionToken = this.mutationObserver.subscribe(mutationHandler);
        this.disposables.push(() => mutationObserverSubscriptionToken.dispose());
    }

    private addClickTargets(element: Element) {
        if (!element || !element.matches || this.contextClickTargets.has(element)) return;

        const clickTargets = this.findClickTargets(element);
        for (const clickTarget of clickTargets) {
            this.contextClickTargets.add(clickTarget);
            this.logger.debug(`Added element with selector '${this.options.selector}' to click targets`);
        }
    }

    private removeClickTargets(element: Element) {
        if (!element || !element.matches) return;

        const clickTargets = this.findClickTargets(element);
        for (const clickTarget of clickTargets) {
            this.contextClickTargets.delete(clickTarget);
            this.logger.debug(`Removed element with selector '${this.options.selector}' from click targets`);
        }
    }

    private findClickTargets(element: Element): Element[] {
        const matches: Element[] = [];

        if (element.matches(this.options.selector)) {
            matches.push(element);
        } else {
            const childMatches = Array.from(element.children).filter(x => x.matches(this.options.selector));
            if (childMatches.length > 0) {
                for (const match of childMatches) {
                    matches.push(match);
                }
            }
        }

        return matches;
    }
}
