import {IEventBus, IScriptService, ISession, IShortcutManager, RunScriptEvent} from "@domain";
import {ContextMenuOptions} from "@application";

export class ScriptEnvironments {
    public tabContextMenuOptions: ContextMenuOptions;

    constructor(
        @ISession private readonly session: ISession,
        @IScriptService private readonly scriptService: IScriptService,
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
                    icon: "bi bi-play-fill",
                    text: "Run",
                    shortcut: this.shortcutManager.getShortcutByName("Run"),
                    selected: async (clickTarget) => this.eventBus.publish(new RunScriptEvent(this.getScriptId(clickTarget)))
                },
                {
                    icon: "bi bi-disc-fill",
                    text: "Save",
                    shortcut: this.shortcutManager.getShortcutByName("Save"),
                    selected: async (clickTarget) => await this.scriptService.save(this.getScriptId(clickTarget))
                },
                {
                    icon: "bi bi-gear-fill",
                    text: "Properties",
                    shortcut: this.shortcutManager.getShortcutByName("Script Properties"),
                    selected: async (clickTarget) => await this.scriptService.openConfigWindow(this.getScriptId(clickTarget))
                },
                {
                    isDivider: true
                },
                {
                    icon: "bi bi-x",
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
}
