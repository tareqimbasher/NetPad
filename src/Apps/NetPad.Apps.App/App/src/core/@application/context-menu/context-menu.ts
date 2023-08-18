import {bindable, ILogger, PLATFORM} from "aurelia";
import {
    ContextMenuOptions,
    IContextMenuItem,
    IContextMenuItemWithInternals,
    IShortcutManager,
    ViewModelBase
} from "@application";
import {AppMutationObserver, Util} from "@common";

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

        const mouseClickHandler = (ev: MouseEvent) => this.handleClickEvent(ev);
        document.addEventListener("mousedown", mouseClickHandler);
        this.addDisposable(() => document.removeEventListener("mousedown", mouseClickHandler));

        const windowBlurHandler = () => this.hideContextMenu();
        window.addEventListener("blur", windowBlurHandler);
        this.addDisposable(() => window.removeEventListener("blur", windowBlurHandler));

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
            this.unstyleActiveClickTarget();

            this.activeClickTarget = clickTarget;

            this.styleActiveClickTarget();

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
        this.unstyleActiveClickTarget();
    }

    private trackContextClickTargets() {

        const locateClickTargets = Util.debounce(this, () => {
            this.logger.debug("Locating click targets...");

            const found = document.querySelectorAll(this.options.selector);
            this.contextClickTargets.clear();
            Array.from(found).forEach(e => this.contextClickTargets.add(e));

            this.logger.debug(`Tracking ${found.length} elements with selector '${this.options.selector}' as context click targets`);
        }, 500, true);

        const mutationHandler = (mutations: MutationRecord[], observer: MutationObserver) => {
            if (mutations.some(m => m.type !== "childList")) {
                return;
            }

            locateClickTargets();
        };

        const mutationObserverSubscriptionToken = this.mutationObserver.subscribe(mutationHandler);
        this.addDisposable(mutationObserverSubscriptionToken);
    }

    private styleActiveClickTarget() {
        this.activeClickTarget?.classList.add("active-context-menu-target");
    }

    private unstyleActiveClickTarget() {
        this.activeClickTarget?.classList.remove("active-context-menu-target");
    }
}
