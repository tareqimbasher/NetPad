import Split from "split.js";
import {IShortcutManager, Settings, BuiltinShortcuts} from "@domain";

export class Index {
    constructor(
        readonly settings: Settings,
        @IShortcutManager readonly shortcutManager: IShortcutManager) {
    }

    public async binding() {
        this.shortcutManager.initialize();
        this.registerBuiltInShortcuts();
    }

    public attached() {
        Split(["sidebar", "script-environments"], {
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
