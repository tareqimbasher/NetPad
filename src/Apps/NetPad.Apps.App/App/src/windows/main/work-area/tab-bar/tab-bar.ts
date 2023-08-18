import {bindable, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import dragula from "dragula";
import {Util} from "@common";
import {CreateScriptDto, IScriptService, ISession, Script, Settings} from "@domain";
import {ContextMenuOptions, IShortcutManager, ViewModelBase} from "@application";
import {ViewableObject} from "../viewers/viewable-object";
import {ViewerHost} from "../viewers/viewer-host";
import {ViewableAppScriptDocument} from "../viewers/text-document-viewer/viewable-text-document";

export class TabBar extends ViewModelBase {
    @bindable viewables: ReadonlySet<ViewableObject>;
    @bindable active?: ViewableObject | null;
    @bindable viewerHost: ViewerHost;
    public tabContextMenuOptions: ContextMenuOptions;
    private viewablesOrder: string[];
    private tabContainer: HTMLElement;

    constructor(
        private readonly element: Element,
        @ISession private readonly session: ISession,
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
                if (ixA < 0) return this.viewablesOrder.length;

                return ixA - this.viewablesOrder.indexOf(b.id);
            });
        return new Set(sorted);
    }

    public binding() {
        this.tabContextMenuOptions = new ContextMenuOptions(".view-tab:not(.new-tab)", [
            {
                icon: "run-icon",
                text: "Run",
                shortcut: this.shortcutManager.getShortcutByName("Run"),
                show: (clickTarget) => {
                    const viewable = this.getViewable(clickTarget);
                    return viewable instanceof ViewableAppScriptDocument
                        && viewable.environment.status !== "Running"
                        && viewable.environment.status !== "Stopping";
                },
                onSelected: async (clickTarget) => await (this.getViewable(clickTarget) as ViewableAppScriptDocument).run()
            },
            {
                icon: "stop-icon",
                text: "Stop",
                show: (clickTarget) => {
                    const viewable = this.getViewable(clickTarget);
                    return viewable instanceof ViewableAppScriptDocument
                        && viewable.environment.status === "Running";
                },
                onSelected: async (clickTarget) => await (this.getViewable(clickTarget) as ViewableAppScriptDocument).stop()
            },
            {
                icon: "save-icon",
                text: "Save",
                shortcut: this.shortcutManager.getShortcutByName("Save"),
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).save()
            },
            {
                icon: "properties-icon",
                text: "Properties",
                shortcut: this.shortcutManager.getShortcutByName("Script Properties"),
                show: (clickTarget) => this.getViewable(clickTarget) instanceof ViewableAppScriptDocument,
                onSelected: async (clickTarget) => await (this.getViewable(clickTarget) as ViewableAppScriptDocument).openProperties()
            },
            {
                isDivider: true
            },
            {
                icon: "open-folder-icon",
                text: "Open Containing Folder",
                show: (clickTarget) => !!this.getScript(clickTarget)?.path,
                onSelected: async (clickTarget) => await this.getViewable(clickTarget).openContainingFolder(),
            },
            {
                icon: "",
                text: "Close Other Tabs",
                shortcut: this.shortcutManager.getShortcutByName("Close Other Tabs"),
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
                shortcut: this.shortcutManager.getShortcutByName("Close All Tabs"),
                onSelected: async () => {
                    for (const viewable of this.viewables) {
                        await this.close(viewable);
                    }
                }
            },
            {
                icon: "close-icon",
                text: "Close",
                shortcut: this.shortcutManager.getShortcutByName("Close"),
                onSelected: async (clickTarget) => await this.close(this.getViewable(clickTarget))
            }
        ]);
    }

    public attached() {
        this.loadViewablesOrder();
        this.initializeTabDragAndDrop();
    }

    public async new() {
        await this.scriptService.create(new CreateScriptDto());
    }

    public async activate(viewable: ViewableObject) {
        await viewable.activate(this.viewerHost);
    }

    public async close(viewable: ViewableObject) {
        await viewable.close(this.viewerHost);
    }

    public async tabWheelButtonClicked(viewable: ViewableObject, event: MouseEvent) {
        if (event.button !== 1) return;
        await this.close(viewable);
    }

    private getViewableId(tab: Element): string {
        const id = tab?.attributes.getNamedItem("data-viewable-id")?.value;

        if (!id) {
            throw new Error(`Could not find viewable ID on element`);
        }

        return id;
    }

    private getViewable(tab: Element) {
        const viewableId = this.getViewableId(tab);
        const viewable = Array.from(this.viewables).find(v => v.id == viewableId);

        if (!viewable)
            throw new Error(`Could not find viewable with ID ${viewableId}`);

        return viewable;
    }

    private getScript(tab: Element): Script | undefined {
        const scriptId = this.getViewableId(tab);

        return this.session.environments.find(e => e.script.id === scriptId)?.script;
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

    private loadViewablesOrder(): void {
        try {
            const key = `tab-bar.${this.viewerHost.order}.viewables-order`;
            const json = localStorage.getItem(key);
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
            .map(e => e.getAttribute("data-viewable-id"))
            .filter(id => id !== null) as string[];

        const key = `tab-bar.${this.viewerHost.order}.viewables-order`;
        localStorage.setItem(key, JSON.stringify(this.viewablesOrder ?? []));
    }, 300, false);

    @watch<TabBar>(vm => vm.viewables.size)
    viewablesChanged() {
        this.saveViewablesOrder();
    }
}
