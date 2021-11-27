import {IScriptService, ISession, IShortcutManager, Shortcut} from "@domain";
import {IBackgroundService, KeyCode} from "@common";
import {IContainer} from "aurelia";

export class Index {
    private readonly backgroundServices: IBackgroundService[] = [];

    constructor(
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService,
        @IShortcutManager readonly shortcutManager: IShortcutManager,
        @IContainer container: IContainer) {
        this.backgroundServices.push(...container.getAll(IBackgroundService));
    }

    public async binding() {
        this.shortcutManager.initialize();

        for (const backgroundService of this.backgroundServices) {
            await backgroundService.start();
        }

        this.registerBuiltInShortcuts();

        await this.session.initialize();
        if (this.session.environments.length === 0) {
            await this.scriptService.create();
        }
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
                        await this.session.activate(environment.script.id);
                        await this.scriptService.save(environment.script.id);
                    }
                })
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Run")
                .withKey(KeyCode.F5)
                .hasAction(() => this.scriptService.run(this.session.active.script.id))
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Script Properties")
                .withKey(KeyCode.F4)
                .hasAction(() => this.scriptService.openConfig(this.session.active.script.id))
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
