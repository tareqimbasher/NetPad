import {IAppService, IEventBus, IScriptService, ISession, IShortcutManager, RunScriptEvent, Script} from "@domain";
import {ContextMenuOptions} from "@application";

export class ScriptEnvironments {
    public tabContextMenuOptions: ContextMenuOptions;

    constructor(
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService,
        @IAppService private readonly appService: IAppService,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IEventBus private readonly eventBus: IEventBus) {
    }

    public async binding() {
        await this.session.initialize();
        if (this.session.environments.length === 0) {
            await this.scriptService.create();
        }

        this.tabContextMenuOptions = {
            selector: ".script-tab:not(.new-script-tab)",
            items: [
                {
                    icon: "run-icon",
                    text: "Run",
                    shortcut: this.shortcutManager.getShortcutByName("Run"),
                    selected: async (clickTarget) => this.eventBus.publish(new RunScriptEvent(this.getScriptId(clickTarget)))
                },
                {
                    icon: "save-icon",
                    text: "Save",
                    shortcut: this.shortcutManager.getShortcutByName("Save"),
                    selected: async (clickTarget) => await this.scriptService.save(this.getScriptId(clickTarget))
                },
                {
                    icon: "script-properties-icon",
                    text: "Properties",
                    shortcut: this.shortcutManager.getShortcutByName("Script Properties"),
                    selected: async (clickTarget) => await this.scriptService.openConfigWindow(this.getScriptId(clickTarget))
                },
                {
                    isDivider: true
                },
                {
                    icon: "open-folder-icon",
                    text: "Open Containing Folder",
                    selected: async (clickTarget) => await this.appService.openFolderContainingScript(this.getScript(clickTarget).path),
                    show: (clickTarget) => !!this.getScript(clickTarget).path
                },
                {
                    icon: "close-icon",
                    text: "Close",
                    shortcut: this.shortcutManager.getShortcutByName("Close"),
                    selected: async (clickTarget) => await this.session.close(this.getScriptId(clickTarget))
                }
            ]
        };
    }

    public async tabSecondaryClicked(scriptId: string, event: MouseEvent) {
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
