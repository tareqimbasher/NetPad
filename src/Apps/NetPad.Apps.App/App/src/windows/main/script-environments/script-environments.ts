import {ILogger} from "aurelia";
import dragula from "dragula";
import {IAppService, IEventBus, IScriptService, ISession, RunScriptEvent, Script, Settings} from "@domain";
import {ContextMenuOptions, IShortcutManager, ViewModelBase} from "@application";

export class ScriptEnvironments extends ViewModelBase {
    public tabContextMenuOptions: ContextMenuOptions;

    constructor(
        private readonly element: Element,
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService,
        @IAppService private readonly appService: IAppService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IEventBus private readonly eventBus: IEventBus,
        private readonly settings: Settings,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public async binding() {
        if (this.session.environments.length === 0) {
            try {
                await this.scriptService.create();
            }
            catch (ex) {
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
                icon: "close-icon",
                text: "Close",
                shortcut: this.shortcutManager.getShortcutByName("Close"),
                onSelected: async (clickTarget) => await this.session.close(this.getScriptId(clickTarget))
            }
        ]);
    }

    public attached() {
        const dndContainer = this.element.querySelector(".script-tabs > .drag-drop-container");
        const drake = dragula([dndContainer], {
            direction: "horizontal",
            mirrorContainer: dndContainer,
            invalid: (el, target) => {
                return el && el.classList.contains("new-script-tab");
            }
        });

        this.disposables.push(() => drake.destroy());

        const horizontalScroll = (ev: WheelEvent) => {
            dndContainer.scrollLeft += (ev.deltaY ?? 1);
            ev.preventDefault();
        };

        dndContainer.addEventListener("wheel", horizontalScroll);

        this.disposables.push(() => dndContainer.removeEventListener("wheel", horizontalScroll))
    }

    public async tabWheelButtonClicked(scriptId: string, event: MouseEvent) {
        if (event.button !== 1) return;
        await this.session.close(scriptId);
    }

    private getScriptId(tab: Element): string | undefined {
        return tab?.attributes.getNamedItem("data-script-id")?.value;
    }

    private getScript(tab: Element): Script | undefined {
        const scriptId = this.getScriptId(tab);
        if (scriptId) {
            return this.session.environments.find(e => e.script.id === scriptId)?.script;
        }
    }
}
