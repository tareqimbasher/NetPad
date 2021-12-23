import {IScriptService, ISession, IShortcutManager, Settings, Shortcut} from "@domain";
import {IBackgroundService, KeyCode} from "@common";
import {IContainer, IDialogService} from "aurelia";
import {KeyboardShortcutsDialog} from "@domain/shortcuts/keyboard-shortcuts-dialog";

export class Index {
    private readonly backgroundServices: IBackgroundService[] = [];

    constructor(
        readonly settings: Settings,
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService,
        @IShortcutManager readonly shortcutManager: IShortcutManager,
        @IDialogService readonly dialogService: IDialogService,
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
            new Shortcut("Shortcuts Help", "App")
                .withAltKey()
                .withKey(KeyCode.KeyK)
                .hasAction(() => KeyboardShortcutsDialog.toggle(this.dialogService))
        );


        this.shortcutManager.registerShortcut(
            new Shortcut("New", "Scripts")
                .withCtrlKey()
                .withKey(KeyCode.KeyT)
                .hasAction(() => this.scriptService.create())
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Close", "Scripts")
                .withCtrlKey()
                .withKey(KeyCode.KeyW)
                .hasAction(() => this.session.close(this.session.active.script.id))
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Save", "Scripts")
                .withCtrlKey()
                .withKey(KeyCode.KeyS)
                .hasAction(() => this.scriptService.save(this.session.active.script.id))
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Save All", "Scripts")
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
            new Shortcut("Run", "Scripts")
                .withKey(KeyCode.F5)
                .hasAction(() => this.scriptService.run(this.session.active.script.id))
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Script Properties", "Scripts")
                .withKey(KeyCode.F4)
                .hasAction(() => this.scriptService.openConfig(this.session.active.script.id))
                .configurable()
        );

        this.shortcutManager.registerShortcut(
            new Shortcut("Switch to Last Active Script", "Scripts")
                .withCtrlKey()
                .withKey(KeyCode.Tab)
                .hasAction(() => this.session.activateLastActive())
                .configurable()
        );

        setTimeout(() => {
            console.log("executing ctrl + k");
            this.shortcutManager.executeKeyCombination(KeyCode.KeyK, false, true);
        }, 1000);
    }
}
