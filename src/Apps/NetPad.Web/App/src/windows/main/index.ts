import {Settings, IScriptService, ISession, IShortcutManager, Shortcut, RunScriptEvent, ISettingService} from "@domain";
import {KeyCode} from "@common";
import {IContainer} from "aurelia";
import Split from "split.js";

export class Index {
    constructor(
        readonly settings: Settings,
        @ISession readonly session: ISession,
        @ISettingService readonly settingsService: ISettingService,
        @IScriptService readonly scriptService: IScriptService,
        @IShortcutManager readonly shortcutManager: IShortcutManager,
        @IContainer container: IContainer) {
    }

    public async binding() {
        this.shortcutManager.initialize();

        this.registerBuiltInShortcuts();

        await this.session.initialize();
        if (this.session.environments.length === 0) {
            await this.scriptService.create();
        }
    }

    public attached() {
        Split([document.getElementById("sidebar"), document.getElementById("scripts-content")], {
            gutterSize: 6,
            sizes: [14, 86],
            minSize: [100, 300],
            expandToMin: true,
        });
    }

    private registerBuiltInShortcuts() {
        this.shortcutManager.registerShortcut(
            new Shortcut("New")
                .withCtrlKey()
                .withKey(KeyCode.KeyN)
                .hasAction(() => this.scriptService.create())
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Close")
                .withCtrlKey()
                .withKey(KeyCode.KeyW)
                .hasAction(() => this.session.close(this.session.active.script.id))
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Save")
                .withCtrlKey()
                .withKey(KeyCode.KeyS)
                .hasAction(() => this.scriptService.save(this.session.active.script.id))
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Save All")
                .withCtrlKey()
                .withShiftKey()
                .withKey(KeyCode.KeyS)
                .hasAction(async () => {
                    for (const environment of this.session.environments.filter(e => e.script.isDirty)) {
                        await this.scriptService.save(environment.script.id);
                    }
                })
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Run")
                .withKey(KeyCode.F5)
                .firesEvent(RunScriptEvent)
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Script Properties")
                .withKey(KeyCode.F4)
                .hasAction(() => this.scriptService.openConfigWindow(this.session.active.script.id))
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Settings")
                .withKey(KeyCode.F12)
                .hasAction(() => this.settingsService.openSettingsWindow())
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Switch to Last Active Script")
                .withCtrlKey()
                .withKey(KeyCode.Tab)
                .hasAction(() => this.session.activateLastActive())
                .configurable()
        );
    }
}
