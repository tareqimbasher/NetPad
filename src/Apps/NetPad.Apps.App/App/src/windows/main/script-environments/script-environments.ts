import {ILogger, IObserverLocator} from "aurelia";
import dragula from "dragula";
import {
    ActiveEnvironmentChangedEvent,
    CreateScriptDto,
    IAppService,
    IEventBus,
    IScriptService,
    ISession,
    RunScriptEvent,
    Script,
    Settings
} from "@domain";
import {ContextMenuOptions, IShortcutManager, ViewModelBase} from "@application";
import {LocalStorageBacked} from "@common";

export class ScriptEnvironments extends ViewModelBase {
    public tabContextMenuOptions: ContextMenuOptions;
    public headerStyle: HeaderStyle;

    constructor(
        private readonly element: Element,
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService,
        @IAppService private readonly appService: IAppService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IEventBus private readonly eventBus: IEventBus,
        private readonly settings: Settings,
        @IObserverLocator observerLocator: IObserverLocator,
        @ILogger logger: ILogger) {
        super(logger);

        this.headerStyle = new HeaderStyle(observerLocator);
        this.headerStyle.load();
        this.disposables.push(() => this.headerStyle.dispose());
    }

    public async binding() {
        if (this.session.environments.length === 0) {
            try {
                await this.scriptService.create(new CreateScriptDto());
            } catch (ex) {
                this.logger.error("Could not create new script", ex);
            }
        }

        this.tabContextMenuOptions = new ContextMenuOptions(".script-tab:not(.new-script-tab)", [
            {
                icon: "run-icon",
                text: "Run",
                shortcut: this.shortcutManager.getShortcutByName("Run"),
                onSelected: async (clickTarget) => this.eventBus.publish(new RunScriptEvent(this.getScriptId(clickTarget)))
            },
            {
                icon: "save-icon",
                text: "Save",
                shortcut: this.shortcutManager.getShortcutByName("Save"),
                onSelected: async (clickTarget) => await this.scriptService.save(this.getScriptId(clickTarget))
            },
            {
                icon: "script-properties-icon",
                text: "Properties",
                shortcut: this.shortcutManager.getShortcutByName("Script Properties"),
                onSelected: async (clickTarget) => await this.scriptService.openConfigWindow(this.getScriptId(clickTarget), null)
            },
            {
                isDivider: true
            },
            {
                icon: "open-folder-icon",
                text: "Open Containing Folder",
                onSelected: async (clickTarget) => await this.appService.openFolderContainingScript(this.getScript(clickTarget).path),
                show: (clickTarget) => !!this.getScript(clickTarget).path
            },
            {
                icon: "",
                text: "Close Other Tabs",
                shortcut: this.shortcutManager.getShortcutByName("Close Other Tabs"),
                onSelected: async (clickTarget) => {
                    const scriptId = this.getScriptId(clickTarget);
                    for (const env of this.session.environments) {
                        if (env.script.id !== scriptId) {
                            await this.session.close(env.script.id);
                        }
                    }
                }
            },
            {
                icon: "",
                text: "Close All Tabs",
                shortcut: this.shortcutManager.getShortcutByName("Close All Tabs"),
                onSelected: async () => {
                    for (const env of this.session.environments) {
                        await this.session.close(env.script.id);
                    }
                }
            },
            {
                icon: "close-icon",
                text: "Close",
                shortcut: this.shortcutManager.getShortcutByName("Close"),
                onSelected: async (clickTarget) => await this.session.close(this.getScriptId(clickTarget))
            }
        ]);
    }

    public attached() {
        this.eventBus.subscribeToServer(ActiveEnvironmentChangedEvent, msg => {
            setTimeout(() => {
                this.element.querySelector(`.script-tab[data-script-id="${msg.scriptId}"]`)?.scrollIntoView();
            }, 1);
        });

        this.initializeTabDragAndDrop();
    }

    public async tabWheelButtonClicked(scriptId: string, event: MouseEvent) {
        if (event.button !== 1) return;
        await this.session.close(scriptId);
    }

    private getScriptId(tab: Element): string {
        const scriptId = tab?.attributes.getNamedItem("data-script-id")?.value;

        if (!scriptId)
            throw new Error(`Could not find script ID on element.`);

        return scriptId;
    }

    private getScript(tab: Element): Script {
        const scriptId = this.getScriptId(tab);

        const script = this.session.environments.find(e => e.script.id === scriptId)?.script;

        if (!script)
            throw new Error(`Could not find script with ID ${scriptId}.`);

        return script;
    }

    private initializeTabDragAndDrop() {
        const selector = ".script-tabs > .drag-drop-container";
        const dndContainer = this.element.querySelector(selector);
        if (!dndContainer) {
            this.logger.error(`Could not find elements with selector: '${selector}'. Tab drag and drop will not be initialized.`);
            return;
        }

        const drakeOptions: dragula.DragulaOptions = {
            direction: "horizontal",
            mirrorContainer: dndContainer,
            invalid: (el, target) => {
                return !!el && el.classList.contains("new-script-tab");
            }
        };

        // Type definition don't include these 2 properties
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (drakeOptions as unknown as any).slideFactorX = 10;
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        (drakeOptions as unknown as any).slideFactorY = 10;

        const drake = dragula([dndContainer], drakeOptions);

        this.disposables.push(() => drake.destroy());

        const horizontalScroll = (ev: WheelEvent) => {
            dndContainer.scrollLeft += (ev.deltaY ?? 1);
            ev.preventDefault();
        };

        dndContainer.addEventListener("wheel", horizontalScroll);

        this.disposables.push(() => dndContainer.removeEventListener("wheel", horizontalScroll));
    }
}

class HeaderStyle extends LocalStorageBacked {
    public style: "minimal" | "bold" = "minimal";
    public size: "comfy" | "compact" = "comfy";

    constructor(observerLocator: IObserverLocator) {
        super("script-environments.header-style");

        const properties = [
            nameof(this.style),
            nameof(this.size),
        ];

        super.autoSave(observerLocator, properties);
    }
}

