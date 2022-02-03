import Split from "split.js";
import {IScriptService, ISession, IShortcutManager, Settings, BuiltinShortcuts} from "@domain";

export class Index {
    constructor(
        readonly settings: Settings,
        @ISession readonly session: ISession,
        @IScriptService readonly scriptService: IScriptService,
        @IShortcutManager readonly shortcutManager: IShortcutManager) {
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
        Split([
            document.getElementById("sidebar"),
            document.getElementById("scripts-content")
        ], {
            gutterSize: 6,
            sizes: [14, 86],
            minSize: [100, 300],
            expandToMin: true,
        });
    }

    private registerBuiltInShortcuts() {
        for (const builtinShortcut of BuiltinShortcuts) {
            this.shortcutManager.registerShortcut(builtinShortcut);
        }
    }
}
