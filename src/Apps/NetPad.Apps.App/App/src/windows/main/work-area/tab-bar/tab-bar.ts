import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import dragula from "dragula";
import {Util} from "@common";
import {
    ContextMenuOptions,
    CreateScriptDto,
    IScriptService,
    IShortcutManager,
    Settings,
    ShortcutIds,
    ViewModelBase
} from "@application";
import {ViewableObject} from "../viewers/viewable-object";
import {ViewerHost} from "../viewers/viewer-host";

export class TabBar extends ViewModelBase {
    @bindable viewables: ReadonlySet<ViewableObject>;
    @bindable active?: ViewableObject | null;
    @bindable viewerHost: ViewerHost;
    public tabContextMenuOptions: ContextMenuOptions;
    private viewablesOrder: string[];
    private tabContainer: HTMLElement;

    constructor(
        private readonly element: Element,
        @IScriptService private readonly scriptService: IScriptService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @ILogger logger: ILogger,
        private readonly settings: Settings,
    ) {
        super(logger);
    }

    public get orderedViewables() {
        if (!this.viewablesOrder || !this.viewablesOrder.length) return this.viewables;

        const sorted = [...this.viewables]
            .sort((a, b) => {
                const ixA = this.viewablesOrder.indexOf(a.id);
                const ixB = this.viewablesOrder.indexOf(b.id);

                // Viewables not in the saved order (ex: newly created tabs) go to the end.
                if (ixA < 0 && ixB < 0) return 0;
                if (ixA < 0) return 1;
                if (ixB < 0) return -1;

                return ixA - ixB;
            });
        return new Set(sorted);
    }

    public binding() {
        this.tabContextMenuOptions = new ContextMenuOptions(".view-tab:not(.new-tab)", [
            {
                icon: "run-icon",
                text: "Run",
                shortcut: this.shortcutManager.getShortcut(ShortcutIds.openCommandPalette),
                show: (clickTarget) => this.getViewable(clickTarget).canRun(),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).run()
            },
            {
                icon: "stop-icon text-red",
                text: "Stop",
                show: (clickTarget) => this.getViewable(clickTarget).canStop(),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).stop()
            },
            {
                icon: "rename-icon",
                text: "Rename",
                show: (clickTarget) => this.getViewable(clickTarget).canRename(),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).rename()
            },
            {
                icon: "duplicate-icon",
                text: "Duplicate",
                show: (clickTarget) => this.getViewable(clickTarget).canDuplicate(),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).duplicate()
            },
            {
                icon: "save-icon",
                text: "Save",
                shortcut: this.shortcutManager.getShortcut(ShortcutIds.saveDocument),
                show: (clickTarget) => this.getViewable(clickTarget).canSave(),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).save()
            },
            {
                icon: "properties-icon",
                text: "Properties",
                shortcut: this.shortcutManager.getShortcut(ShortcutIds.openDocumentProperties),
                show: (clickTarget) => this.getViewable(clickTarget).canOpenProperties(),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).openProperties()
            },
            {
                isDivider: true
            },
            {
                icon: "open-folder-icon",
                text: "Open Containing Folder",
                show: (clickTarget) => this.getViewable(clickTarget).canOpenContainingFolder(),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).openContainingFolder(),
            },
            {
                icon: "",
                text: "Close Other Tabs",
                onSelected: async (clickTarget) => {
                    const clicked = this.getViewable(clickTarget);
                    for (const viewable of this.viewables) {
                        if (viewable != clicked)
                            await this.close(viewable);
                    }
                }
            },
            {
                icon: "",
                text: "Close All Tabs",
                onSelected: async () => {
                    for (const viewable of this.viewables) {
                        await this.close(viewable);
                    }
                }
            },
            {
                icon: "close-icon",
                text: "Close",
                shortcut: this.shortcutManager.getShortcut(ShortcutIds.closeDocument),
                onSelected: async (clickTarget) => await this.close(this.getViewable(clickTarget))
            }
        ]);
    }

    public attached() {
        this.loadViewablesOrder();
        this.initializeTabDragAndDrop();

        setTimeout(() => this.scrollTabIntoView(this.active), 200);
    }

    private activeChanged(newActive?: ViewableObject, oldActive?: ViewableObject) {
        this.scrollTabIntoView(newActive);
    }

    private scrollTabIntoView(viewable: ViewableObject | undefined | null) {
        if (!viewable) return;

        const tab = this.element.querySelector(`.view-tab[data-id='${viewable.id}']`);

        if (tab) {
            tab.scrollIntoView();
        }
    }

    public async new() {
        await this.scriptService.create(new CreateScriptDto());
    }

    public async activate(viewable: ViewableObject) {
        await viewable.activate(this.viewerHost);
    }

    public async close(viewable: ViewableObject, event?: MouseEvent) {
        if (event && event.button !== 1) { // Only mouse-wheel click should close tab
            return;
        }

        await viewable.close(this.viewerHost);
    }

    private getViewableId(tab: Element): string {
        const id = tab?.attributes.getNamedItem("data-id")?.value;

        if (!id) {
            throw new Error(`Could not find viewable ID on element`);
        }

        return id;
    }

    private getViewable(tab: Element): ViewableObject {
        const viewableId = this.getViewableId(tab);
        const viewable = Array.from(this.viewables).find(v => v.id == viewableId);

        if (!viewable)
            throw new Error(`Could not find viewable with ID ${viewableId}`);

        return viewable;
    }

    private initializeTabDragAndDrop() {
        const selector = ".drag-drop-container";
        const dndContainer = this.element.querySelector(selector) as HTMLElement | null;
        if (!dndContainer) {
            this.logger.error(`Could not find elements with selector: '${selector}'. Tab drag and drop will not be initialized.`);
            return;
        }

        const drakeOptions: dragula.DragulaOptions = {
            direction: "horizontal",
            mirrorContainer: dndContainer,
            invalid: (el, target) => {
                return !!el && el.classList.contains("new-tab");
            }
        };

        // Type definition don't include these 2 properties
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (drakeOptions as unknown as any).slideFactorX = 10;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (drakeOptions as unknown as any).slideFactorY = 10;

        const drake = dragula([dndContainer], drakeOptions);
        drake.on("drop", () => this.saveViewablesOrder());

        this.addDisposable(() => drake.destroy());

        const horizontalScroll = (ev: WheelEvent) => {
            dndContainer.scrollLeft += (ev.deltaY ?? 1);
            ev.preventDefault();
        };

        dndContainer.addEventListener("wheel", horizontalScroll);

        this.addDisposable(() => dndContainer.removeEventListener("wheel", horizontalScroll));
    }

    private get viewablesOrderKey(): string {
        return `tab-bar.${this.viewerHost.name}.viewables-order`;
    }

    private loadViewablesOrder(): void {
        try {
            const json = localStorage.getItem(this.viewablesOrderKey);
            if (!json) return;

            this.viewablesOrder = JSON.parse(json) as string[];
        } catch (ex) {
            this.logger.error("Failed to load viewables order", ex);
        } finally {
            if (!this.viewablesOrder) this.viewablesOrder = [];
        }
    }

    private saveViewablesOrder = Util.debounce(this, () => {
        this.viewablesOrder = Array.from(this.tabContainer.children)
            .map(e => e.getAttribute("data-id"))
            .filter(id => id !== null) as string[];

        localStorage.setItem(this.viewablesOrderKey, JSON.stringify(this.viewablesOrder ?? []));
    }, 300, false);

    @watch<TabBar>(vm => vm.viewables.size)
    viewablesChanged() {
        this.saveViewablesOrder();
    }
}
